using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Application.Abstractions;

/// <summary>
/// Abstraction over the file system for testability.
/// </summary>
public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    void CreateDirectory(string path);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string contents);
    Task<byte[]> ReadAllBytesAsync(string path);
    Task WriteAllBytesAsync(string path, byte[] contents);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive);
    void MoveFile(string source, string destination, bool overwrite = false);
    void CopyFile(string source, string destination, bool overwrite = false);
    string[] GetFiles(string directory, string pattern);
    string[] GetDirectories(string directory);
    string GetTempFileName();
    string CombinePath(params string[] parts);
    string? GetDirectoryName(string path);
    string GetFileName(string path);
    string GetFileNameWithoutExtension(string path);
    string GetExtension(string path);
}
