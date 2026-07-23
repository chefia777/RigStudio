using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Transforms;
using SpriteRigStudio.Rendering.Abstractions;

namespace SpriteRigStudio.Rendering.Poses;

/// <summary>
/// Evaluates skeleton poses for character rigs using forward kinematics.
/// Supports setup poses, animation sampling, retargeting, and character corrections.
/// </summary>
public class RigPoseEvaluator : IRigPoseEvaluator
{
    /// <summary>
    /// Evaluates the setup pose for a character rig, computing world transforms
    /// for every bone by traversing the skeleton hierarchy and applying
    /// character-specific setup overrides.
    /// </summary>
    public EvaluatedPose EvaluateSetupPose(SkeletonDefinition skeleton, CharacterRigDefinition rig)
    {
        var pose = new EvaluatedPose
        {
            SkeletonId = skeleton.SkeletonId,
            HasAnimation = false,
            AnimationTimeSeconds = null
        };

        var rootBone = skeleton.GetRootBone();
        if (rootBone is null)
            return pose;

        // Compute the character setup root transform
        var characterSetupRoot = rig.SetupTransform.ToMatrix();

        // Traverse hierarchy in breadth-first order (root first)
        var orderedBones = GetHierarchyOrder(skeleton);

        foreach (var bone in orderedBones)
        {
            // Compute local transform: rest pose + setup override
            var local = ComputeSetupLocalTransform(bone, rig);

            // Compute world transform
            Matrix3x2D world;
            if (bone.ParentBoneId.HasValue &&
                pose.BoneWorldTransforms.TryGetValue(bone.ParentBoneId.Value, out var parentWorld))
            {
                // Parent world * character setup root * local
                world = parentWorld * local.ToMatrix();
            }
            else
            {
                // Root bone: character setup root * local
                world = characterSetupRoot * local.ToMatrix();
            }

            pose.BoneLocalTransforms[bone.BoneId] = local;
            pose.BoneWorldTransforms[bone.BoneId] = world;
        }

        return pose;
    }

    /// <summary>
    /// Evaluates an animation pose at a given time for a character rig.
    /// Samples animation keyframes, computes deltas from the setup pose,
    /// applies character corrections, and evaluates forward kinematics.
    /// </summary>
    public EvaluatedPose EvaluateAnimationPose(
        SkeletonDefinition skeleton,
        CharacterRigDefinition rig,
        AnimationClipDefinition animation,
        double timeSeconds)
    {
        if (animation is null)
            return EvaluateSetupPose(skeleton, rig);

        // Resolve loop mode to get effective time
        var effectiveTime = ResolveLoopTime(animation, timeSeconds);

        var pose = new EvaluatedPose
        {
            SkeletonId = skeleton.SkeletonId,
            HasAnimation = true,
            AnimationTimeSeconds = effectiveTime
        };

        var rootBone = skeleton.GetRootBone();
        if (rootBone is null)
            return pose;

        // Get character animation overrides for this animation
        rig.CharacterAnimationOverrides.TryGetValue(animation.AnimationId, out var animationOverride);

        // Compute setup locals and worlds first (without animation)
        var setupLocals = new Dictionary<BoneId, Transform2D>();
        var setupWorlds = new Dictionary<BoneId, Matrix3x2D>();
        ComputeSetupPose(skeleton, rig, setupLocals, setupWorlds);

        // Compute animated locals and final worlds
        var characterSetupRoot = rig.SetupTransform.ToMatrix();
        var orderedBones = GetHierarchyOrder(skeleton);

        foreach (var bone in orderedBones)
        {
            var setupLocal = setupLocals[bone.BoneId];

            // Sample animation track for this bone
            Transform2D animatedLocal;
            if (animation.BoneTracks.TryGetValue(bone.BoneId, out var track))
            {
                animatedLocal = SampleTrack(track, effectiveTime);
            }
            else
            {
                animatedLocal = setupLocal;
            }

            // Compute animation delta: animatedLocal relative to setupLocal
            // delta = animatedLocal * inverse(setupLocal)
            var deltaMatrix = animatedLocal.ToMatrix() * setupLocal.ToMatrix().Inverted();

            // Apply character bone correction on top
            if (animationOverride is not null &&
                animationOverride.BoneCorrections.TryGetValue(bone.BoneId, out var correction))
            {
                var correctionMatrix = ComputeCorrectionMatrix(correction);
                deltaMatrix = deltaMatrix * correctionMatrix;
            }

            // Compute final world: parentFinalWorld * setupLocal * deltaMatrix
            Matrix3x2D finalWorld;
            if (bone.ParentBoneId.HasValue &&
                pose.BoneWorldTransforms.TryGetValue(bone.ParentBoneId.Value, out var parentFinalWorld))
            {
                // Parent final world * setup local * animation delta
                var boneSetupWorld = parentFinalWorld * setupLocal.ToMatrix();
                finalWorld = boneSetupWorld * deltaMatrix;
            }
            else
            {
                // Root bone: character setup root * setup local * delta
                var boneSetupWorld = characterSetupRoot * setupLocal.ToMatrix();
                finalWorld = boneSetupWorld * deltaMatrix;
            }

            pose.BoneLocalTransforms[bone.BoneId] = animatedLocal;
            pose.BoneWorldTransforms[bone.BoneId] = finalWorld;
        }

        return pose;
    }

