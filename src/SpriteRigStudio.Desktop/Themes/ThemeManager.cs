using Avalonia;
using Avalonia.Styling;
using SpriteRigStudio.Infrastructure.Settings;

namespace SpriteRigStudio.Desktop.Themes;

/// <summary>
/// Manages application theme (light/dark mode).
/// Uses Avalonia's built-in Fluent theme variants.
/// </summary>
public static class ThemeManager
{
    public static void ApplyTheme(string themeName)
    {
        var app = global::Avalonia.Application.Current;
        if (app == null) return;

        app.RequestedThemeVariant = themeName switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Dark
        };
    }

    public static void ApplyFromSettings()
    {
        var settings = ApplicationSettings.Load();
        ApplyTheme(settings.Theme);
    }

    public static void ToggleTheme()
    {
        var app = global::Avalonia.Application.Current;
        var current = app?.RequestedThemeVariant;
        var newTheme = current == ThemeVariant.Dark ? "Light" : "Dark";
        ApplyTheme(newTheme);

        var settings = ApplicationSettings.Load();
        settings.Theme = newTheme;
        settings.Save();
    }
}
