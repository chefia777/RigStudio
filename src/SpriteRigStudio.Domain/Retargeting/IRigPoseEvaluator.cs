using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
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

/// <summary>
/// Authoritative service for evaluating skeleton poses.
/// All consumers (viewer, export, CLI) must use this single pipeline.
/// </summary>
public interface IRigPoseEvaluator
{
    /// <summary>
    /// Evaluates the setup pose for a character rig (no animation).
    /// </summary>
    EvaluatedPose EvaluateSetupPose(SkeletonDefinition skeleton, CharacterRigDefinition rig);

    /// <summary>
    /// Evaluates an animation pose at a given time for a character rig.
    /// Applies retargeting and character corrections.
    /// </summary>
    EvaluatedPose EvaluateAnimationPose(
        SkeletonDefinition skeleton,
        CharacterRigDefinition rig,
        AnimationClipDefinition animation,
        double timeSeconds);
}
