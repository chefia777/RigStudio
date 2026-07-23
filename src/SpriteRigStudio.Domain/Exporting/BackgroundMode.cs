namespace SpriteRigStudio.Domain.Exporting;

/// <summary>
/// Background rendering mode for exported frames.
/// </summary>
public enum BackgroundMode
{
    /// <summary>Fully transparent background.</summary>
    Transparent = 0,

    /// <summary>Solid white background.</summary>
    White,

    /// <summary>Custom solid color background.</summary>
    SolidColor
}
