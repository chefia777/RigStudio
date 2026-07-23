using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Domain.Projects;

/// <summary>
/// Primary project aggregate for Sprite Rig Studio.
/// Contains all data for a single character rigging project.
/// </summary>
public class SpriteRigProject
{
    /// <summary>Stable identifier for this project.</summary>
    public ProjectId ProjectId { get; init; } = ProjectId.New();

    /// <summary>Display name for the project.</summary>
    public string Name { get; set; } = "Untitled Project";

    /// <summary>Format version for migration support.</summary>
    public int FormatVersion { get; set; } = 1;

    /// <summary>When the project was created.</summary>
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>When the project was last modified.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Skeletons defined in this project.</summary>
    public Dictionary<string, SkeletonDefinition> Skeletons { get; init; } = new();

    /// <summary>Character rigs in this project.</summary>
    public Dictionary<string, CharacterRigDefinition> CharacterRigs { get; init; } = new();

    /// <summary>Animation clips in this project.</summary>
    public Dictionary<string, AnimationClipDefinition> Animations { get; init; } = new();

    /// <summary>Export profiles in this project.</summary>
    public Dictionary<string, ExportProfile> ExportProfiles { get; init; } = new();

    /// <summary>Project-level metadata.</summary>
    public ProjectMetadata Metadata { get; set; } = new();

    /// <summary>Asset references (managed files copied into the project).</summary>
    public Dictionary<string, AssetReference> AssetReferences { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the project has unsaved changes.</summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Path to the project directory (set when loaded or created).
    /// Not serialized; used at runtime.
    /// </summary>
    public string? ProjectDirectory { get; set; }

    public override string ToString() => $"{Name} v{FormatVersion} [{ProjectId}]";
}
