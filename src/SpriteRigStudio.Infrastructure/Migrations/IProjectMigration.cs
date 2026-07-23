namespace SpriteRigStudio.Infrastructure.Migrations;

/// <summary>
/// A single migration step between two format versions.
/// </summary>
public interface IProjectMigration
{
    /// <summary>The source format version.</summary>
    int SourceVersion { get; }

    /// <summary>The target format version.</summary>
    int TargetVersion { get; }

    /// <summary>Migrates the project data forward one version.</summary>
    string Migrate(string projectJson);
}
