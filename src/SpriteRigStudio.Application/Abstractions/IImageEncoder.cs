using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Application.Abstractions;

/// <summary>
/// Encodes images to PNG format for export.
/// Implementation isolated in the Rendering layer.
/// </summary>
public interface IImageEncoder
{
    /// <summary>
    /// Encodes RGBA pixel data to a PNG byte array.
    /// </summary>
    Task<Result<byte[]>> EncodePngAsync(int width, int height, byte[] rgbaPixels);

    /// <summary>
    /// Writes encoded PNG data directly to a file path.
    /// </summary>
    Task<Result> EncodePngToFileAsync(string filePath, int width, int height, byte[] rgbaPixels);
}
