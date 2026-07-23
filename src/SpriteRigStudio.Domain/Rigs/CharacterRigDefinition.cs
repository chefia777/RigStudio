using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Masks;
using SpriteRigStudio.Domain.Parts;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Domain.Rigs;

/// <summary>
/// Adapts a shared skeleton to one specific character.
/// Does not duplicate shared animation clips.
/// </summary>
public class CharacterRigDefinition
{
    /// <summary>Stable identifier for this character rig.</summary>
    public CharacterRigId CharacterRigId { get; init; } = CharacterRigId.New();

    /// <summary>Display name for this character.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The skeleton this character is based on.</summary>
    public SkeletonId SkeletonId { get; set; }

    /// <summary>Reference to the source artwork (managed asset relative path).</summary>
    public string? SourceArtwork { get; set; }

    /// <summary>Setup transform for aligning the entire rig to the character artwork.</summary>
    public CharacterSetupTransform SetupTransform { get; set; } = new();

    /// <summary>Per-bone setup overrides for this character.</summary>
    public Dictionary<string, BoneSetupOverride> BoneSetupOverrides { get; init; } = new();

    /// <summary>Ground anchor position in skeleton space.</summary>
    public Vector2D GroundAnchor { get; set; }

    /// <summary>Sprite parts belonging to this character.</summary>
    public Dictionary<string, SpritePartDefinition> SpriteParts { get; init; } = new();

    /// <summary>Masks defined for this character's flattened artwork.</summary>
    public Dictionary<string, PolygonMaskDefinition> Masks { get; init; } = new();

    /// <summary>Per-animation character-specific corrections.</summary>
    public Dictionary<string, CharacterAnimationOverride> CharacterAnimationOverrides { get; init; } = new();

    /// <summary>Preview settings for animation preview.</summary>
    public PreviewSettings PreviewSettings { get; set; } = new();

    /// <summary>Part-to-bone bindings (additional mapping info).</summary>
    public Dictionary<string, string> PartBindings { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Optional metadata dictionary.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.Ordinal);

    public override string ToString() => $"{Name} [{CharacterRigId}]";
}
