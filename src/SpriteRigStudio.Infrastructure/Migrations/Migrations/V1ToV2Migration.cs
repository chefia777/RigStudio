using System.Text.Json;

namespace SpriteRigStudio.Infrastructure.Migrations.Migrations;

/// <summary>
/// Example migration from version 1 to version 2.
/// Actual implementation would transform the project JSON structure.
/// </summary>
public class V1ToV2Migration : IProjectMigration
{
    public int SourceVersion => 1;
    public int TargetVersion => 2;

    public string Migrate(string projectJson)
    {
        // Parse, transform, and re-serialize
        using var doc = JsonDocument.Parse(projectJson);
        var root = doc.RootElement;

        // Example: add a new field to the manifest
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        foreach (var property in root.EnumerateObject())
        {
            writer.WritePropertyName(property.Name);
            property.Value.WriteTo(writer);
        }
        writer.WriteNumber("formatVersion", 2); // Update version
        writer.WriteEndObject();

        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
