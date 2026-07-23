using Microsoft.Extensions.Logging;
using SpriteRigStudio.Application.Abstractions;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Application.Projects;

/// <summary>
/// Application service for project management use cases.
/// </summary>
public class ProjectService
{
    private readonly IProjectRepository _repository;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository repository, ILogger<ProjectService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new project with the default humanoid skeleton.
    /// </summary>
    public async Task<OpenProjectResult> CreateProjectAsync(string name, string directory)
    {
        try
        {
            var project = new SpriteRigProject
            {
                Name = name,
                FormatVersion = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Metadata = new ProjectMetadata
                {
                    SoftwareVersion = "0.1.0"
                }
            };

            // Add default humanoid skeleton
            var skeleton = DefaultHumanoidSkeleton.Create();
            project.Skeletons[skeleton.SkeletonId] = skeleton;

            var result = await _repository.CreateAsync(directory, project);
            if (result.IsFailure)
                return OpenProjectResult.Failure(result.ErrorCode!, result.ErrorMessage!);

            _logger.LogInformation("Created project: {Name} at {Path}", name, directory);
            return OpenProjectResult.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project");
            return OpenProjectResult.Failure("CREATE_FAILED", $"Failed to create project: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens an existing project from a directory.
    /// </summary>
    public async Task<OpenProjectResult> OpenProjectAsync(string directory)
    {
        _logger.LogInformation("Opening project at {Path}", directory);
        return await _repository.OpenAsync(directory);
    }

    /// <summary>
    /// Saves the project to its current directory.
    /// </summary>
    public async Task<SaveProjectResult> SaveProjectAsync(SpriteRigProject project)
    {
        _logger.LogInformation("Saving project: {Name}", project.Name);
        return await _repository.SaveAsync(project);
    }

    /// <summary>
    /// Saves the project to a new directory.
    /// </summary>
    public async Task<SaveProjectResult> SaveProjectAsAsync(SpriteRigProject project, string directory)
    {
        _logger.LogInformation("Saving project as: {Name} to {Path}", project.Name, directory);
        return await _repository.SaveAsAsync(project, directory);
    }
}
