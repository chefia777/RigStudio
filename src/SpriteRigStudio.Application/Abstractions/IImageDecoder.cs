using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Application.Abstractions;

/// <summary>
/// Decodes image files into raw pixel data.
/// Implementation isolated in the Rendering layer.
/// </summary>
public interface IImageDecoder
{
    /// <summary>
    /// Gets the dimensions of an image file without fully decoding.
    /// </summary>
    Result<Size2D> GetDimensions(string filePath);

    /// <summary>
    /// Decodes an image file from the specified path.
    /// </summary>
    Task<Result<DecodedImage>> DecodeAsync(string filePath);
}

/// <summary>
/// Decoded image data transfer object.
/// </summary>
public class DecodedImage
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] Pixels { get; init; } = Array.Empty<byte>(); // RGBA
    public bool HasTransparency { get; init; }
}
