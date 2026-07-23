using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Domain.Rigs;

/// <summary>
/// The complete character rig setup transform.
/// Used to align the entire skeleton to the original character artwork.
/// Remains separate from animation transforms.
/// </summary>
public class CharacterSetupTransform
{
    /// <summary>Position offset of the complete rig.</summary>
    public Vector2D Position { get; set; }

    /// <summary>Rotation of the complete rig in degrees.</summary>
    public double RotationDegrees { get; set; }

    /// <summary>Uniform scale factor.</summary>
    public double UniformScale { get; set; } = 1.0;

    /// <summary>Whether the rig is flipped horizontally.</summary>
    public bool FlipHorizontal { get; set; }

    /// <summary>Whether the transform is locked.</summary>
    public bool Locked { get; set; }

    /// <summary>Whether position snaps to integer pixels.</summary>
    public bool SnapToPixels { get; set; } = true;

    /// <summary>Computes the setup transform matrix.</summary>
    public Matrix3x2D ToMatrix()
    {
        var scaleX = FlipHorizontal ? -UniformScale : UniformScale;
        var t = Matrix3x2D.CreateTranslation(Position);
        var r = Matrix3x2D.CreateRotation(RotationDegrees);
        var s = Matrix3x2D.CreateScale(scaleX, UniformScale);
        return t * r * s;
    }

    /// <summary>Resets all values to defaults.</summary>
    public void Reset()
    {
        Position = Vector2D.Zero;
        RotationDegrees = 0;
        UniformScale = 1.0;
        FlipHorizontal = false;
    }

    public CharacterSetupTransform Clone() => new()
    {
        Position = Position,
        RotationDegrees = RotationDegrees,
        UniformScale = UniformScale,
        FlipHorizontal = FlipHorizontal,
        Locked = Locked,
        SnapToPixels = SnapToPixels
    };
}
