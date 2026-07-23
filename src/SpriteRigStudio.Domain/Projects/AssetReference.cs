namespace SpriteRigStudio.Domain.Projects;

/// <summary>
/// Reference to an asset file stored within the project directory.
/// Uses relative paths only.
/// </summary>
public class AssetReference
{
    /// <summary>Relative path from the project root.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Original filename at import time.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Content hash for change detection.</summary>
    public string? ContentHash { get; set; }

    /// <summary>Size of the asset in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>MIME type or file extension category.</summary>
    public string? Category { get; set; }
}
