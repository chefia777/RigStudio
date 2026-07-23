using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpriteRigStudio.Application.Abstractions;
using SpriteRigStudio.Application.Animations;
using SpriteRigStudio.Application.Exporting;
using SpriteRigStudio.Application.Masks;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Application.Retargeting;
using SpriteRigStudio.Application.Rigs;
using SpriteRigStudio.Application.Skeletons;
using SpriteRigStudio.Application.Validation;
using SpriteRigStudio.Cli.Output;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Validation;
using SpriteRigStudio.Infrastructure.FileSystem;
using SpriteRigStudio.Infrastructure.Hashing;
using SpriteRigStudio.Infrastructure.Persistence;
using SpriteRigStudio.Infrastructure.Serialization;
using SpriteRigStudio.Rendering.Abstractions;
using SpriteRigStudio.Rendering.Images;
using SpriteRigStudio.Rendering.Masks;
using SpriteRigStudio.Rendering.Poses;
using SpriteRigStudio.Rendering.Spritesheets;
using SpriteRigStudio.Rendering.Composition;

namespace SpriteRigStudio.Cli;

public class Program
{
    // ── Exit codes ──────────────────────────────────────────────
    private const int ExitSuccess = 0;
    private const int ExitValidationFailure = 1;
    private const int ExitLoadFailure = 2;
    private const int ExitExportFailure = 3;
    private const int ExitInvalidArgs = 4;
    private const int ExitUnexpectedFailure = 5;

