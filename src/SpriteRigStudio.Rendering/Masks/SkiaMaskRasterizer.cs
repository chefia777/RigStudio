using SkiaSharp;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Masks;

namespace SpriteRigStudio.Rendering.Masks;

/// <summary>
/// Applies polygon masks to source images using SkiaSharp.
/// Supports antialiased rasterization and mask application.
/// </summary>
public interface IMaskRasterizer
{
    /// <summary>
    /// Applies a polygon mask to source RGBA pixel data, returning masked pixels.
    /// Pixels outside the mask become fully transparent.
    /// </summary>
    byte[] ApplyMask(byte[] sourceRgba, int width, int height, PolygonMaskDefinition mask, bool antialias);

    /// <summary>
    /// Rasterizes a polygon mask into a grayscale alpha mask.
    /// Returns a byte array where 255 = inside, 0 = outside (with antialiased edges).
    /// </summary>
    byte[] RasterizeMask(PolygonMaskDefinition mask, int width, int height, bool antialias);
}

/// <summary>
/// Rasterizes and applies polygon masks using SkiaSharp rendering.
/// </summary>
public class SkiaMaskRasterizer : IMaskRasterizer
{
    /// <summary>
    /// Applies a polygon mask to source RGBA pixel data.
    /// Pixels outside the mask contour become fully transparent.
    /// </summary>
    public byte[] ApplyMask(byte[] sourceRgba, int width, int height, PolygonMaskDefinition mask, bool antialias)
    {
        if (sourceRgba.Length != width * height * 4)
            throw new ArgumentException($"Source pixel buffer size mismatch. Expected {width * height * 4}, got {sourceRgba.Length}.");

        var maskAlpha = RasterizeMask(mask, width, height, antialias);
        var result = new byte[sourceRgba.Length];

        for (int i = 0; i < sourceRgba.Length; i += 4)
        {
            var maskIdx = i / 4;
            var maskValue = maskAlpha[maskIdx];

            // Copy RGB
            result[i] = sourceRgba[i];
            result[i + 1] = sourceRgba[i + 1];
            result[i + 2] = sourceRgba[i + 2];
            // Apply mask to alpha channel
            result[i + 3] = (byte)(sourceRgba[i + 3] * maskValue / 255);
        }

        return result;
    }

    /// <summary>
    /// Rasterizes a polygon mask into a byte array of alpha values.
    /// 255 = fully inside the mask, 0 = fully outside.
    /// Intermediate values on antialiased edges.
    /// </summary>
    public byte[] RasterizeMask(PolygonMaskDefinition mask, int width, int height, bool antialias)
    {
        if (!mask.Enabled || mask.OuterContour.Count < 3)
        {
            // Return fully opaque if mask is disabled or invalid
            return Enumerable.Repeat((byte)255, width * height).ToArray();
        }

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Gray8, SKAlphaType.Opaque));
        var canvas = surface.Canvas;

        // Clear to black (outside mask)
        canvas.Clear(SKColors.Black);

        // Build the mask path
        using var path = BuildMaskPath(mask);

        // Fill the path with white (inside mask)
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = antialias,
            Color = SKColors.White
        };

        canvas.DrawPath(path, paint);

        // Apply expansion if configured (draw a stroked version)
        if (Math.Abs(mask.ExpansionPixels) > 0.01)
        {
            using var expandPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                IsAntialias = antialias,
                Color = SKColors.White,
                StrokeWidth = (float)(mask.ExpansionPixels * 2),
                StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(path, expandPaint);
        }

        // Read back the alpha values
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);

        var pixels = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                // Gray8 stores luminance which equals alpha for our purposes
                pixels[y * width + x] = color.Red; // R=G=B in grayscale
            }
        }

        return pixels;
    }

    /// <summary>
    /// Builds an SKPath from a polygon mask definition, including inner contours (holes).
    /// </summary>
    private static SKPath BuildMaskPath(PolygonMaskDefinition mask)
    {
        var path = new SKPath();

        // Add outer contour
        if (mask.OuterContour.Count < 3)
            return path;

        AddContourToPath(path, mask.OuterContour, SKPathFillType.EvenOdd);

        // Add inner contours (holes)
        foreach (var inner in mask.InnerContours)
        {
            if (inner.Count >= 3)
                AddContourToPath(path, inner, SKPathFillType.EvenOdd);
        }

        path.FillType = SKPathFillType.EvenOdd;
        return path;
    }

    /// <summary>
    /// Adds a closed contour to an SKPath.
    /// </summary>
    private static void AddContourToPath(SKPath path, List<Vector2D> contour, SKPathFillType fillType)
    {
        if (contour.Count < 3)
            return;

        path.MoveTo((float)contour[0].X, (float)contour[0].Y);

        for (int i = 1; i < contour.Count; i++)
            path.LineTo((float)contour[i].X, (float)contour[i].Y);

        path.Close();
    }
}
