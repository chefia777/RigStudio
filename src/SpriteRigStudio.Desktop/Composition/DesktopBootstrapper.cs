using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpriteRigStudio.Desktop.ViewModels;

namespace SpriteRigStudio.Desktop.Composition;

/// <summary>
/// Composition root for the Sprite Rig Studio desktop application.
/// Configures dependency injection and application services.
/// </summary>
public static class DesktopBootstrapper
{
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // View models
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
