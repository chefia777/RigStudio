using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Infrastructure.Serialization;

/// <summary>
/// Factory that creates JSON converters for strongly-typed ID record structs.
/// Handles both value serialization and dictionary key serialization.
/// </summary>
public class StronglyTypedIdConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(ProjectId),
        typeof(SkeletonId),
        typeof(BoneId),
        typeof(CharacterRigId),
        typeof(SpritePartId),
        typeof(AnimationId),
        typeof(ExportProfileId)
    };

    public override bool CanConvert(Type typeToConvert)
    {
        return SupportedTypes.Contains(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(ProjectId))
            return new ProjectIdJsonConverter();
        if (typeToConvert == typeof(SkeletonId))
            return new SkeletonIdJsonConverter();
        if (typeToConvert == typeof(BoneId))
            return new BoneIdJsonConverter();
        if (typeToConvert == typeof(CharacterRigId))
            return new CharacterRigIdJsonConverter();
        if (typeToConvert == typeof(SpritePartId))
            return new SpritePartIdJsonConverter();
        if (typeToConvert == typeof(AnimationId))
            return new AnimationIdJsonConverter();
        if (typeToConvert == typeof(ExportProfileId))
            return new ExportProfileIdJsonConverter();

        throw new NotSupportedException($"No converter for type {typeToConvert}");
    }
}
