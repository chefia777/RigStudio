namespace SpriteRigStudio.Domain.Parts;

/// <summary>
/// Identifies how a sprite part's source pixels are obtained.
/// </summary>
public enum SourceType
{
    /// <summary>Part is cut from a flattened full-character image using a mask.</summary>
    FlattenedImageMask = 0,

    /// <summary>Part is an individually imported transparent PNG.</summary>
    SeparateImage,

    /// <summary>Part was generated as a programmatic cutout (future use).</summary>
    GeneratedCutout
}
