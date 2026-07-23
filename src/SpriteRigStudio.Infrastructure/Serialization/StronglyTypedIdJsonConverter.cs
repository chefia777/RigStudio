using System.Text.Json;
using System.Text.Json.Serialization;
using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Infrastructure.Serialization;

/// <summary>JSON converter for ProjectId.</summary>
public class ProjectIdJsonConverter : JsonConverter<ProjectId>
{
    public override ProjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Guid.Parse(reader.GetString()!));
    public override void Write(Utf8JsonWriter writer, ProjectId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

/// <summary>JSON converter for SkeletonId.</summary>
public class SkeletonIdJsonConverter : JsonConverter<SkeletonId>
{
    public override SkeletonId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Guid.Parse(reader.GetString()!));
    public override void Write(Utf8JsonWriter writer, SkeletonId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

/// <summary>JSON converter for BoneId.</summary>
public class BoneIdJsonConverter : JsonConverter<BoneId>
{
    public override BoneId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return str != null ? new BoneId(Guid.Parse(str)) : BoneId.Empty;
    }
    public override void Write(Utf8JsonWriter writer, BoneId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

/// <summary>JSON converter for CharacterRigId.</summary>
public class CharacterRigIdJsonConverter : JsonConverter<CharacterRigId>
{
    public override CharacterRigId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Guid.Parse(reader.GetString()!));
    public override void Write(Utf8JsonWriter writer, CharacterRigId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

/// <summary>JSON converter for SpritePartId.</summary>
public class SpritePartIdJsonConverter : JsonConverter<SpritePartId>
{
    public override SpritePartId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Guid.Parse(reader.GetString()!));
    public override void Write(Utf8JsonWriter writer, SpritePartId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

/// <summary>JSON converter for AnimationId.</summary>
public class AnimationIdJsonConverter : JsonConverter<AnimationId>
{
    public override AnimationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Guid.Parse(reader.GetString()!));
    public override void Write(Utf8JsonWriter writer, AnimationId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

/// <summary>JSON converter for ExportProfileId.</summary>
public class ExportProfileIdJsonConverter : JsonConverter<ExportProfileId>
{
    public override ExportProfileId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Guid.Parse(reader.GetString()!));
    public override void Write(Utf8JsonWriter writer, ExportProfileId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}
