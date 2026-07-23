namespace SpriteRigStudio.Domain.Retargeting;

/// <summary>
/// Determines how translation values in an animation are interpreted
/// when retargeted to characters with different proportions.
/// </summary>
public enum TranslationMode
{
    /// <summary>Values are in absolute pixels.</summary>
    AbsolutePixels = 0,

    /// <summary>Values are relative to the target bone's length.</summary>
    RelativeToBoneLength,

    /// <summary>Values are relative to the parent bone's length.</summary>
    RelativeToParentBoneLength,

    /// <summary>Values are relative to the torso length.</summary>
    RelativeToTorsoLength,

    /// <summary>Values are relative to the character height.</summary>
    RelativeToCharacterHeight,

    /// <summary>Values are relative to the export frame size.</summary>
    RelativeToExportFrame
}
