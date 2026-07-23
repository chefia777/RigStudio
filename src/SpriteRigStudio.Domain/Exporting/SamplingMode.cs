namespace SpriteRigStudio.Domain.Exporting;

/// <summary>
/// Image sampling mode for rendering and export.
/// </summary>
public enum SamplingMode
{
    /// <summary>Nearest-neighbor (pixel-art safe).</summary>
    NearestNeighbor = 0,

    /// <summary>Bilinear interpolation (smooth).</summary>
    Bilinear
}
