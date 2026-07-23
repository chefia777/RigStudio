using SpriteRigStudio.Domain.Common;

namespace SpriteRigStudio.Domain.Animations;

/// <summary>
/// A track of keyframes animating a single bone.
/// </summary>
public class BoneTrack
{
    /// <summary>The bone being animated.</summary>
    public BoneId BoneId { get; init; }

    /// <summary>Keyframes sorted by time.</summary>
    public List<TransformKeyframe> Keyframes { get; init; } = new();

    /// <summary>Whether this bone is animated (has at least one keyframe).</summary>
    public bool HasKeyframes => Keyframes.Count > 0;

    /// <summary>
    /// Gets the keyframes sorted by time.
    /// </summary>
    public IReadOnlyList<TransformKeyframe> GetSortedKeyframes()
    {
        Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
        return Keyframes;
    }

    public BoneTrack Clone() => new()
    {
        BoneId = BoneId,
        Keyframes = Keyframes.Select(k => k.Clone()).ToList()
    };
}
