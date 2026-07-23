using Microsoft.Extensions.Logging;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Application.Animations;

/// <summary>
/// Application service for animation management use cases.
/// </summary>
public class AnimationService
{
    private readonly ILogger<AnimationService> _logger;

    public AnimationService(ILogger<AnimationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new animation clip.
    /// </summary>
    public AnimationClipDefinition CreateAnimation(string name, SkeletonId skeletonId, int fps = 12, double durationSeconds = 1.0)
    {
        return new AnimationClipDefinition
        {
            AnimationId = AnimationId.New(),
            Name = name,
            SkeletonId = skeletonId,
            FramesPerSecond = fps,
            DurationSeconds = durationSeconds,
            LoopMode = LoopMode.Loop
        };
    }

    /// <summary>
    /// Adds a keyframe to a bone track.
    /// </summary>
    public Result AddKeyframe(AnimationClipDefinition animation, BoneId boneId, TransformKeyframe keyframe)
    {
        if (!animation.BoneTracks.TryGetValue(boneId.ToKeyString(), out var track))
        {
            track = new BoneTrack { BoneId = boneId };
            animation.BoneTracks[boneId.ToKeyString()] = track;
        }

        // Check for duplicate time
        if (track.Keyframes.Any(k => Math.Abs(k.TimeSeconds - keyframe.TimeSeconds) < 0.001))
            return Result.Failure("DUPLICATE_KEYFRAME",
                $"A keyframe already exists at time {keyframe.TimeSeconds:F3}s for bone {boneId}.");

        track.Keyframes.Add(keyframe);
        track.Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));

        return Result.Success();
    }

    /// <summary>
    /// Updates an existing keyframe's values.
    /// </summary>
    public Result UpdateKeyframe(AnimationClipDefinition animation, BoneId boneId, double timeSeconds,
        Vector2D? position = null, double? rotationDegrees = null, Vector2D? scale = null)
    {
        if (!animation.BoneTracks.TryGetValue(boneId.ToKeyString(), out var track))
            return Result.Failure("TRACK_NOT_FOUND", $"No track found for bone {boneId}.");

        var keyframe = track.Keyframes.FirstOrDefault(k => Math.Abs(k.TimeSeconds - timeSeconds) < 0.001);
        if (keyframe == null)
            return Result.Failure("KEYFRAME_NOT_FOUND", $"No keyframe at time {timeSeconds:F3}s for bone {boneId}.");

        if (position.HasValue) keyframe.Position = position.Value;
        if (rotationDegrees.HasValue) keyframe.RotationDegrees = rotationDegrees.Value;
        if (scale.HasValue) keyframe.Scale = scale.Value;

        return Result.Success();
    }

    /// <summary>
    /// Deletes a keyframe at the specified time.
    /// </summary>
    public Result DeleteKeyframe(AnimationClipDefinition animation, BoneId boneId, double timeSeconds)
    {
        if (!animation.BoneTracks.TryGetValue(boneId.ToKeyString(), out var track))
            return Result.Failure("TRACK_NOT_FOUND", $"No track found for bone {boneId}.");

        var keyframe = track.Keyframes.FirstOrDefault(k => Math.Abs(k.TimeSeconds - timeSeconds) < 0.001);
        if (keyframe == null)
            return Result.Failure("KEYFRAME_NOT_FOUND", $"No keyframe at time {timeSeconds:F3}s for bone {boneId}.");

        track.Keyframes.Remove(keyframe);
        return Result.Success();
    }

    /// <summary>
    /// Moves keyframes by a time offset.
    /// </summary>
    public Result MoveKeyframes(AnimationClipDefinition animation, BoneId boneId, double fromTime, double toTime)
    {
        if (!animation.BoneTracks.TryGetValue(boneId.ToKeyString(), out var track))
            return Result.Failure("TRACK_NOT_FOUND", $"No track found for bone {boneId}.");

        // Check target time is not occupied
        if (track.Keyframes.Any(k => Math.Abs(k.TimeSeconds - toTime) < 0.001))
            return Result.Failure("TIME_OCCUPIED", $"A keyframe already exists at time {toTime:F3}s.");

        var keyframe = track.Keyframes.FirstOrDefault(k => Math.Abs(k.TimeSeconds - fromTime) < 0.001);
        if (keyframe == null)
            return Result.Failure("KEYFRAME_NOT_FOUND", $"No keyframe at time {fromTime:F3}s for bone {boneId}.");

        keyframe.TimeSeconds = toTime;
        track.Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));

        return Result.Success();
    }

    /// <summary>
    /// Updates animation settings.
    /// </summary>
    public void UpdateAnimationSettings(AnimationClipDefinition animation, int? fps = null, double? durationSeconds = null, LoopMode? loopMode = null)
    {
        if (fps.HasValue) animation.FramesPerSecond = fps.Value;
        if (durationSeconds.HasValue) animation.DurationSeconds = durationSeconds.Value;
        if (loopMode.HasValue) animation.LoopMode = loopMode.Value;
    }

    /// <summary>
    /// Duplicates an animation clip.
    /// </summary>
    public AnimationClipDefinition DuplicateAnimation(AnimationClipDefinition source)
    {
        var duplicate = new AnimationClipDefinition
        {
            AnimationId = AnimationId.New(),
            Name = source.Name + " (copy)",
            SkeletonId = source.SkeletonId,
            FramesPerSecond = source.FramesPerSecond,
            DurationSeconds = source.DurationSeconds,
            LoopMode = source.LoopMode,
            RootMotionPolicy = source.RootMotionPolicy
        };

        foreach (var (boneId, track) in source.BoneTracks)
        {
            duplicate.BoneTracks[boneId] = track.Clone();
        }

        foreach (var evt in source.Events)
        {
            duplicate.Events.Add(new AnimationEvent
            {
                TimeSeconds = evt.TimeSeconds,
                Name = evt.Name,
                Parameter = evt.Parameter
            });
        }

        return duplicate;
    }

    /// <summary>
    /// Gets the sampled frame count for an animation at a given FPS.
    /// </summary>
    public int GetSampledFrameCount(AnimationClipDefinition animation, int? exportFps = null)
    {
        var fps = exportFps ?? animation.FramesPerSecond;
        if (fps <= 0) return 0;

        var frameCount = (int)Math.Ceiling(animation.DurationSeconds * fps);

        // For loops, sample [0, duration) - no duplicate endpoint
        if (animation.LoopMode == LoopMode.Loop || animation.LoopMode == LoopMode.PingPong)
            return frameCount;

        // For once/clamp, include the duration endpoint
        return frameCount + 1;
    }

    /// <summary>
    /// Gets the sample time for a given frame index.
    /// </summary>
    public double GetSampleTime(AnimationClipDefinition animation, int frameIndex, int? exportFps = null)
    {
        var fps = exportFps ?? animation.FramesPerSecond;
        if (fps <= 0) return 0;

        return frameIndex / (double)fps;
    }
}