    public static int Main(string[] args)
    {
        try
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            CliFormatter.WriteError($"Unexpected error: {ex.Message}");
            return ExitUnexpectedFailure;
        }
    }

    private static async Task<int> MainAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return ExitInvalidArgs;
        }

        var command = args[0].ToLowerInvariant();
        var remaining = args.Length > 1 ? args[1..] : Array.Empty<string>();

        // ── Build DI ────────────────────────────────────────────
        var serviceProvider = BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            return command switch
            {
                "validate" => await ValidateCommand(remaining, serviceProvider, logger),
                "export" => await ExportCommand(remaining, serviceProvider, logger),
                "export-character" => await ExportCharacterCommand(remaining, serviceProvider, logger),
                "export-animation" => await ExportAnimationCommand(remaining, serviceProvider, logger),
                "list-characters" => await ListCharactersCommand(remaining, serviceProvider, logger),
                "list-animations" => await ListAnimationsCommand(remaining, serviceProvider, logger),
                "list-profiles" => await ListProfilesCommand(remaining, serviceProvider, logger),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException &&
                                    ex is not ArgumentException &&
                                    ex is not KeyNotFoundException)
        {
            // Log unexpected exceptions without stack trace to user
            logger.LogError(ex, "Command failed unexpectedly");
            CliFormatter.WriteError($"Command failed: {ex.Message}");
            return ExitUnexpectedFailure;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DI Setup
    // ═══════════════════════════════════════════════════════════════

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Infrastructure
        services.AddSingleton<IFileSystem, WindowsFileSystem>();
        services.AddSingleton<IHashProvider, Sha256HashProvider>();
        services.AddSingleton<ProjectSerializer>();
        services.AddSingleton<AtomicProjectFileWriter>();
        services.AddSingleton<IProjectRepository, ProjectRepository>();

        // Rendering
        services.AddSingleton<IImageDecoder, SkiaImageDecoder>();
        services.AddSingleton<IImageEncoder, SkiaImageEncoder>();
        services.AddSingleton<IMaskRasterizer, SkiaMaskRasterizer>();
        services.AddSingleton<IRigPoseEvaluator, RigPoseEvaluator>();
        services.AddSingleton<FrameRenderer>();
        services.AddSingleton<SpritesheetComposer>();

        // Application services
        services.AddSingleton<ProjectService>();
        services.AddSingleton<SkeletonService>();
        services.AddSingleton<RigService>();
        services.AddSingleton<MaskService>();
        services.AddSingleton<AnimationService>();
        services.AddSingleton<RetargetingService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<ProjectValidator>();

        return services.BuildServiceProvider();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Argument Parsing Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Extracts the value of a named option (e.g. --profile name) from args.
    /// Removes the option and its value from the working set.
    /// Returns null if the option is not present.
    /// </summary>
    private static string? GetOption(List<string> args, string name)
    {
        var idx = args.IndexOf(name);
        if (idx < 0 || idx + 1 >= args.Count)
            return null;

        var value = args[idx + 1];
        args.RemoveRange(idx, 2);
        return value;
    }

    /// <summary>
    /// Ensures the project path argument exists. Writes usage and returns null on failure.
    /// </summary>
    private static string? RequireProjectPath(List<string> args, ILogger logger)
    {
        if (args.Count == 0 || args[0].StartsWith("--"))
        {
            CliFormatter.WriteError("Missing required <project-path> argument.");
            return null;
        }

        var path = args[0];
        args.RemoveAt(0);

        if (!Directory.Exists(path))
        {
            CliFormatter.WriteError($"Project directory not found: {path}");
            return null;
        }

        return Path.GetFullPath(path);
    }

    /// <summary>
    /// Loads a project from the given path. Returns null and writes errors on failure.
    /// </summary>
    private static async Task<SpriteRigProject?> LoadProjectAsync(string projectPath, ProjectService projectService, ILogger logger)
    {
        var result = await projectService.OpenProjectAsync(projectPath);
        if (result.IsFailure)
        {
            CliFormatter.WriteError($"Failed to load project: {result.ErrorMessage}");
            return null;
        }

        if (result.Project is null)
        {
            CliFormatter.WriteError("Project was loaded but no project data was returned.");
            return null;
        }

        return result.Project;
    }

    /// <summary>
    /// Finds an export profile by name in the project. Returns the first profile if name is null or empty.
    /// Returns null if not found.
    /// </summary>
    private static ExportProfile? ResolveProfile(SpriteRigProject project, string? profileName)
    {
        if (string.IsNullOrEmpty(profileName))
            return project.ExportProfiles.Values.FirstOrDefault();

        foreach (var profile in project.ExportProfiles.Values)
        {
            if (string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase))
                return profile;
        }

        CliFormatter.WriteError($"Export profile '{profileName}' not found in project.");
        return null;
    }

    /// <summary>
    /// Finds a character by name in the project. Returns null if not found.
    /// </summary>
    private static CharacterRigDefinition? ResolveCharacter(SpriteRigProject project, string characterName)
    {
        foreach (var character in project.CharacterRigs.Values)
        {
            if (string.Equals(character.Name, characterName, StringComparison.OrdinalIgnoreCase))
                return character;
        }

        CliFormatter.WriteError($"Character '{characterName}' not found in project.");
        return null;
    }

    /// <summary>
    /// Finds an animation by name in the project. Returns null if not found.
    /// </summary>
    private static AnimationClipDefinition? ResolveAnimation(SpriteRigProject project, string animationName)
    {
        foreach (var animation in project.Animations.Values)
        {
            if (string.Equals(animation.Name, animationName, StringComparison.OrdinalIgnoreCase))
                return animation;
        }

        CliFormatter.WriteError($"Animation '{animationName}' not found in project.");
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    //  validate <project-path>
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ValidateCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var projectPath = RequireProjectPath(args, logger);
        if (projectPath is null)
            return ExitInvalidArgs;

        var projectService = sp.GetRequiredService<ProjectService>();
        var validator = sp.GetRequiredService<ProjectValidator>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        CliFormatter.WriteInfo($"Validating project: {project.Name}");

        var validationResult = validator.ValidateProject(project);

        // Display warnings
        foreach (var warning in validationResult.Warnings)
        {
            CliFormatter.WriteWarning($"  [{warning.Code}] {warning.Message}");
        }

        // Display errors
        foreach (var error in validationResult.Errors)
        {
            CliFormatter.WriteError($"  [{error.Code}] {error.Message}");
        }

        // Summary
        Console.WriteLine();
        if (validationResult.IsValid)
        {
            CliFormatter.WriteSuccess($"Validation passed. {validationResult.Warnings.Count} warning(s).");
            return ExitSuccess;
        }
        else
        {
            CliFormatter.WriteError($"Validation failed: {validationResult.Errors.Count} error(s), {validationResult.Warnings.Count} warning(s).");
            return ExitValidationFailure;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  export <project-path> [--profile <name>]
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ExportCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var profileName = GetOption(args, "--profile");
        var projectPath = RequireProjectPath(args, logger);
        if (projectPath is null)
            return ExitInvalidArgs;

        var projectService = sp.GetRequiredService<ProjectService>();
        var exportService = sp.GetRequiredService<ExportService>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        var profile = ResolveProfile(project, profileName);
        if (profile is null)
            return string.IsNullOrEmpty(profileName) ? ExitExportFailure : ExitInvalidArgs;

        if (project.CharacterRigs.Count == 0)
        {
            CliFormatter.WriteWarning("Project has no characters to export.");
            return ExitSuccess;
        }

        CliFormatter.WriteInfo($"Exporting all animations for all characters using profile '{profile.Name}'...");

        int totalExported = 0;
        int totalErrors = 0;

        foreach (var character in project.CharacterRigs.Values)
        {
            var matchingAnimations = project.Animations.Values
                .Where(a => a.SkeletonId == character.SkeletonId)
                .ToList();

            if (matchingAnimations.Count == 0)
            {
                CliFormatter.WriteWarning($"  Character '{character.Name}': no matching animations found.");
                continue;
            }

            foreach (var animation in matchingAnimations)
            {
                var result = await PerformExport(project, projectPath, character, animation, profile, exportService, logger, sp);
                if (result.IsSuccess)
                {
                    CliFormatter.WriteSuccess($"  Exported: {character.Name} / {animation.Name} -> {result.OutputPath}");
                    totalExported++;
                }
                else
                {
                    CliFormatter.WriteError($"  Export failed: {character.Name} / {animation.Name} - {result.ErrorMessage}");
                    totalErrors++;
                }
            }
        }

        Console.WriteLine();
        if (totalErrors == 0)
        {
            CliFormatter.WriteSuccess($"Export complete. {totalExported} animation(s) exported successfully.");
            return ExitSuccess;
        }
        else
        {
            CliFormatter.WriteError($"Export finished with {totalErrors} error(s). {totalExported} animation(s) exported.");
            return ExitExportFailure;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  export-character <project-path> --character <name> [--profile <name>]
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ExportCharacterCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var characterName = GetOption(args, "--character");
        var profileName = GetOption(args, "--profile");
        var projectPath = RequireProjectPath(args, logger);

        if (projectPath is null || string.IsNullOrEmpty(characterName))
        {
            if (string.IsNullOrEmpty(characterName))
                CliFormatter.WriteError("Missing required --character <name> option.");
            return ExitInvalidArgs;
        }

        var projectService = sp.GetRequiredService<ProjectService>();
        var exportService = sp.GetRequiredService<ExportService>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        var character = ResolveCharacter(project, characterName);
        if (character is null)
            return ExitInvalidArgs;

        var profile = ResolveProfile(project, profileName);
        if (profile is null)
            return ExitExportFailure;

        var matchingAnimations = project.Animations.Values
            .Where(a => a.SkeletonId == character.SkeletonId)
            .ToList();

        if (matchingAnimations.Count == 0)
        {
            CliFormatter.WriteWarning($"Character '{character.Name}' has no matching animations.");
            return ExitSuccess;
        }

        CliFormatter.WriteInfo($"Exporting {matchingAnimations.Count} animation(s) for character '{character.Name}' using profile '{profile.Name}'...");

        int totalExported = 0;
        int totalErrors = 0;

        foreach (var animation in matchingAnimations)
        {
            var result = await PerformExport(project, projectPath, character, animation, profile, exportService, logger, sp);
            if (result.IsSuccess)
            {
                CliFormatter.WriteSuccess($"  Exported: {animation.Name} -> {result.OutputPath}");
                totalExported++;
            }
            else
            {
                CliFormatter.WriteError($"  Export failed: {animation.Name} - {result.ErrorMessage}");
                totalErrors++;
            }
        }

        Console.WriteLine();
        if (totalErrors == 0)
        {
            CliFormatter.WriteSuccess($"Export complete for '{character.Name}'. {totalExported} animation(s) exported.");
            return ExitSuccess;
        }
        else
        {
            CliFormatter.WriteError($"Export finished with {totalErrors} error(s). {totalExported} animation(s) exported for '{character.Name}'.");
            return ExitExportFailure;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  export-animation <project-path> --character <name> --animation <name> [--profile <name>]
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ExportAnimationCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var characterName = GetOption(args, "--character");
        var animationName = GetOption(args, "--animation");
        var profileName = GetOption(args, "--profile");
        var projectPath = RequireProjectPath(args, logger);

        if (projectPath is null ||
            string.IsNullOrEmpty(characterName) ||
            string.IsNullOrEmpty(animationName))
        {
            if (string.IsNullOrEmpty(characterName))
                CliFormatter.WriteError("Missing required --character <name> option.");
            if (string.IsNullOrEmpty(animationName))
                CliFormatter.WriteError("Missing required --animation <name> option.");
            return ExitInvalidArgs;
        }

        var projectService = sp.GetRequiredService<ProjectService>();
        var exportService = sp.GetRequiredService<ExportService>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        var character = ResolveCharacter(project, characterName);
        if (character is null)
            return ExitInvalidArgs;

        var animation = ResolveAnimation(project, animationName);
        if (animation is null)
            return ExitInvalidArgs;

        // Verify skeleton compatibility
        if (animation.SkeletonId != character.SkeletonId)
        {
            CliFormatter.WriteError(
                $"Animation '{animation.Name}' (skeleton {animation.SkeletonId}) is not compatible with " +
                $"character '{character.Name}' (skeleton {character.SkeletonId}).");
            return ExitExportFailure;
        }

        var profile = ResolveProfile(project, profileName);
        if (profile is null)
            return ExitExportFailure;

        CliFormatter.WriteInfo($"Exporting animation '{animation.Name}' for character '{character.Name}' using profile '{profile.Name}'...");

        var result = await PerformExport(project, projectPath, character, animation, profile, exportService, logger, sp);

        Console.WriteLine();
        if (result.IsSuccess)
        {
            CliFormatter.WriteSuccess($"Export complete: {result.FramesExported} frame(s) written to {result.OutputPath}");
            return ExitSuccess;
        }
        else
        {
            CliFormatter.WriteError($"Export failed: {result.ErrorMessage}");
            return ExitExportFailure;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Core Export Logic (shared by all export commands)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Performs a single export operation for one character animation pair.
    /// Validates preconditions, computes output paths, and invokes the export pipeline.
    /// NOTE: The actual rendering pipeline (FrameRenderer / SpritesheetComposer) is not yet
    /// connected to the CLI. This method demonstrates the correct architecture pattern
    /// and will create the output directory structure and metadata.
    /// </summary>
    private static async Task<Domain.Common.ExportResult> PerformExport(
        SpriteRigProject project,
        string projectDirectory,
        CharacterRigDefinition character,
        AnimationClipDefinition animation,
        ExportProfile profile,
        ExportService exportService,
        ILogger logger,
        IServiceProvider services)
    {
        try
        {
            // 1. Validate export preconditions
            var validationResult = exportService.ValidateExport(project, character, animation, profile);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => $"[{e.Code}] {e.Message}"));
                return Domain.Common.ExportResult.Failure("EXPORT_VALIDATION_FAILED",
                    $"Export validation failed: {errors}");
            }

            // 2. Compute output paths
            var outputPaths = exportService.GetOutputPaths(projectDirectory, character.Name, animation.Name, profile);

            // 3. Ensure output directory exists
            Directory.CreateDirectory(outputPaths.ExportsDirectory);
            if (!string.IsNullOrEmpty(outputPaths.FramesDirectory))
                Directory.CreateDirectory(outputPaths.FramesDirectory);

            // 4. Calculate frame count
            int frameCount = animation.FrameCount;

            // 5. Resolve rendering services
            var poseEvaluator = services.GetRequiredService<IRigPoseEvaluator>();
            var frameRenderer = services.GetRequiredService<FrameRenderer>();
            var spritesheetComposer = services.GetRequiredService<SpritesheetComposer>();
            var imageEncoder = services.GetRequiredService<IImageEncoder>();
            var imageDecoder = services.GetRequiredService<IImageDecoder>();

            // 6. Decode all images referenced by character parts
            var decodedImages = new Dictionary<string, DecodedImageInfo>();
            foreach (var part in character.SpriteParts.Values)
            {
                if (string.IsNullOrEmpty(part.ImageReference) || decodedImages.ContainsKey(part.ImageReference))
                    continue;
                var imagePath = Path.Combine(projectDirectory, part.ImageReference);
                var decodeResult = await imageDecoder.DecodeAsync(imagePath);
                if (decodeResult.IsSuccess && decodeResult.Value is not null)
                    decodedImages[part.ImageReference] = decodeResult.Value;
            }

            // 7. Get skeleton
            if (!project.Skeletons.TryGetValue(character.SkeletonId.ToKeyString(), out var skeleton))
            {
                return Domain.Common.ExportResult.Failure("SKELETON_NOT_FOUND",
                    $"Skeleton {character.SkeletonId} not found for character '{character.Name}'.");
            }

            // 8. Evaluate and render each frame
            var frames = new List<byte[]>(frameCount);
            for (int frameIdx = 0; frameIdx < frameCount; frameIdx++)
            {
                var time = frameIdx / (double)animation.FramesPerSecond;
                var pose = poseEvaluator.EvaluateAnimationPose(skeleton, character, animation, time);
                var framePixels = frameRenderer.RenderFrame(pose, character, profile, decodedImages);
                frames.Add(framePixels);
            }

            // 9. Compose spritesheet
            var sheetPixels = spritesheetComposer.ComposeSpritesheet(
                frames,
                profile.Columns,
                profile.Rows,
                profile.FrameWidth,
                profile.FrameHeight);

            // 10. Write PNG file
            var encodeResult = await imageEncoder.EncodePngToFileAsync(
                outputPaths.SpritesheetPath,
                profile.SheetWidth,
                profile.SheetHeight,
                sheetPixels);
            if (encodeResult.IsFailure)
            {
                return Domain.Common.ExportResult.Failure("PNG_ENCODE_FAILED",
                    $"Failed to write spritesheet: {encodeResult.ErrorMessage}");
            }

            // 11. Write metadata JSON
            var metadata = new
            {
                project = project.Name,
                character = character.Name,
                animation = animation.Name,
                profile = profile.Name,
                frames = frameCount,
                frameWidth = profile.FrameWidth,
                frameHeight = profile.FrameHeight,
                sheetWidth = profile.SheetWidth,
                sheetHeight = profile.SheetHeight,
                columns = profile.Columns,
                rows = profile.Rows,
                timestamp = DateTime.UtcNow.ToString("O")
            };
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPaths.MetadataPath, metadataJson);

            return Domain.Common.ExportResult.Success(outputPaths.ExportsDirectory, frameCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export failed for {Character}/{Animation}", character.Name, animation.Name);
            return Domain.Common.ExportResult.Failure("EXPORT_FAILED",
                $"Export failed: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  list-characters <project-path>
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ListCharactersCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var projectPath = RequireProjectPath(args, logger);
        if (projectPath is null)
            return ExitInvalidArgs;

        var projectService = sp.GetRequiredService<ProjectService>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        var characters = project.CharacterRigs.Values
            .OrderBy(c => c.Name)
            .ToList();

        CliFormatter.WriteTable(
            $"Characters in '{project.Name}'",
            characters,
            new TableColumn<CharacterRigDefinition>("Name", c => c.Name),
            new TableColumn<CharacterRigDefinition>("ID", c => c.CharacterRigId.ToString()),
            new TableColumn<CharacterRigDefinition>("Skeleton", c => c.SkeletonId.ToString()),
            new TableColumn<CharacterRigDefinition>("Parts", c => c.SpriteParts.Count.ToString()),
            new TableColumn<CharacterRigDefinition>("Animations", c =>
                project.Animations.Values.Count(a => a.SkeletonId == c.SkeletonId).ToString()));

        return ExitSuccess;
    }

    // ═══════════════════════════════════════════════════════════════
    //  list-animations <project-path> [--character <name>]
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ListAnimationsCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var characterName = GetOption(args, "--character");
        var projectPath = RequireProjectPath(args, logger);
        if (projectPath is null)
            return ExitInvalidArgs;

        var projectService = sp.GetRequiredService<ProjectService>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        IEnumerable<AnimationClipDefinition> animations = project.Animations.Values;

        // Filter by character if specified
        if (!string.IsNullOrEmpty(characterName))
        {
            var character = ResolveCharacter(project, characterName);
            if (character is null)
                return ExitInvalidArgs;

            animations = animations.Where(a => a.SkeletonId == character.SkeletonId);
            CliFormatter.WriteInfo($"Showing animations for character '{character.Name}':");
        }

        var sorted = animations.OrderBy(a => a.Name).ToList();

        CliFormatter.WriteTable(
            string.IsNullOrEmpty(characterName) ? $"Animations in '{project.Name}'" : null,
            sorted,
            new TableColumn<AnimationClipDefinition>("Name", a => a.Name),
            new TableColumn<AnimationClipDefinition>("ID", a => a.AnimationId.ToString()),
            new TableColumn<AnimationClipDefinition>("FPS", a => a.FramesPerSecond.ToString()),
            new TableColumn<AnimationClipDefinition>("Duration", a => $"{a.DurationSeconds:F2}s"),
            new TableColumn<AnimationClipDefinition>("Frames", a => a.FrameCount.ToString()),
            new TableColumn<AnimationClipDefinition>("Loop", a => a.LoopMode.ToString()),
            new TableColumn<AnimationClipDefinition>("Skeleton", a => a.SkeletonId.ToString()));

        return ExitSuccess;
    }

    // ═══════════════════════════════════════════════════════════════
    //  list-profiles <project-path>
    // ═══════════════════════════════════════════════════════════════

    private static async Task<int> ListProfilesCommand(string[] rawArgs, ServiceProvider sp, ILogger logger)
    {
        var args = rawArgs.ToList();
        var projectPath = RequireProjectPath(args, logger);
        if (projectPath is null)
            return ExitInvalidArgs;

        var projectService = sp.GetRequiredService<ProjectService>();

        var project = await LoadProjectAsync(projectPath, projectService, logger);
        if (project is null)
            return ExitLoadFailure;

        var profiles = project.ExportProfiles.Values
            .OrderBy(p => p.Name)
            .ToList();

        CliFormatter.WriteTable(
            $"Export Profiles in '{project.Name}'",
            profiles,
            new TableColumn<ExportProfile>("Name", p => p.Name),
            new TableColumn<ExportProfile>("ID", p => p.ExportProfileId.ToString()),
            new TableColumn<ExportProfile>("Size", p => $"{p.FrameWidth}x{p.FrameHeight}"),
            new TableColumn<ExportProfile>("Sheet", p => $"{p.Columns}x{p.Rows} ({p.SheetWidth}x{p.SheetHeight})"),
            new TableColumn<ExportProfile>("Anchor", p => $"({p.AnchorPixel.X}, {p.AnchorPixel.Y})"),
            new TableColumn<ExportProfile>("Bg", p => p.BackgroundMode.ToString()),
            new TableColumn<ExportProfile>("Scale", p => $"{p.ScalingMode:F2}x"));

        return ExitSuccess;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Unknown / Help
    // ═══════════════════════════════════════════════════════════════

    private static int UnknownCommand(string command)
    {
        CliFormatter.WriteError($"Unknown command: {command}");
        Console.WriteLine();
        PrintUsage();
        return ExitInvalidArgs;
    }

    private static void PrintUsage()
    {
        CliFormatter.WriteHeader("Sprite Rig Studio CLI");
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli validate <project-path>");
        Console.WriteLine("    Loads a project and runs full validation.");
        Console.WriteLine("    Exit code 0 if valid, 1 if validation errors found.");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli export <project-path> [--profile <name>]");
        Console.WriteLine("    Exports all animations for all characters.");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli export-character <project-path> --character <name> [--profile <name>]");
        Console.WriteLine("    Exports all animations for one character.");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli export-animation <project-path> --character <name> --animation <name> [--profile <name>]");
        Console.WriteLine("    Exports one animation for one character.");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli list-characters <project-path>");
        Console.WriteLine("    Lists all character rigs with names and IDs.");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli list-animations <project-path> [--character <name>]");
        Console.WriteLine("    Lists all animations, optionally filtered by character.");
        Console.WriteLine();
        Console.WriteLine("  SpriteRigStudio.Cli list-profiles <project-path>");
        Console.WriteLine("    Lists all export profiles.");
        Console.WriteLine();
        Console.WriteLine("Exit codes:");
        Console.WriteLine("  0 = Success");
        Console.WriteLine("  1 = Validation failure");
        Console.WriteLine("  2 = Load failure");
        Console.WriteLine("  3 = Export failure");
        Console.WriteLine("  4 = Invalid arguments");
        Console.WriteLine("  5 = Unexpected failure");
    }
}
