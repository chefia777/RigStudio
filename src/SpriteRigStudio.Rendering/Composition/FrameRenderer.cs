using SkiaSharp;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Masks;
using SpriteRigStudio.Domain.Parts;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Transforms;
using SpriteRigStudio.Rendering.Abstractions;
using SpriteRigStudio.Rendering.Masks;

namespace SpriteRigStudio.Rendering.Composition;

/// <summary>
/// Renders a single frame of a character rig given an evaluated pose.
/// Composes all sprite parts in render order, applying transforms,
/// pivots, and masks using SkiaSharp.
/// </summary>
public class FrameRenderer
{
    private readonly IMaskRasterizer _maskRasterizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameRenderer"/> class.
    /// </summary>
    /// <param name="maskRasterizer">The mask rasterizer for applying polygon masks.</param>
    public FrameRenderer(IMaskRasterizer maskRasterizer)
    {
        _maskRasterizer = maskRasterizer ?? throw new ArgumentNullException(nameof(maskRasterizer));
    }

    /// <summary>
    /// Renders a single frame of the character at the given pose.
    /// </summary>
    /// <param name="pose">The evaluated pose containing bone world transforms.</param>
    /// <param name="character">The character rig definition with sprite parts.</param>
    /// <param name="profile">Export profile controlling frame dimensions and background.</param>
    /// <param name="decodedImages">Decoded image data keyed by image reference path.</param>
    /// <returns>RGBA pixel data for the rendered frame.</returns>
    public byte[] RenderFrame(
        EvaluatedPose pose,
        CharacterRigDefinition character,
        ExportProfile profile,
        IReadOnlyDictionary<string, DecodedImageInfo> decodedImages)
    {
        var width = profile.FrameWidth;
        var height = profile.FrameHeight;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        var canvas = surface.Canvas;

        // Set background
        switch (profile.BackgroundMode)
        {
            case BackgroundMode.SolidColor:
                var bgColor = profile.BackgroundColor;
                canvas.Clear(new SKColor(bgColor.Red, bgColor.Green, bgColor.Blue, bgColor.Alpha));
                break;
            case BackgroundMode.Transparent:
            default:
                canvas.Clear(SKColors.Transparent);
                break;
        }

        // Compute export frame-to-world transform
        var exportFrameToWorld = ComputeExportFrameTransform(profile, character.GroundAnchor);

        // Get parts sorted by render order
        var orderedParts = character.SpriteParts.Values
            .Where(p => p.Visibility)
            .OrderBy(p => p.RenderOrder)
            .ToList();

        foreach (var part in orderedParts)
        {
            // Skip parts without bound bone or image reference
            if (!part.BoundBoneId.HasValue || string.IsNullOrEmpty(part.ImageReference))
                continue;

            // Get bone world transform from pose
            if (!pose.BoneWorldTransforms.TryGetValue(part.BoundBoneId.Value, out var boneWorld))
                continue;

            // Get decoded image data (includes width, height, and RGBA pixels)
            if (!decodedImages.TryGetValue(part.ImageReference, out var imageInfo))
                continue;

            // Compute part local transform (including pivot)
            var partLocalMatrix = ComputePartLocalMatrix(part);

            // Compose the final part world transform.
            // The EvaluatedPose.BoneWorldTransforms already includes all of:
            //   characterSetupRoot, boneSetupWorld, boneAnimationDelta, characterCorrection
            // so we pass Identity for characterSetupRoot to avoid double-applying.
            var partWorld = TransformComposition.ComputePartWorldTransform(
                exportFrameToWorld,
                Matrix3x2D.Identity,   // baked into boneWorld
                boneWorld,             // includes setup root, bone hierarchy, animation, corrections
                Matrix3x2D.Identity,
                Matrix3x2D.Identity,
                partLocalMatrix);

            // Render the part with optional mask
            RenderPart(canvas, part, imageInfo, partWorld, character, _maskRasterizer);
        }

        // Snapshot to RGBA bytes
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);

        var pixels = new byte[width * height * 4];
        var ptr = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

