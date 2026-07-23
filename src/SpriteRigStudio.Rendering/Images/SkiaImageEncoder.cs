using SkiaSharp;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Rendering.Abstractions;

namespace SpriteRigStudio.Rendering.Images;

/// <summary>
/// Encodes images to PNG format using SkiaSharp.
/// </summary>
public class SkiaImageEncoder : IImageEncoder
{
    public Task<Result<byte[]>> EncodePngAsync(int width, int height, byte[] rgbaPixels)
    {
        return Task.Run(() =>
        {
            try
            {
                using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                var ptr = bitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(rgbaPixels, 0, ptr, rgbaPixels.Length);
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return Result.Success(data.ToArray());
            }
            catch (Exception ex)
            {
                return Result.Failure<byte[]>("ENCODE_FAILED", $"Failed to encode PNG: {ex.Message}");
            }
        });
    }

    public async Task<Result> EncodePngToFileAsync(string filePath, int width, int height, byte[] rgbaPixels)
    {
        var result = await EncodePngAsync(width, height, rgbaPixels);
        if (result.IsFailure)
            return Result.Failure(result.ErrorCode!, result.ErrorMessage!);

        try
        {
            await File.WriteAllBytesAsync(filePath, result.Value!);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("WRITE_FAILED", $"Failed to write PNG file: {ex.Message}");
        }
    }
}
