using System.Text.Json;

namespace SpriteRigStudio.Infrastructure.Settings;

/// <summary>
/// User-level application settings stored separately from projects.
/// </summary>
public class ApplicationSettings
{
    public string Theme { get; set; } = "Dark";
    public List<string> RecentProjects { get; init; } = new();
    public int AutosaveIntervalSeconds { get; set; } = 60;
    public int RecoveryRetentionDays { get; set; } = 7;
    public string DefaultProjectDirectory { get; set; } = "";
    public string DefaultExportDirectory { get; set; } = "";
    public double DefaultGridSize { get; set; } = 16;
    public bool DefaultPixelArtMode { get; set; } = true;
    public string ViewportBackground { get; set; } = "#2D2D30";
    public string LoggingLevel { get; set; } = "Information";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "SpriteRigStudio");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static ApplicationSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ApplicationSettings>(json, JsonOptions) ?? new();
            }
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch { }
    }

    public void AddRecentProject(string path)
    {
        RecentProjects.Remove(path);
        RecentProjects.Insert(0, path);
        if (RecentProjects.Count > 10)
            RecentProjects.RemoveRange(10, RecentProjects.Count - 10);
        Save();
    }
}