    /// <summary>
    /// Computes the local transform for a bone in setup pose, applying
    /// the character's per-bone setup override on top of the skeleton rest pose.
    /// </summary>
    private static Transform2D ComputeSetupLocalTransform(BoneDefinition bone, CharacterRigDefinition rig)
    {
        var rest = bone.RestLocalTransform;

        if (rig.BoneSetupOverrides.TryGetValue(bone.BoneId, out var setupOverride))
        {
            return new Transform2D(
                rest.Position + setupOverride.LocalPosition,
                rest.RotationDegrees + setupOverride.LocalRotationDegrees,
                new Vector2D(
                    rest.Scale.X * setupOverride.LocalScale.X,
                    rest.Scale.Y * setupOverride.LocalScale.Y));
        }

        return rest;
    }

    /// <summary>
    /// Computes the full setup pose (local and world transforms for all bones).
    /// </summary>
    private void ComputeSetupPose(
        SkeletonDefinition skeleton,
        CharacterRigDefinition rig,
        Dictionary<BoneId, Transform2D> setupLocals,
        Dictionary<BoneId, Matrix3x2D> setupWorlds)
    {
        var characterSetupRoot = rig.SetupTransform.ToMatrix();
        var orderedBones = GetHierarchyOrder(skeleton);

        foreach (var bone in orderedBones)
        {
            var local = ComputeSetupLocalTransform(bone, rig);
            setupLocals[bone.BoneId] = local;

            Matrix3x2D world;
            if (bone.ParentBoneId.HasValue &&
                setupWorlds.TryGetValue(bone.ParentBoneId.Value, out var parentWorld))
            {
                world = parentWorld * local.ToMatrix();
            }
            else
            {
                world = characterSetupRoot * local.ToMatrix();
            }

            setupWorlds[bone.BoneId] = world;
        }
    }

    /// <summary>
    /// Gets bones in hierarchy order (parents before children).
    /// Uses breadth-first traversal starting from the root.
    /// </summary>
    private static List<BoneDefinition> GetHierarchyOrder(SkeletonDefinition skeleton)
    {
        var result = new List<BoneDefinition>(skeleton.Bones.Count);
        var root = skeleton.GetRootBone();
        if (root is null)
            return result;

        var queue = new Queue<BoneDefinition>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var bone = queue.Dequeue();
            result.Add(bone);

            foreach (var child in skeleton.GetChildren(bone.BoneId))
                queue.Enqueue(child);
        }

