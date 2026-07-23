using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Animations;

/// <summary>
/// A single keyframe storing transform values at a specific time.
/// </summary>
public class TransformKeyframe
{
    /// <summary>Time in seconds from the start of the animation.</summary>
    public double TimeSeconds { get; set; }

    /// <summary>Position value.</summary>
    public Vector2D Position { get; set; }

    /// <summary>Rotation value in degrees.</summary>
    public double RotationDegrees { get; set; }

    /// <summary>Scale value.</summary>
    public Vector2D Scale { get; set; } = new(1, 1);

    /// <summary>Interpolation mode from this keyframe to the next.</summary>
    public InterpolationMode Interpolation { get; set; } = InterpolationMode.Linear;

    /// <summary>
    /// Tangent data for smooth interpolation.
    /// Only used when Interpolation is Smooth.
    /// </summary>
    public TangentData? TangentData { get; set; }

    /// <summary>
    /// If true, rotation interpolation uses the explicit direction.
    /// If false, the shortest path is preferred.
    /// </summary>
    public bool ExplicitRotationDirection { get; set; }

    /// <summary>
    /// Explicit total rotation in degrees (used when ExplicitRotationDirection is true).
    /// For example, 720 means two full rotations regardless of the angular difference.
    /// </summary>
    public double ExplicitTotalRotationDegrees { get; set; }

    public TransformKeyframe Clone() => new()
    {
        TimeSeconds = TimeSeconds,
        Position = Position,
        RotationDegrees = RotationDegrees,
        Scale = Scale,
        Interpolation = Interpolation,
        TangentData = TangentData?.Clone(),
        ExplicitRotationDirection = ExplicitRotationDirection,
        ExplicitTotalRotationDegrees = ExplicitTotalRotationDegrees
    };
}

/// <summary>
/// Tangent data for smooth (cubic) interpolation.
/// </summary>
public class TangentData
{
    public Vector2D InTangent { get; set; }
    public Vector2D OutTangent { get; set; }

    public TangentData Clone() => new()
    {
        InTangent = InTangent,
        OutTangent = OutTangent
    };
}
