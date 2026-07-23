using Microsoft.Extensions.Logging;
using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Infrastructure.Migrations;

/// <summary>
/// Runs all required migrations to bring a project up to the current version.
/// </summary>
public class MigrationRunner
{
    private readonly IEnumerable<IProjectMigration> _migrations;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(IEnumerable<IProjectMigration> migrations, ILogger<MigrationRunner> logger)
    {
        _migrations = migrations.OrderBy(m => m.SourceVersion);
        _logger = logger;
    }

    /// <summary>
    /// Gets the latest supported format version.
    /// </summary>
    public int LatestVersion => _migrations.Any()
        ? _migrations.Max(m => m.TargetVersion)
        : 1;

    /// <summary>
    /// Migrates a project JSON string from its current version to the latest version.
    /// Returns a result indicating success or failure, with the migrated JSON.
    /// </summary>
    public MigrationResult Migrate(string projectJson, int currentVersion)
    {
        if (currentVersion > LatestVersion)
        {
            return MigrationResult.Failure("UNSUPPORTED_VERSION",
                $"Project format version {currentVersion} is newer than the supported version {LatestVersion}. " +
                "Please update Sprite Rig Studio to open this project.");
        }

        if (currentVersion == LatestVersion)
        {
            return MigrationResult.Success(projectJson, "Project is already at the latest version.");
        }

        var result = projectJson;
        var appliedMigrations = new List<string>();

        while (currentVersion < LatestVersion)
        {
            var migration = _migrations.FirstOrDefault(m => m.SourceVersion == currentVersion);
            if (migration == null)
            {
                return MigrationResult.Failure("MISSING_MIGRATION",
                    $"No migration found from version {currentVersion} to {currentVersion + 1}. " +
                    "The project file may be from an incompatible version.");
            }

            try
            {
                result = migration.Migrate(result);
                appliedMigrations.Add($"v{migration.SourceVersion}->v{migration.TargetVersion}");
                currentVersion = migration.TargetVersion;
                _logger.LogInformation("Applied migration: {Migration}", appliedMigrations.Last());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed: {Migration}", appliedMigrations.Last());
                return MigrationResult.Failure("MIGRATION_FAILED",
                    $"Migration from version {migration.SourceVersion} to {migration.TargetVersion} failed: {ex.Message}");
            }
        }

        return MigrationResult.Success(result,
            $"Applied migrations: {string.Join(", ", appliedMigrations)}");
    }
}

/// <summary>
/// Result of a migration operation.
/// </summary>
public class MigrationResult : Result
{
    public string? MigratedJson { get; private set; }
    public string? Description { get; private set; }

    private MigrationResult(string json, string description, bool _)
    {
        IsSuccess = true;
        MigratedJson = json;
        Description = description;
    }

    private MigrationResult(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    public static MigrationResult Success(string json, string description) => new(json, description, true);
    public new static MigrationResult Failure(string code, string message) => new(code, message);
}