        return result;
    }

    /// <summary>
    /// Resolves the effective animation time based on the loop mode.
    /// </summary>
    private static double ResolveLoopTime(AnimationClipDefinition animation, double timeSeconds)
    {
        if (animation.DurationSeconds <= 0)
            return 0;

        switch (animation.LoopMode)
        {
            case LoopMode.Once:
                return Math.Min(timeSeconds, animation.DurationSeconds);

            case LoopMode.Loop:
                return timeSeconds % animation.DurationSeconds;

            case LoopMode.PingPong:
            {
                var period = animation.DurationSeconds * 2;
                var t = timeSeconds % period;
                return t > animation.DurationSeconds
                    ? period - t
                    : t;
            }

            case LoopMode.Clamp:
                return Math.Min(timeSeconds, animation.DurationSeconds);

            default:
                return timeSeconds;
        }
    }

    /// <summary>
    /// Samples an animation track at the given time, interpolating between keyframes.
    /// </summary>
    private static Transform2D SampleTrack(BoneTrack track, double timeSeconds)
    {
        var keyframes = track.GetSortedKeyframes();
        if (keyframes.Count == 0)
            return Transform2D.Identity;

        if (keyframes.Count == 1)
        {
            var kf = keyframes[0];
            return new Transform2D(kf.Position, kf.RotationDegrees, kf.Scale);
        }

        // Handle time before first keyframe
        if (timeSeconds <= keyframes[0].TimeSeconds)
        {
            var kf = keyframes[0];
            return new Transform2D(kf.Position, kf.RotationDegrees, kf.Scale);
        }

        // Handle time after last keyframe
        if (timeSeconds >= keyframes[^1].TimeSeconds)
        {
            var kf = keyframes[^1];
            return new Transform2D(kf.Position, kf.RotationDegrees, kf.Scale);
        }

        // Find surrounding keyframes
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            var kf0 = keyframes[i];
            var kf1 = keyframes[i + 1];

            if (timeSeconds >= kf0.TimeSeconds && timeSeconds <= kf1.TimeSeconds)
            {
                return InterpolateKeyframes(kf0, kf1, timeSeconds);
            }
        }

        // Fallback (shouldn't reach here)
        var last = keyframes[^1];
        return new Transform2D(last.Position, last.RotationDegrees, last.Scale);
    }

    /// <summary>
    /// Interpolates between two keyframes at the given time.
    /// </summary>
    private static Transform2D InterpolateKeyframes(TransformKeyframe from, TransformKeyframe to, double timeSeconds)
    {
        var t = to.TimeSeconds - from.TimeSeconds;
        if (Math.Abs(t) < 1e-9)
            return new Transform2D(from.Position, from.RotationDegrees, from.Scale);

        var fraction = (timeSeconds - from.TimeSeconds) / t;
        fraction = Math.Clamp(fraction, 0.0, 1.0);

        switch (from.Interpolation)
        {
            case InterpolationMode.Step:
                return new Transform2D(from.Position, from.RotationDegrees, from.Scale);

            case InterpolationMode.Smooth:
                return SmoothInterpolate(from, to, fraction);

            case InterpolationMode.Linear:
            default:
                return LinearInterpolate(from, to, fraction);
        }
    }

    /// <summary>
    /// Linear interpolation between two keyframes.
    /// </summary>
    private static Transform2D LinearInterpolate(TransformKeyframe from, TransformKeyframe to, double fraction)
    {
        var pos = new Vector2D(
            from.Position.X + (to.Position.X - from.Position.X) * fraction,
            from.Position.Y + (to.Position.Y - from.Position.Y) * fraction);

        var rot = from.RotationDegrees + (to.RotationDegrees - from.RotationDegrees) * fraction;

        var scale = new Vector2D(
            from.Scale.X + (to.Scale.X - from.Scale.X) * fraction,
            from.Scale.Y + (to.Scale.Y - from.Scale.Y) * fraction);

        return new Transform2D(pos, rot, scale);
    }

    /// <summary>
    /// Smooth (cubic Hermite) interpolation between two keyframes.
    /// Falls back to linear if no tangent data is available.
    /// </summary>
    private static Transform2D SmoothInterpolate(TransformKeyframe from, TransformKeyframe to, double fraction)
    {
        // Fall back to linear if no tangent data
        if (from.TangentData is null || to.TangentData is null)
            return LinearInterpolate(from, to, fraction);

        var t = fraction;
        var tt = t * t;
        var ttt = tt * t;

        // Hermite basis functions
        var h00 = 2 * ttt - 3 * tt + 1;
        var h10 = ttt - 2 * tt + t;
        var h01 = -2 * ttt + 3 * tt;
        var h11 = ttt - tt;

        var pos = new Vector2D(
            h00 * from.Position.X + h10 * from.TangentData.OutTangent.X +
            h01 * to.Position.X + h11 * to.TangentData.InTangent.X,
            h00 * from.Position.Y + h10 * from.TangentData.OutTangent.Y +
            h01 * to.Position.Y + h11 * to.TangentData.InTangent.Y);

        var rot = h00 * from.RotationDegrees + h01 * to.RotationDegrees +
                  h10 * (from.TangentData.OutTangent.X != 0 ? from.RotationDegrees : 0) +
                  h11 * (to.TangentData.InTangent.X != 0 ? to.RotationDegrees : 0);

        var scale = new Vector2D(
            h00 * from.Scale.X + h01 * to.Scale.X,
            h00 * from.Scale.Y + h01 * to.Scale.Y);

        return new Transform2D(pos, rot, scale);
    }

    /// <summary>
    /// Computes a transformation matrix from a bone animation correction.
    /// </summary>
    private static Matrix3x2D ComputeCorrectionMatrix(BoneAnimationCorrection correction)
    {
        var translation = Matrix3x2D.CreateTranslation(correction.PositionOffset);
        var rotation = Matrix3x2D.CreateRotation(correction.RotationOffsetDegrees);
        var scale = Matrix3x2D.CreateScale(correction.ScaleMultiplier.X, correction.ScaleMultiplier.Y);

        return translation * rotation * scale;
    }
}
