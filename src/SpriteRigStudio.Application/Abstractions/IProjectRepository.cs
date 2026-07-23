using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Projects;

namespace SpriteRigStudio.Application.Abstractions;

/// <summary>
/// Repository for saving and loading Sprite Rig Studio projects.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Creates a new project directory structure at the specified path.
    /// </summary>
    Task<Result> CreateAsync(string projectDirectory, SpriteRigProject project);

    /// <summary>
    /// Opens and loads a project from the specified directory.
    /// </summary>
    Task<OpenProjectResult> OpenAsync(string projectDirectory);

    /// <summary>
    /// Saves the project to its directory.
    /// </summary>
    Task<SaveProjectResult> SaveAsync(SpriteRigProject project);

    /// <summary>
    /// Saves the project to a new directory.
    /// </summary>
    Task<SaveProjectResult> SaveAsAsync(SpriteRigProject project, string newDirectory);

    /// <summary>
    /// Performs an autosave of the project.
    /// </summary>
    Task<Result> AutosaveAsync(SpriteRigProject project);

    /// <summary>
    /// Detects any recoverable autosave sessions.
    /// </summary>
    Task<IReadOnlyList<RecoverySession>> DetectRecoverySessionsAsync();

    /// <summary>
    /// Recovers a project from an autosave session.
    /// </summary>
    Task<OpenProjectResult> RecoverAsync(RecoverySession session);

    /// <summary>
    /// Deletes an autosave recovery session after successful recovery.
    /// </summary>
    Task CleanupRecoveryAsync(RecoverySession session);
}

/// <summary>
/// Information about a recoverable autosave session.
/// </summary>
public class RecoverySession
{
    public string SessionId { get; init; } = string.Empty;
    public string OriginalProjectPath { get; init; } = string.Empty;
    public DateTime AutosaveTimestamp { get; init; }
    public string RecoveryPath { get; init; } = string.Empty;
    public string? ContentHash { get; init; }
}
