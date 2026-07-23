using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Rendering.Abstractions;

/// <summary>
/// Decodes image files into raw RGBA pixel data.
/// </summary>
public interface IImageDecoder
{
    /// <summary>Gets the dimensions of an image file.</summary>
    Result<Size2D> GetDimensions(string filePath);

    /// <summary>Decodes an image file to RGBA pixel data.</summary>
    Task<Result<DecodedImageInfo>> DecodeAsync(string filePath);
}

/// <summary>
/// Result of decoding an image.
/// </summary>
public class DecodedImageInfo
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] RgbaPixels { get; init; } = Array.Empty<byte>();
    public bool HasTransparency { get; init; }
}
