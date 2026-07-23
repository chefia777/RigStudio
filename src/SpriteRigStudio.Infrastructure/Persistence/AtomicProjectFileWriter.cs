using Microsoft.Extensions.Logging;
using SpriteRigStudio.Application.Abstractions;

namespace SpriteRigStudio.Infrastructure.Persistence;

/// <summary>
/// Writes project files atomically using a temporary file and rename strategy.
/// Ensures the previous valid file is preserved if writing fails.
/// </summary>
public class AtomicProjectFileWriter
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AtomicProjectFileWriter> _logger;

    public AtomicProjectFileWriter(IFileSystem fileSystem, ILogger<AtomicProjectFileWriter> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Writes text content to a file atomically.
    /// </summary>
    public async Task<bool> WriteAsync(string targetPath, string content)
    {
        var tempPath = targetPath + ".tmp." + Guid.NewGuid().ToString("N");

        try
        {
            await _fileSystem.WriteAllTextAsync(tempPath, content);
            // Flush is implicit in WriteAllTextAsync (stream is closed)

            if (_fileSystem.FileExists(targetPath))
            {
                var backupPath = targetPath + ".bak";
                _fileSystem.CopyFile(targetPath, backupPath, overwrite: true);
                _fileSystem.DeleteFile(targetPath);
            }

            _fileSystem.MoveFile(tempPath, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to atomically write file: {Path}", targetPath);

            // Clean up temp file
            try
            {
                if (_fileSystem.FileExists(tempPath))
                    _fileSystem.DeleteFile(tempPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temp file: {TempPath}", tempPath);
            }

            return false;
        }
    }

    /// <summary>
    /// Writes binary content to a file atomically.
    /// </summary>
    public async Task<bool> WriteBytesAsync(string targetPath, byte[] content)
    {
        var tempPath = targetPath + ".tmp." + Guid.NewGuid().ToString("N");

        try
        {
            await _fileSystem.WriteAllBytesAsync(tempPath, content);

            if (_fileSystem.FileExists(targetPath))
            {
                var backupPath = targetPath + ".bak";
                _fileSystem.CopyFile(targetPath, backupPath, overwrite: true);
                _fileSystem.DeleteFile(targetPath);
            }

            _fileSystem.MoveFile(tempPath, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to atomically write binary file: {Path}", targetPath);

            try
            {
                if (_fileSystem.FileExists(tempPath))
                    _fileSystem.DeleteFile(tempPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temp file: {TempPath}", tempPath);
            }

            return false;
        }
    }
}
