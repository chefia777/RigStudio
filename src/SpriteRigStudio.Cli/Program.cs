using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpriteRigStudio.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ProgramCli>>();

        if (args.Length == 0)
        {
            Console.WriteLine("Sprite Rig Studio CLI");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  SpriteRigStudio.Cli validate <project-path>");
            Console.WriteLine("  SpriteRigStudio.Cli export <project-path> [options]");
            Console.WriteLine("  SpriteRigStudio.Cli export-character <project-path> --character <name>");
            Console.WriteLine("  SpriteRigStudio.Cli export-animation <project-path> --character <name> --animation <name>");
            Console.WriteLine("  SpriteRigStudio.Cli list-characters <project-path>");
            Console.WriteLine("  SpriteRigStudio.Cli list-animations <project-path>");
            Console.WriteLine("  SpriteRigStudio.Cli list-profiles <project-path>");
            return 4; // Invalid arguments
        }

        var command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "validate":
                logger.LogInformation("Validate command not yet implemented");
                return 0;
            case "export":
            case "export-character":
            case "export-animation":
                logger.LogInformation("Export command not yet implemented");
                return 0;
            case "list-characters":
            case "list-animations":
            case "list-profiles":
                logger.LogInformation("List command not yet implemented");
                return 0;
            default:
                Console.WriteLine($"Unknown command: {command}");
                return 4;
        }
    }
}

file class ProgramCli { }
