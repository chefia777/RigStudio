using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Domain.Projects;

/// <summary>
/// Metadata associated with a project.
/// </summary>
public class ProjectMetadata
{
    /// <summary>User-defined description.</summary>
    public string? Description { get; set; }

    /// <summary>Author name.</summary>
    public string? Author { get; set; }

    /// <summary>Software version that created the project.</summary>
    public string? SoftwareVersion { get; set; }

    /// <summary>Custom tags.</summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>Custom key-value metadata.</summary>
    public Dictionary<string, string> Custom { get; init; } = new(StringComparer.Ordinal);
}
