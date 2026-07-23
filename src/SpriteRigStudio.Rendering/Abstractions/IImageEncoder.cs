using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Rendering.Abstractions;

/// <summary>
/// Encodes RGBA pixel data to PNG format.
/// </summary>
public interface IImageEncoder
{
    /// <summary>Encodes RGBA pixel data to PNG bytes.</summary>
    Task<Result<byte[]>> EncodePngAsync(int width, int height, byte[] rgbaPixels);

    /// <summary>Writes RGBA pixel data directly to a PNG file.</summary>
    Task<Result> EncodePngToFileAsync(string filePath, int width, int height, byte[] rgbaPixels);
}