        return pixels;
    }

    /// <summary>
    /// Computes the part local transform matrix, including pivot offset.
    /// The pivot is the point in the source image that aligns with the bone attachment.
    /// </summary>
    private static Matrix3x2D ComputePartLocalMatrix(SpritePartDefinition part)
    {
        // Part local transform consists of:
        // 1. Translate so that pivot aligns to bone origin (translate by -pivot)
        // 2. Apply local setup transform (position, rotation, scale relative to bone)
        //
        // The pivot is in source image space. We translate so that
        // the pivot point maps to the bone attachment point.
        var pivotTranslation = Matrix3x2D.CreateTranslation(-part.Pivot);
        var localTransform = part.LocalSetupTransform.ToMatrix();

        return localTransform * pivotTranslation;
    }

    /// <summary>
    /// Renders a single sprite part onto the canvas, applying mask if specified.
    /// </summary>
    private static void RenderPart(
        SKCanvas canvas,
        SpritePartDefinition part,
        DecodedImageInfo imageInfo,
        Matrix3x2D worldTransform,
        CharacterRigDefinition character,
        IMaskRasterizer maskRasterizer)
    {
        var width = imageInfo.Width;
        var height = imageInfo.Height;
        var pixels = imageInfo.RgbaPixels;

        // Apply mask if the part references one
        if (part.MaskId.HasValue &&
            character.Masks.TryGetValue(part.MaskId.Value.ToString("N"), out var mask) &&
            mask.Enabled &&
            mask.OuterContour.Count >= 3)
        {
            pixels = maskRasterizer.ApplyMask(pixels, width, height, mask, antialias: true);
        }

        // Determine source rectangle (sub-region within the source image)
        var srcRect = part.SourceRectangle;

        // Create SKBitmap from the (possibly masked) pixels
        using var srcBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var destPtr = srcBitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, destPtr, pixels.Length);

        // Convert Matrix3x2D to SkiaSharp matrix
        var skMatrix = new SKMatrix
        {
            ScaleX = (float)worldTransform.M11,
            SkewY = (float)worldTransform.M12,
            SkewX = (float)worldTransform.M21,
            ScaleY = (float)worldTransform.M22,
            TransX = (float)worldTransform.M31,
            TransY = (float)worldTransform.M32,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

        using var paint = new SKPaint
        {
            IsAntialias = true,
        };

        // Apply opacity
        if (part.Opacity < 1.0)
        {
            paint.Color = paint.Color.WithAlpha((byte)(part.Opacity * 255));
        }

        canvas.Save();
        canvas.SetMatrix(skMatrix);

        if (srcRect.HasValue)
        {
            var rect = srcRect.Value;
            var srcSkRect = new SKRect((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom);
            // Destination is the source rect size placed at origin; the matrix handles positioning
            var dstSkRect = new SKRect(0, 0, (float)rect.Width, (float)rect.Height);
            canvas.DrawBitmap(srcBitmap, srcSkRect, dstSkRect, paint);
        }
        else
        {
            canvas.DrawBitmap(srcBitmap, 0, 0, paint);
        }

        canvas.Restore();
    }

    /// <summary>
    /// Computes the export frame-to-world transform that maps the character's
    /// ground anchor to the configured export anchor pixel.
    /// </summary>
    private static Matrix3x2D ComputeExportFrameTransform(ExportProfile profile, Vector2D groundAnchor)
    {
        // The anchor pixel is in frame space where Y increases downward.
        // We need to map the character's ground anchor (domain space, Y up)
        // to the export frame (pixel space, Y down).
        var anchorX = profile.AnchorPixel.X;
        var anchorY = profile.AnchorPixel.Y;

        // In export frame space: origin is top-left, Y increases downward
        // Ground anchor in character space needs to align to (anchorX, anchorY)
        // in the export frame.
        var offsetX = anchorX - groundAnchor.X;
        var offsetY = anchorY + groundAnchor.Y; // Y flip: character Y-up to image Y-down

        return Matrix3x2D.CreateTranslation(new Vector2D(offsetX, offsetY));
    }
}
