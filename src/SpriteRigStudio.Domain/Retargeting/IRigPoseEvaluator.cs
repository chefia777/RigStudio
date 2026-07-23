using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Domain.Retargeting;

/// <summary>
/// Result of a pose evaluation containing world transforms for every bone.
/// </summary>
public class EvaluatedPose
{
    /// <summary>World transforms for each bone.</summary>
    public Dictionary<BoneId, Matrix3x2D> BoneWorldTransforms { get; init; } = new();

    /// <summary>Local transforms for each bone.</summary>
    public Dictionary<BoneId, Transform2D> BoneLocalTransforms { get; init; } = new();

    /// <summary>The skeleton this pose was evaluated for.</summary>
    public SkeletonId SkeletonId { get; init; }

    /// <summary>Whether the pose includes animation.</summary>
    public bool HasAnimation { get; init; }

    /// <summary>The current animation time (if animated).</summary>
    public double? AnimationTimeSeconds { get; init; }
}
