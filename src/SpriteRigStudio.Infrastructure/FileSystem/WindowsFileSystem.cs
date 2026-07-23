using SpriteRigStudio.Application.Abstractions;

namespace SpriteRigStudio.Infrastructure.FileSystem;

/// <summary>
/// Real file system implementation using System.IO.
/// </summary>
public class WindowsFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public async Task<string> ReadAllTextAsync(string path)
        => await File.ReadAllTextAsync(path);

    public async Task WriteAllTextAsync(string path, string contents)
        => await File.WriteAllTextAsync(path, contents);

    public async Task<byte[]> ReadAllBytesAsync(string path)
        => await File.ReadAllBytesAsync(path);

    public async Task WriteAllBytesAsync(string path, byte[] contents)
        => await File.WriteAllBytesAsync(path, contents);

    public void DeleteFile(string path) => File.Delete(path);
    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
    public void MoveFile(string source, string destination, bool overwrite = false)
    {
        if (overwrite && File.Exists(destination))
            File.Delete(destination);
        File.Move(source, destination);
    }

    public void CopyFile(string source, string destination, bool overwrite = false)
        => File.Copy(source, destination, overwrite);

    public string[] GetFiles(string directory, string pattern)
        => Directory.GetFiles(directory, pattern);

    public string[] GetDirectories(string directory)
        => Directory.GetDirectories(directory);

    public string GetTempFileName() => Path.GetTempFileName();

    public string CombinePath(params string[] parts) => Path.Combine(parts);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string GetExtension(string path) => Path.GetExtension(path);
}
