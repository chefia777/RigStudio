using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Retargeting;

/// <summary>
/// Character-specific animation corrections that modify a shared animation
/// without mutating the original clip.
/// </summary>
public class CharacterAnimationOverride
{
    /// <summary>The character rig this override applies to.</summary>
    public CharacterRigId CharacterRigId { get; init; }

    /// <summary>The animation being overridden.</summary>
    public AnimationId AnimationId { get; init; }

    /// <summary>Per-bone correction offsets.</summary>
    public Dictionary<BoneId, BoneAnimationCorrection> BoneCorrections { get; init; } = new();

    /// <summary>Per-part correction offsets.</summary>
    public Dictionary<SpritePartId, PartAnimationCorrection> PartCorrections { get; init; } = new();

    /// <summary>Optional metadata dictionary.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.Ordinal);

    public CharacterAnimationOverride Clone() => new()
    {
        CharacterRigId = CharacterRigId,
        AnimationId = AnimationId,
        BoneCorrections = BoneCorrections.ToDictionary(
            kvp => kvp.Key, kvp => kvp.Value.Clone()),
        PartCorrections = PartCorrections.ToDictionary(
            kvp => kvp.Key, kvp => kvp.Value.Clone()),
        Metadata = new Dictionary<string, string>(Metadata)
    };
}

/// <summary>
/// Per-bone animation correction offset.
/// </summary>
public class BoneAnimationCorrection
{
    /// <summary>Position offset applied on top of the retargeted animation.</summary>
    public Vector2D PositionOffset { get; set; }

    /// <summary>Rotation offset in degrees.</summary>
    public double RotationOffsetDegrees { get; set; }

    /// <summary>Scale multiplier.</summary>
    public Vector2D ScaleMultiplier { get; set; } = new(1, 1);

    /// <summary>Optional constraint override.</summary>
    public string? ConstraintOverride { get; set; }

    public BoneAnimationCorrection Clone() => new()
    {
        PositionOffset = PositionOffset,
        RotationOffsetDegrees = RotationOffsetDegrees,
        ScaleMultiplier = ScaleMultiplier,
        ConstraintOverride = ConstraintOverride
    };
}

/// <summary>
/// Per-part animation correction.
/// </summary>
public class PartAnimationCorrection
{
    /// <summary>Position offset.</summary>
    public Vector2D PositionOffset { get; set; }

    /// <summary>Rotation offset in degrees.</summary>
    public double RotationOffsetDegrees { get; set; }

    /// <summary>Render-order offset.</summary>
    public int RenderOrderOffset { get; set; }

    /// <summary>Whether to override visibility.</summary>
    public bool? VisibilityOverride { get; set; }

    public PartAnimationCorrection Clone() => new()
    {
        PositionOffset = PositionOffset,
        RotationOffsetDegrees = RotationOffsetDegrees,
        RenderOrderOffset = RenderOrderOffset,
        VisibilityOverride = VisibilityOverride
    };
}
