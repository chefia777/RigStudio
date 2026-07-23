using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace SpriteRigStudio.Desktop.Platform;

/// <summary>
/// Provides access to the main application window for dialog operations.
/// </summary>
public interface IWindowProvider
{
    /// <summary>Returns the main application window, or null if not available.</summary>
    Window? GetMainWindow();
}

/// <summary>
/// Avalonia-based implementation that retrieves the main window from the application lifetime.
/// </summary>
public class AvaloniaWindowProvider : IWindowProvider
{
    public Window? GetMainWindow()
    {
        var app = global::Avalonia.Application.Current;
        return (app?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }
}
