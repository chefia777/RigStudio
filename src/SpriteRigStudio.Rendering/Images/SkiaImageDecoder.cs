using SkiaSharp;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Rendering.Abstractions;

namespace SpriteRigStudio.Rendering.Images;

/// <summary>
/// Decodes images using SkiaSharp. Supports PNG, JPEG, WebP, BMP, and other
/// formats supported by SkiaSharp.
/// </summary>
public class SkiaImageDecoder : IImageDecoder
{
    public Result<Size2D> GetDimensions(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var skBitmap = SKBitmap.Decode(stream);
            if (skBitmap is null)
                return Result.Failure<Size2D>("DECODE_FAILED", $"Failed to decode: {filePath}");
            return Result.Success(new Size2D(skBitmap.Width, skBitmap.Height));
        }
        catch (Exception ex)
        {
            return Result.Failure<Size2D>("DECODE_FAILED", ex.Message);
        }
    }

    public async Task<Result<DecodedImageInfo>> DecodeAsync(string filePath)
    {
        return await Task.Run(() => DecodeSync(filePath));
    }

    private static Result<DecodedImageInfo> DecodeSync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result.Failure<DecodedImageInfo>("FILE_NOT_FOUND", $"Image file not found: {filePath}");

            using var stream = File.OpenRead(filePath);
            using var skBitmap = SKBitmap.Decode(stream);

            if (skBitmap is null)
                return Result.Failure<DecodedImageInfo>("DECODE_FAILED", $"Failed to decode image: {filePath}");

            var width = skBitmap.Width;
            var height = skBitmap.Height;
            var pixels = new byte[width * height * 4];

            if (skBitmap.ColorType == SKColorType.Rgba8888)
            {
                var ptr = skBitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
            }
            else
            {
                using var rgbaBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                if (!skBitmap.CopyTo(rgbaBitmap, SKColorType.Rgba8888))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var color = skBitmap.GetPixel(x, y);
                            var offset = (y * width + x) * 4;
                            pixels[offset] = color.Red;
                            pixels[offset + 1] = color.Green;
                            pixels[offset + 2] = color.Blue;
                            pixels[offset + 3] = color.Alpha;
                        }
                    }
                }
                else
                {
                    var ptr = rgbaBitmap.GetPixels();
                    System.Runtime.InteropServices.Marshal.Copy(ptr, pixels, 0, pixels.Length);
                }
            }

            var hasTransparency = false;
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] < 255)
                {
                    hasTransparency = true;
                    break;
                }
            }

            return Result.Success(new DecodedImageInfo
            {
                Width = skBitmap.Width,
                Height = skBitmap.Height,
                RgbaPixels = pixels,
                HasTransparency = hasTransparency
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<DecodedImageInfo>("DECODE_ERROR", $"Error decoding image '{filePath}': {ex.Message}");
        }
    }
}
