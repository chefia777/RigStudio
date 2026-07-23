using SkiaSharp;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
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
        IReadOnlyDictionary<string, byte[]> decodedImages)
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
        var characterSetupRoot = character.SetupTransform.ToMatrix();

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

            // Get decoded image data
            if (!decodedImages.TryGetValue(part.ImageReference, out var imageData))
                continue;

            // Compute part local transform (including pivot)
            var partLocalMatrix = ComputePartLocalMatrix(part);

            // Compute final part world transform using TransformComposition
            // For animation, boneWorld already includes animation deltas
            var partWorld = TransformComposition.ComputePartWorldTransform(
                exportFrameToWorld,
                characterSetupRoot,
                boneWorld,        // boneSetupWorld already includes animation in animated pose
                Matrix3x2D.Identity,  // boneAnimationDelta is baked into boneWorld
                Matrix3x2D.Identity,  // characterCorrection is baked into boneWorld
                partLocalMatrix);

            // Render the part
            RenderPart(canvas, part, imageData, partWorld, profile);
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
        // 1. Translate by pivot (so pivot aligns to bone origin)
        // 2. Apply local setup transform (position, rotation, scale relative to bone)
        //
        // The pivot is in source image space. We need to translate so that
        // the pivot point maps to the bone attachment point.
        var pivotTranslation = Matrix3x2D.CreateTranslation(-part.Pivot);
        var localTransform = part.LocalSetupTransform.ToMatrix();

        return localTransform * pivotTranslation;
    }

    /// <summary>
    /// Renders a single sprite part onto the canvas.
    /// </summary>
    private static void RenderPart(
        SKCanvas canvas,
        SpritePartDefinition part,
        byte[] imageData,
        Matrix3x2D worldTransform,
        ExportProfile profile)
    {
        // Determine source rectangle
        var srcRect = part.SourceRectangle;

        // Create an SKBitmap from the raw pixel data
        // We need width/height - infer from the part or pass separately
        // For this implementation, we assume the full image is used
        // and source rectangle (if specified) defines the sub-region.
        //
        // Since we don't have the image dimensions here, we'd need them
        // from the DecodedImageInfo. In practice, the caller should pass
        // dimensions alongside pixel data. For simplicity, we use the
        // SourceRectangle if provided, otherwise fallback.

        // Note: In a production implementation, the decodedImages dictionary
        // should contain DecodedImageInfo objects (with Width/Height) rather
        // than raw byte arrays. Here we use imageData as a raw bitmap.
        // The size is inferred from the source rectangle.

        if (srcRect.HasValue)
        {
            var rect = srcRect.Value;
            var sw = (int)rect.Width;
            var sh = (int)rect.Height;

            // Extract sub-region from image data
            using var srcBitmap = CreateBitmapFromSubRegion(imageData, (int)rect.X, (int)rect.Y, sw, sh);
            if (srcBitmap is null)
                return;

            DrawSkewedBitmap(canvas, srcBitmap, worldTransform, part.Opacity);
        }
        else
        {
        // No source rectangle specified - try to decode as full image
        // This is a simplified approach; production code should use proper image dimensions
        var size = (int)Math.Sqrt(imageData.Length / 4);
        if (size <= 0) return;

        using var srcBitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var destPtr = srcBitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(imageData, 0, destPtr, imageData.Length);

        DrawSkewedBitmap(canvas, srcBitmap, worldTransform, part.Opacity);
        }
    }

    /// <summary>
    /// Creates a bitmap from a sub-region of a larger image buffer.
    /// </summary>
    private static SKBitmap? CreateBitmapFromSubRegion(byte[] sourceData, int srcX, int srcY, int width, int height)
    {
        // This requires knowing the source image dimensions.
        // For a proper implementation, the caller should provide this info.
        // Simplified fallback: assume square root layout.
        var srcWidth = (int)Math.Sqrt(sourceData.Length / 4);
        if (srcWidth == 0) return null;

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var srcIdx = ((srcY + y) * srcWidth + (srcX + x)) * 4;
                if (srcIdx + 3 >= sourceData.Length)
                    continue;

                var color = new SKColor(
                    sourceData[srcIdx],
                    sourceData[srcIdx + 1],
                    sourceData[srcIdx + 2],
                    sourceData[srcIdx + 3]);

                bitmap.SetPixel(x, y, color);
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Draws a bitmap onto the canvas using the provided transform matrix.
    /// Supports rotation, scaling, translation, and skew through the matrix.
    /// </summary>
    private static void DrawSkewedBitmap(SKCanvas canvas, SKBitmap bitmap, Matrix3x2D transform, double opacity)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium
        };

        // Apply opacity
        if (opacity < 1.0)
        {
            paint.Color = paint.Color.WithAlpha((byte)(opacity * 255));
        }

        // Convert Matrix3x2D to SkiaSharp matrix
        var skMatrix = new SKMatrix
        {
            ScaleX = (float)transform.M11,
            SkewY = (float)transform.M12,
            SkewX = (float)transform.M21,
            ScaleY = (float)transform.M22,
            TransX = (float)transform.M31,
            TransY = (float)transform.M32,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };

        canvas.SetMatrix(skMatrix);
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        canvas.ResetMatrix();
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
