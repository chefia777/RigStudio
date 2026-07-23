namespace SpriteRigStudio.Application.Abstractions;

/// <summary>
/// Provides content hashing for change detection.
/// </summary>
public interface IHashProvider
{
    /// <summary>Computes the SHA-256 hash of a byte array.</summary>
    string ComputeHash(byte[] data);

    /// <summary>Computes the SHA-256 hash of a string.</summary>
    string ComputeHash(string data);

    /// <summary>Computes a combined hash of multiple inputs.</summary>
    string ComputeCombinedHash(params string[] hashes);
}
