using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Rigs;

/// <summary>
/// Per-bone setup override that adapts a shared skeleton to a specific character.
/// These values describe the character's setup pose, not animation keyframes.
/// A short character, tall character, or leaning character uses the same
/// skeleton and animations through these overrides.
/// </summary>
public class BoneSetupOverride
{
    /// <summary>The bone being overridden.</summary>
    public BoneId BoneId { get; init; }

    /// <summary>Local position offset relative to the skeleton's rest pose.</summary>
    public Vector2D LocalPosition { get; set; }

    /// <summary>Local rotation offset in degrees.</summary>
    public double LocalRotationDegrees { get; set; }

    /// <summary>Local scale multiplier.</summary>
    public Vector2D LocalScale { get; set; } = new(1, 1);

    /// <summary>Override bone length. If null, uses skeleton reference length.</summary>
    public double? Length { get; set; }

    /// <summary>Whether this bone is enabled in the character's rig.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether manipulation of this bone is locked.</summary>
    public bool LockState { get; set; }
}
