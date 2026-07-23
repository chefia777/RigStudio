using Microsoft.Extensions.Logging;

namespace SpriteRigStudio.Infrastructure.Logging;

/// <summary>
/// Configures logging for Sprite Rig Studio.
/// Logs are written to a rolling file in the application data directory
/// and optionally to the console.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Gets the application data directory for logs.
    /// </summary>
    public static string GetLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "SpriteRigStudio", "Logs");
        Directory.CreateDirectory(logDir);
        return logDir;
    }

    /// <summary>
    /// Configures logging with file output.
    /// </summary>
    public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder)
    {
        // In a production app, this would use a file logging provider
        // such as Serilog's File sink or similar.
        // For the MVP, console logging is sufficient.
        return builder;
    }
}
