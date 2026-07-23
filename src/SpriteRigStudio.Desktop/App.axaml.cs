using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SpriteRigStudio.Desktop.Composition;
using SpriteRigStudio.Desktop.Views;

namespace SpriteRigStudio.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = DesktopBootstrapper.BuildServiceProvider();
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<ViewModels.MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
