using System.Security.Cryptography;
using System.Text;
using SpriteRigStudio.Application.Abstractions;

namespace SpriteRigStudio.Infrastructure.Hashing;

/// <summary>
/// Provides SHA-256 content hashing.
/// </summary>
public class Sha256HashProvider : IHashProvider
{
    public string ComputeHash(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string ComputeHash(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return ComputeHash(bytes);
    }

    public string ComputeCombinedHash(params string[] hashes)
    {
        var combined = string.Join('|', hashes);
        return ComputeHash(combined);
    }
}
