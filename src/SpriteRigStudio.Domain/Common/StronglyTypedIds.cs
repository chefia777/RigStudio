using System;

namespace SpriteRigStudio.Domain.Common;

/// <summary>Stable identifier for a project.</summary>
public readonly record struct ProjectId(Guid Value)
{
    public static ProjectId New() => new(Guid.NewGuid());
    public static ProjectId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier for a skeleton definition.</summary>
public readonly record struct SkeletonId(Guid Value)
{
    public static SkeletonId New() => new(Guid.NewGuid());
    public static SkeletonId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier for a single bone.</summary>
public readonly record struct BoneId(Guid Value)
{
    public static BoneId New() => new(Guid.NewGuid());
    public static BoneId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier for a character rig.</summary>
public readonly record struct CharacterRigId(Guid Value)
{
    public static CharacterRigId New() => new(Guid.NewGuid());
    public static CharacterRigId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier for a sprite part.</summary>
public readonly record struct SpritePartId(Guid Value)
{
    public static SpritePartId New() => new(Guid.NewGuid());
    public static SpritePartId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier for an animation clip.</summary>
public readonly record struct AnimationId(Guid Value)
{
    public static AnimationId New() => new(Guid.NewGuid());
    public static AnimationId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier for an export profile.</summary>
public readonly record struct ExportProfileId(Guid Value)
{
    public static ExportProfileId New() => new(Guid.NewGuid());
    public static ExportProfileId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString("N");
}
