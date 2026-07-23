using System.Text.Json;
using System.Text.Json.Serialization;
using SpriteRigStudio.Domain.Projects;

namespace SpriteRigStudio.Infrastructure.Serialization;

/// <summary>
/// Serializes and deserializes project manifests and entity files.
/// Uses System.Text.Json with consistent settings.
/// </summary>
public class ProjectSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();
    private static readonly JsonSerializerOptions IndentedOptions = CreateOptions(true);

    private static JsonSerializerOptions CreateOptions(bool writeIndented = false)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Register converter factory for strongly-typed IDs
        // (handles both value and dictionary-key serialization)
        options.Converters.Add(new StronglyTypedIdConverterFactory());

        return options;
    }

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
    /// Serializes the project manifest with indentation for human readability.
    /// </summary>
    public string SerializeManifest(ProjectManifest manifest) => JsonSerializer.Serialize(manifest, IndentedOptions);

    /// <summary>
    /// Deserializes the project manifest.
    /// </summary>
    public ProjectManifest? DeserializeManifest(string json) => Deserialize<ProjectManifest>(json);
}
