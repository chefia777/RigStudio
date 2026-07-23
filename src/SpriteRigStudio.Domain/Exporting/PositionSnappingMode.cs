namespace SpriteRigStudio.Domain.Exporting;

/// <summary>
/// Determines how part positions are snapped during export.
/// </summary>
public enum PositionSnappingMode
{
    /// <summary>No snapping; floating-point positions are used directly.</summary>
    None = 0,

    /// <summary>Snap to the part origin.</summary>
    PartOrigin,

    /// <summary>Snap to the final anchor point.</summary>
    FinalAnchor,

    /// <summary>Snap to the nearest integer pixel grid.</summary>
    PixelGrid
}
