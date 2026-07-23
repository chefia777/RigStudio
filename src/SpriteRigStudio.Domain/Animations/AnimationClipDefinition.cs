using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Skeletons;

namespace SpriteRigStudio.Domain.Animations;

/// <summary>
/// Defines a reusable skeletal animation clip.
/// Animation is produced by transforming body parts through the skeleton;
/// it is not a frame-by-frame redraw.
/// </summary>
public class AnimationClipDefinition
{
    /// <summary>Stable identifier for this animation.</summary>
    public AnimationId AnimationId { get; init; } = AnimationId.New();

    /// <summary>Display name for this animation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The skeleton this animation is authored for.</summary>
    public SkeletonId SkeletonId { get; set; }

    /// <summary>Frames per second for playback and export sampling.</summary>
    public int FramesPerSecond { get; set; } = 12;

    /// <summary>Duration of the animation in seconds.</summary>
    public double DurationSeconds { get; set; } = 1.0;

    /// <summary>How the animation behaves at the end of its duration.</summary>
    public LoopMode LoopMode { get; set; } = LoopMode.Loop;

    /// <summary>Per-bone animation tracks.</summary>
    public Dictionary<string, BoneTrack> BoneTracks { get; init; } = new();

    /// <summary>Animation events at specific times.</summary>
    public List<AnimationEvent> Events { get; init; } = new();

    /// <summary>How root motion is handled during retargeting.</summary>
    public RootMotionPolicy RootMotionPolicy { get; set; } = RootMotionPolicy.None;

    /// <summary>Optional metadata dictionary.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Frame count for standard sampling.</summary>
    public int FrameCount => (int)Math.Ceiling(DurationSeconds * FramesPerSecond);

    public override string ToString() => $"{Name} ({FramesPerSecond} FPS, {DurationSeconds}s) [{AnimationId}]";
}

/// <summary>
/// An event marker in an animation timeline.
/// </summary>
public class AnimationEvent
{
    /// <summary>Time in seconds when this event occurs.</summary>
    public double TimeSeconds { get; set; }

    /// <summary>Event name/type.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional parameter.</summary>
    public string? Parameter { get; set; }
}

/// <summary>
/// Determines how root motion from animation is applied.
/// </summary>
public enum RootMotionPolicy
{
    /// <summary>No root motion; ground anchor is fixed.</summary>
    None = 0,

    /// <summary>Root motion is applied to the character position.</summary>
    Apply,

    /// <summary>Root motion is applied but separated for procedural handling.</summary>
    Separate
}
