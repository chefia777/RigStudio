using System.Text.Json;
using SpriteRigStudio.Domain.Projects;

namespace SpriteRigStudio.Infrastructure.Serialization;

/// <summary>
/// Serializes and deserializes project manifests and entity files.
/// Uses System.Text.Json with consistent settings.
/// </summary>
public class ProjectSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);

    /// <summary>
    /// Attempts to deserialize with error handling.
    /// </summary>
    public bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = Deserialize<T>(json);
            return result is not null;
        }
        catch (JsonException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Serializes the project manifest.
    /// </summary>
    public string SerializeManifest(ProjectManifest manifest) => Serialize(manifest);

    /// <summary>
    /// Deserializes the project manifest.
    /// </summary>
    public ProjectManifest? DeserializeManifest(string json) => Deserialize<ProjectManifest>(json);
}
