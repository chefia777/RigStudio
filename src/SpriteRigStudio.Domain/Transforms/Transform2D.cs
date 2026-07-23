using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Transforms;

/// <summary>
/// Application-owned 2D transform composed of position, rotation, and scale.
/// Transform multiplication order: Translate * Rotate * Scale (TRS).
/// Domain coordinates: Y increases upward. Positive rotation is counterclockwise.
/// </summary>
public readonly record struct Transform2D(Vector2D Position, double RotationDegrees, Vector2D Scale)
{
    public static readonly Transform2D Identity = new(Vector2D.Zero, 0, new Vector2D(1, 1));

    public Transform2D() : this(Vector2D.Zero, 0, new Vector2D(1, 1)) { }

    public bool IsIdentity =>
        Position == Vector2D.Zero &&
        Math.Abs(RotationDegrees) < 1e-9 &&
        Math.Abs(Scale.X - 1) < 1e-9 &&
        Math.Abs(Scale.Y - 1) < 1e-9;

    /// <summary>
    /// Computes the local-to-parent transformation matrix.
    /// Order: Translate * Rotate * Scale.
    /// </summary>
    public Matrix3x2D ToMatrix()
    {
        var t = Matrix3x2D.CreateTranslation(Position);
        var r = Matrix3x2D.CreateRotation(RotationDegrees);
        var s = Matrix3x2D.CreateScale(Scale.X, Scale.Y);
        return t * r * s;
    }

    /// <summary>
    /// Composes two transforms. The resulting transform applies 'parent' then 'child'.
    /// </summary>
    public static Transform2D Compose(Transform2D parent, Transform2D child)
    {
        var parentMatrix = parent.ToMatrix();
        var childMatrix = child.ToMatrix();
        var combined = parentMatrix * childMatrix;

        // Decompose: extract position, then decompose rotation+scale
        var position = new Vector2D(combined.M31, combined.M32);
        var scaleX = Math.Sqrt(combined.M11 * combined.M11 + combined.M12 * combined.M12);
        var scaleY = Math.Sqrt(combined.M21 * combined.M21 + combined.M22 * combined.M22);
        var rotation = Math.Atan2(combined.M12 / scaleX, combined.M11 / scaleX) * 180.0 / Math.PI;

        return new Transform2D(position, rotation, new Vector2D(scaleX, scaleY));
    }

    public override string ToString() =>
        $"T({Position.X:F1},{Position.Y:F1}) R({RotationDegrees:F1}) S({Scale.X:F2},{Scale.Y:F2})";
}
