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
using SpriteRigStudio.Desktop.Tools;
using SpriteRigStudio.Desktop.ViewModels;
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

namespace SpriteRigStudio.Desktop.Composition;

/// <summary>
/// Composition root for the Sprite Rig Studio desktop application.
/// Configures all dependency injection registrations.
/// </summary>
public static class DesktopBootstrapper
{
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // ── Logging ──────────────────────────────────────────────
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // ── Infrastructure ───────────────────────────────────────
        services.AddSingleton<IFileSystem, WindowsFileSystem>();
        services.AddSingleton<IHashProvider, Sha256HashProvider>();
        services.AddSingleton<ProjectSerializer>();
        services.AddSingleton<AtomicProjectFileWriter>();
        services.AddSingleton<IProjectRepository, ProjectRepository>();

        // ── Rendering ────────────────────────────────────────────
        services.AddSingleton<IImageDecoder, SkiaImageDecoder>();
        services.AddSingleton<IImageEncoder, SkiaImageEncoder>();
        services.AddSingleton<IMaskRasterizer, SkiaMaskRasterizer>();
        services.AddSingleton<IRigPoseEvaluator, RigPoseEvaluator>();
        services.AddSingleton<FrameRenderer>();
        services.AddSingleton<SpritesheetComposer>();

        // ── Application Services ─────────────────────────────────
        services.AddSingleton<ProjectService>();
        services.AddSingleton<SkeletonService>();
        services.AddSingleton<RigService>();
        services.AddSingleton<MaskService>();
        services.AddSingleton<AnimationService>();
        services.AddSingleton<RetargetingService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<ProjectValidator>();

        // ── Tools ────────────────────────────────────────────────
        services.AddSingleton<ToolManager>();

        // ── View Models ──────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ProjectPanelViewModel>();
        services.AddSingleton<InspectorViewModel>();

        return services.BuildServiceProvider();
    }
}
