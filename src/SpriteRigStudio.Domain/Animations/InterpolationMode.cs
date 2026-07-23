namespace SpriteRigStudio.Domain.Animations;

/// <summary>
/// Interpolation method between two keyframes.
/// </summary>
public enum InterpolationMode
{
    /// <summary>No interpolation; value jumps at the keyframe boundary.</summary>
    Step = 0,

    /// <summary>Linear interpolation between keyframes.</summary>
    Linear,

    /// <summary>Smooth (cubic) interpolation using tangent data.</summary>
    Smooth
}
