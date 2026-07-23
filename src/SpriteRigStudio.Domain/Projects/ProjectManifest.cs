using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Domain.Projects;

/// <summary>
/// Manifest file stored in the project directory.
/// References entity files and asset files by relative path.
/// </summary>
public class ProjectManifest
{
    /// <summary>Format version of the manifest schema.</summary>
    public int FormatVersion { get; set; } = 1;

    /// <summary>Stable project identifier.</summary>
    public ProjectId ProjectId { get; set; }

    /// <summary>Project display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When the project was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the project was last updated.</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Relative paths to skeleton definition files.</summary>
    public List<string> SkeletonFiles { get; init; } = new();

    /// <summary>Relative paths to character rig definition files.</summary>
    public List<string> CharacterFiles { get; init; } = new();

    /// <summary>Relative paths to animation clip files.</summary>
    public List<string> AnimationFiles { get; init; } = new();

    /// <summary>Relative paths to export profile files.</summary>
    public List<string> ExportProfileFiles { get; init; } = new();

    /// <summary>Managed assets referenced by the project.</summary>
    public List<AssetReference> Assets { get; init; } = new();
}
