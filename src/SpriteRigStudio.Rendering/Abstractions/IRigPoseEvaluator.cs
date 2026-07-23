using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Rendering.Abstractions;

/// <summary>
/// Authoritative service for evaluating skeleton poses.
/// All consumers (viewer, export, CLI) must use this single pipeline.
/// Separated from the domain layer to keep domain clean of rendering concerns.
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
