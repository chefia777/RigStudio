namespace SpriteRigStudio.Domain.Exporting;

/// <summary>
/// Determines behavior when a character exceeds the configured frame dimensions.
/// </summary>
public enum OverflowPolicy
{
    /// <summary>Fail the export with a validation error.</summary>
    FailExport = 0,

    /// <summary>Warn and clip parts that exceed the frame.</summary>
    WarnAndClip,

    /// <summary>Scale the character to fit within the frame.</summary>
    ScaleToFit,

    /// <summary>Expand the frame to accommodate the character.</summary>
    ExpandFrame
}
