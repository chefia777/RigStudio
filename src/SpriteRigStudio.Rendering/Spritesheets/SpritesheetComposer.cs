using SkiaSharp;

namespace SpriteRigStudio.Rendering.Spritesheets;

/// <summary>
/// Composes individual frame images into a single spritesheet image.
/// Frames are arranged left-to-right, top-to-bottom in row-major order.
/// </summary>
public class SpritesheetComposer
{
    /// <summary>
    /// Composes a set of frame images into a spritesheet.
    /// </summary>
    /// <param name="frameImages">Individual frame RGBA pixel data, in order.</param>
    /// <param name="columns">Number of columns in the spritesheet grid.</param>
    /// <param name="rows">Number of rows in the spritesheet grid.</param>
    /// <param name="frameWidth">Width of each frame in pixels.</param>
    /// <param name="frameHeight">Height of each frame in pixels.</param>
    /// <returns>RGBA pixel data for the complete spritesheet.</returns>
    public byte[] ComposeSpritesheet(
        IReadOnlyList<byte[]> frameImages,
        int columns,
        int rows,
        int frameWidth,
        int frameHeight)
    {
        if (frameImages is null || frameImages.Count == 0)
            return Array.Empty<byte>();

        if (columns <= 0) columns = 1;
        if (rows <= 0) rows = 1;

        var sheetWidth = columns * frameWidth;
        var sheetHeight = rows * frameHeight;

        using var surface = SKSurface.Create(new SKImageInfo(
            sheetWidth, sheetHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        for (int i = 0; i < frameImages.Count && i < columns * rows; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var destX = col * frameWidth;
            var destY = row * frameHeight;

            using var frameBitmap = CreateBitmapFromRgba(frameImages[i], frameWidth, frameHeight);
            if (frameBitmap is not null)
            {
                canvas.DrawBitmap(frameBitmap, destX, destY);
            }
        }

        // Snapshot to RGBA bytes
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);

        var pixels = new byte[sheetWidth * sheetHeight * 4];
        var ptr = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);

        return pixels;
    }

    /// <summary>
    /// Creates an SKBitmap from raw RGBA pixel data.
    /// </summary>
    private static SKBitmap? CreateBitmapFromRgba(byte[] rgbaPixels, int width, int height)
    {
        if (rgbaPixels.Length != width * height * 4)
            return null;

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var ptr = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(rgbaPixels, 0, ptr, rgbaPixels.Length);

        return bitmap;
    }
}
