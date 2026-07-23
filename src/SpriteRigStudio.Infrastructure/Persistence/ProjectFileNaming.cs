namespace SpriteRigStudio.Infrastructure.Persistence;

/// <summary>
/// Defines file and directory naming conventions for the project storage format.
/// </summary>
public static class ProjectFileNaming
{
    // Project file
    public const string ManifestFileName = "project.srsproject";

    // Top-level directories
    public const string SourcesDir = "Sources";
    public const string PartsDir = "Parts";
    public const string SkeletonsDir = "Skeletons";
    public const string CharactersDir = "Characters";
    public const string AnimationsDir = "Animations";
    public const string ExportProfilesDir = "ExportProfiles";
    public const string ThumbnailsDir = "Thumbnails";
    public const string RecoveryDir = "Recovery";
    public const string ExportsDir = "Exports";

    // File extensions
    public const string SkeletonExtension = ".json";
    public const string CharacterExtension = ".json";
    public const string AnimationExtension = ".json";
    public const string ExportProfileExtension = ".json";
    public const string ImageExtension = ".png";
    public const string AutosaveExtension = ".autosave";
    public const string RecoveryIndexFile = "recovery.index.json";

    // Patterns
    public const string SkeletonPattern = "*.json";
    public const string CharacterPattern = "*.json";
    public const string AnimationPattern = "*.json";
    public const string ExportProfilePattern = "*.json";
    public const string ImagePattern = "*.png";

    /// <summary>
    /// Gets the skeleton filename for a given skeleton ID.
    /// </summary>
    public static string GetSkeletonFileName(string skeletonId) => $"skeleton_{skeletonId}.json";

    /// <summary>
    /// Gets the character filename for a given character ID.
    /// </summary>
    public static string GetCharacterFileName(string characterId) => $"character_{characterId}.json";

    /// <summary>
    /// Gets the animation filename for a given animation ID.
    /// </summary>
    public static string GetAnimationFileName(string animationId) => $"animation_{animationId}.json";

    /// <summary>
    /// Gets the export profile filename for a given profile ID.
    /// </summary>
    public static string GetExportProfileFileName(string profileId) => $"profile_{profileId}.json";
}
