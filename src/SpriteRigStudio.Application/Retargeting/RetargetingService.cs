using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Retargeting;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Application.Retargeting;

/// <summary>
/// Centralized retargeting service that applies one reusable skeleton animation
/// to multiple character rigs with different proportions and setup poses.
/// </summary>
public class RetargetingService
{
    /// <summary>
    /// Computes the animation rotation delta.
    /// Formula: animation rotation delta = animated canonical rotation - canonical rest rotation
    /// </summary>
    public double ComputeRotationDelta(BoneDefinition canonicalBone, TransformKeyframe? keyframe, double defaultRotation)
    {
        var canonicalRest = canonicalBone.RestLocalTransform.RotationDegrees;
        var animatedRotation = keyframe?.RotationDegrees ?? defaultRotation;
        return animatedRotation - canonicalRest;
    }

    /// <summary>
    /// Applies rotation retargeting to get the character's local rotation.
    /// Formula: character local rotation = character setup rotation + canonical animation delta + character correction
    /// </summary>
    public double ApplyRotationRetargeting(
        BoneSetupOverride? characterSetup,
        double animationDelta,
        BoneAnimationCorrection? correction)
    {
        var setupRotation = characterSetup?.LocalRotationDegrees ?? 0;
        var correctionRotation = correction?.RotationOffsetDegrees ?? 0;
        return setupRotation + animationDelta + correctionRotation;
    }

    /// <summary>
    /// Computes the retargeted position based on translation mode.
    /// </summary>
    public Vector2D ComputeTranslation(
        Vector2D animatedPosition,
        TranslationMode mode,
        double characterBoneLength,
        double referenceBoneLength,
        double characterHeight,
        double referenceHeight,
        Vector2D? correctionOffset = null)
    {
        var offset = correctionOffset ?? Vector2D.Zero;

        return mode switch
        {
            TranslationMode.AbsolutePixels => animatedPosition + offset,
            TranslationMode.RelativeToBoneLength when referenceBoneLength > 0 =>
                new Vector2D(
                    animatedPosition.X * (characterBoneLength / referenceBoneLength) + offset.X,
                    animatedPosition.Y * (characterBoneLength / referenceBoneLength) + offset.Y),
            TranslationMode.RelativeToCharacterHeight when referenceHeight > 0 =>
                new Vector2D(
                    animatedPosition.X * (characterHeight / referenceHeight) + offset.X,
                    animatedPosition.Y * (characterHeight / referenceHeight) + offset.Y),
            _ => animatedPosition + offset
        };
    }

    /// <summary>
    /// Evaluates a single bone's local animation transform at a given time.
    /// </summary>
    public Transform2D EvaluateBoneAnimation(
        BoneTrack track,
        double timeSeconds,
        LoopMode loopMode,
        double durationSeconds)
    {
        var keyframes = track.GetSortedKeyframes();
        if (keyframes.Count == 0)
            return Transform2D.Identity;

        // Before first keyframe: hold first
        if (timeSeconds <= keyframes[0].TimeSeconds)
            return new Transform2D(keyframes[0].Position, keyframes[0].RotationDegrees, keyframes[0].Scale);

        // After last keyframe: depends on loop mode
        var last = keyframes[^1];
        if (timeSeconds >= last.TimeSeconds)
        {
            if (loopMode == LoopMode.Loop || loopMode == LoopMode.PingPong)
            {
                // Wrap time for looping
                timeSeconds %= durationSeconds;
                if (timeSeconds <= keyframes[0].TimeSeconds)
                    return new Transform2D(keyframes[0].Position, keyframes[0].RotationDegrees, keyframes[0].Scale);
            }
            else
            {
                return new Transform2D(last.Position, last.RotationDegrees, last.Scale);
            }
        }

        // Find the two keyframes to interpolate between
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            var kf0 = keyframes[i];
            var kf1 = keyframes[i + 1];

            if (timeSeconds >= kf0.TimeSeconds && timeSeconds <= kf1.TimeSeconds)
            {
                return Interpolate(kf0, kf1, timeSeconds);
            }
        }

        return Transform2D.Identity;
    }

    private static Transform2D Interpolate(TransformKeyframe from, TransformKeyframe to, double time)
    {
        var range = to.TimeSeconds - from.TimeSeconds;
        if (range <= 0) return new Transform2D(from.Position, from.RotationDegrees, from.Scale);

        var t = (time - from.TimeSeconds) / range;

        switch (from.Interpolation)
        {
            case InterpolationMode.Step:
                return new Transform2D(from.Position, from.RotationDegrees, from.Scale);

            case InterpolationMode.Linear:
            default:
                var position = Lerp(from.Position, to.Position, t);
                var rotation = LerpRotation(from.RotationDegrees, to.RotationDegrees, t, from.ExplicitRotationDirection, from.ExplicitTotalRotationDegrees, to.ExplicitTotalRotationDegrees);
                var scale = Lerp(from.Scale, to.Scale, t);
                return new Transform2D(position, rotation, scale);
        }
    }

    private static Vector2D Lerp(Vector2D a, Vector2D b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    private static double LerpRotation(double fromDeg, double toDeg, double t,
        bool explicitDirection, double explicitFromTotal, double explicitToTotal)
    {
        if (explicitDirection)
        {
            var total = explicitFromTotal + (explicitToTotal - explicitFromTotal) * t;
            return total;
        }

        // Shortest path interpolation with wraparound
        var diff = ((toDeg - fromDeg) % 360 + 540) % 360 - 180;
        return fromDeg + diff * t;
    }
}
