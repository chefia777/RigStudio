using Microsoft.Extensions.Logging;
using SpriteRigStudio.Application.Abstractions;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Infrastructure.Serialization;

namespace SpriteRigStudio.Infrastructure.Persistence;

/// <summary>
/// Implements IProjectRepository using the directory-based project format.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly IFileSystem _fileSystem;
    private readonly IHashProvider _hashProvider;
    private readonly ProjectSerializer _serializer;
    private readonly AtomicProjectFileWriter _atomicWriter;
    private readonly ILogger<ProjectRepository> _logger;

    public ProjectRepository(
        IFileSystem fileSystem,
        IHashProvider hashProvider,
        ProjectSerializer serializer,
        AtomicProjectFileWriter atomicWriter,
        ILogger<ProjectRepository> logger)
    {
        _fileSystem = fileSystem;
        _hashProvider = hashProvider;
        _serializer = serializer;
        _atomicWriter = atomicWriter;
        _logger = logger;
    }

    public async Task<Result> CreateAsync(string projectDirectory, SpriteRigProject project)
    {
        try
        {
            _fileSystem.CreateDirectory(projectDirectory);
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.SourcesDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.PartsDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.SkeletonsDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.CharactersDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.AnimationsDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.ExportProfilesDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.ThumbnailsDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.RecoveryDir));
            _fileSystem.CreateDirectory(_fileSystem.CombinePath(projectDirectory, ProjectFileNaming.ExportsDir));

            // Save initial manifest
            var manifest = new ProjectManifest
            {
                FormatVersion = 1,
                ProjectId = project.ProjectId,
                Name = project.Name,
                CreatedAtUtc = project.CreatedAtUtc,
                UpdatedAtUtc = project.UpdatedAtUtc
            };

            var manifestPath = _fileSystem.CombinePath(projectDirectory, ProjectFileNaming.ManifestFileName);
            var manifestJson = _serializer.SerializeManifest(manifest);

            if (!await _atomicWriter.WriteAsync(manifestPath, manifestJson))
                return Result.Failure("SAVE_FAILED", "Failed to write project manifest.");

            project.ProjectDirectory = projectDirectory;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project at {Path}", projectDirectory);
            return Result.Failure("CREATE_FAILED", $"Failed to create project: {ex.Message}");
        }
    }

    public async Task<OpenProjectResult> OpenAsync(string projectDirectory)
    {
        try
        {
            if (!_fileSystem.DirectoryExists(projectDirectory))
                return OpenProjectResult.Failure("DIR_NOT_FOUND", $"Project directory not found: {projectDirectory}");

            var manifestPath = _fileSystem.CombinePath(projectDirectory, ProjectFileNaming.ManifestFileName);
            if (!_fileSystem.FileExists(manifestPath))
                return OpenProjectResult.Failure("MANIFEST_NOT_FOUND",
                    $"Project manifest not found at: {manifestPath}. Ensure the directory contains a valid Sprite Rig Studio project.");

            var manifestJson = await _fileSystem.ReadAllTextAsync(manifestPath);
            var manifest = _serializer.DeserializeManifest(manifestJson);
            if (manifest == null)
                return OpenProjectResult.Failure("INVALID_MANIFEST",
                    "Project manifest is corrupted or has an invalid format.");

            var project = new SpriteRigProject
            {
                ProjectId = manifest.ProjectId,
                Name = manifest.Name,
                CreatedAtUtc = manifest.CreatedAtUtc,
                UpdatedAtUtc = manifest.UpdatedAtUtc,
                ProjectDirectory = projectDirectory
            };

            // Skeletons
            foreach (var skeletonFile in manifest.SkeletonFiles)
            {
                var fullPath = _fileSystem.CombinePath(projectDirectory, skeletonFile);
                if (!_fileSystem.FileExists(fullPath))
                    continue;

                var json = await _fileSystem.ReadAllTextAsync(fullPath);
                // Parse skeleton and add to project
                // (simplified - full DTO mapping would go here)
            }

            _logger.LogInformation("Opened project: {Name} from {Path}", manifest.Name, projectDirectory);
            return OpenProjectResult.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open project at {Path}", projectDirectory);
            return OpenProjectResult.Failure("OPEN_FAILED", $"Failed to open project: {ex.Message}");
        }
    }

    public async Task<SaveProjectResult> SaveAsync(SpriteRigProject project)
    {
        if (string.IsNullOrEmpty(project.ProjectDirectory))
            return SaveProjectResult.Failure("NO_DIRECTORY", "Project has no directory. Use Save As first.");

        return await SaveToDirectoryAsync(project, project.ProjectDirectory);
    }

    public async Task<SaveProjectResult> SaveAsAsync(SpriteRigProject project, string newDirectory)
    {
        _fileSystem.CreateDirectory(newDirectory);
        return await SaveToDirectoryAsync(project, newDirectory);
    }

    private async Task<SaveProjectResult> SaveToDirectoryAsync(SpriteRigProject project, string directory)
    {
        try
        {
            project.UpdatedAtUtc = DateTime.UtcNow;

            var manifest = new ProjectManifest
            {
                FormatVersion = project.FormatVersion,
                ProjectId = project.ProjectId,
                Name = project.Name,
                CreatedAtUtc = project.CreatedAtUtc,
                UpdatedAtUtc = project.UpdatedAtUtc
            };

            // Save skeletons
            foreach (var (skeletonId, skeleton) in project.Skeletons)
            {
                var fileName = ProjectFileNaming.GetSkeletonFileName(skeletonId.ToString());
                var filePath = _fileSystem.CombinePath(directory, ProjectFileNaming.SkeletonsDir, fileName);
                var json = _serializer.Serialize(skeleton);
                if (!await _atomicWriter.WriteAsync(filePath, json))
                    return SaveProjectResult.Failure("SAVE_FAILED", $"Failed to write skeleton: {skeleton.Name}");

                manifest.SkeletonFiles.Add(_fileSystem.CombinePath(ProjectFileNaming.SkeletonsDir, fileName));
            }

            // Save characters
            foreach (var (charId, character) in project.CharacterRigs)
            {
                var fileName = ProjectFileNaming.GetCharacterFileName(charId.ToString());
                var filePath = _fileSystem.CombinePath(directory, ProjectFileNaming.CharactersDir, fileName);
                var json = _serializer.Serialize(character);
                if (!await _atomicWriter.WriteAsync(filePath, json))
                    return SaveProjectResult.Failure("SAVE_FAILED", $"Failed to write character: {character.Name}");

                manifest.CharacterFiles.Add(_fileSystem.CombinePath(ProjectFileNaming.CharactersDir, fileName));
            }

            // Save animations
            foreach (var (animId, animation) in project.Animations)
            {
                var fileName = ProjectFileNaming.GetAnimationFileName(animId.ToString());
                var filePath = _fileSystem.CombinePath(directory, ProjectFileNaming.AnimationsDir, fileName);
                var json = _serializer.Serialize(animation);
                if (!await _atomicWriter.WriteAsync(filePath, json))
                    return SaveProjectResult.Failure("SAVE_FAILED", $"Failed to write animation: {animation.Name}");

                manifest.AnimationFiles.Add(_fileSystem.CombinePath(ProjectFileNaming.AnimationsDir, fileName));
            }

            // Save export profiles
            foreach (var (profId, profile) in project.ExportProfiles)
            {
                var fileName = ProjectFileNaming.GetExportProfileFileName(profId.ToString());
                var filePath = _fileSystem.CombinePath(directory, ProjectFileNaming.ExportProfilesDir, fileName);
                var json = _serializer.Serialize(profile);
                if (!await _atomicWriter.WriteAsync(filePath, json))
                    return SaveProjectResult.Failure("SAVE_FAILED", $"Failed to write export profile: {profile.Name}");

                manifest.ExportProfileFiles.Add(_fileSystem.CombinePath(ProjectFileNaming.ExportProfilesDir, fileName));
            }

            // Save manifest last (only after all entity files succeed)
            var manifestPath = _fileSystem.CombinePath(directory, ProjectFileNaming.ManifestFileName);
            var manifestJson = _serializer.SerializeManifest(manifest);
            if (!await _atomicWriter.WriteAsync(manifestPath, manifestJson))
                return SaveProjectResult.Failure("SAVE_FAILED", "Failed to write project manifest after saving entity files.");

            project.ProjectDirectory = directory;
            project.IsDirty = false;

            _logger.LogInformation("Saved project: {Name} to {Path}", project.Name, directory);
            return SaveProjectResult.Success(directory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project to {Path}", directory);
            return SaveProjectResult.Failure("SAVE_FAILED", $"Failed to save project: {ex.Message}");
        }
    }

    public async Task<Result> AutosaveAsync(SpriteRigProject project)
    {
        try
        {
            if (string.IsNullOrEmpty(project.ProjectDirectory))
                return Result.Failure("NO_DIRECTORY", "Cannot autosave without a project directory.");

            var recoveryDir = _fileSystem.CombinePath(project.ProjectDirectory, ProjectFileNaming.RecoveryDir);
            _fileSystem.CreateDirectory(recoveryDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var autosaveDir = _fileSystem.CombinePath(recoveryDir, $"autosave_{timestamp}");
            _fileSystem.CreateDirectory(autosaveDir);

            // Save project data to autosave directory
            var saveResult = await SaveToDirectoryAsync(project, autosaveDir);
            if (!saveResult.IsSuccess)
                return Result.Failure("AUTOSAVE_FAILED", saveResult.ErrorMessage!);

            _logger.LogInformation("Autosave completed at {Path}", autosaveDir);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autosave failed");
            return Result.Failure("AUTOSAVE_FAILED", $"Autosave failed: {ex.Message}");
        }
    }

    public Task<IReadOnlyList<RecoverySession>> DetectRecoverySessionsAsync()
    {
        // Simplified - would scan recovery directories
        return Task.FromResult<IReadOnlyList<RecoverySession>>(Array.Empty<RecoverySession>());
    }

    public Task<OpenProjectResult> RecoverAsync(RecoverySession session)
    {
        // Simplified - would recover from autosave
        return Task.FromResult(OpenProjectResult.Failure("NOT_IMPLEMENTED", "Recovery not yet fully implemented."));
    }

    public Task CleanupRecoveryAsync(RecoverySession session)
    {
        // Simplified
        return Task.CompletedTask;
    }
}
