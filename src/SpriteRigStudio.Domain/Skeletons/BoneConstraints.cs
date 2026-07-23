namespace SpriteRigStudio.Domain.Skeletons;

/// <summary>
/// Constraints that affect bone manipulation and evaluation.
/// Constraints do not silently rewrite stored values.
/// </summary>
public class BoneConstraints
{
    /// <summary>Minimum rotation in degrees (null = no minimum).</summary>
    public double? MinRotationDegrees { get; set; }

    /// <summary>Maximum rotation in degrees (null = no maximum).</summary>
    public double? MaxRotationDegrees { get; set; }

    /// <summary>Whether position is locked.</summary>
    public bool PositionLocked { get; set; }

    /// <summary>Whether rotation is locked.</summary>
    public bool RotationLocked { get; set; }

    /// <summary>Whether scale is locked.</summary>
    public bool ScaleLocked { get; set; }

    /// <summary>Whether bone length is preserved during manipulation.</summary>
    public bool PreserveLength { get; set; } = true;

    /// <summary>Maximum stretch factor (1.0 = no stretch).</summary>
    public double MaximumStretch { get; set; } = 1.0;

    /// <summary>Preferred bend direction for IK (positive = clockwise from bone direction).</summary>
    public double? PreferredBendDirectionDegrees { get; set; }
}
