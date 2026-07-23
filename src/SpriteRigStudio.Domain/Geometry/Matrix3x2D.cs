using System;
using System.Numerics;

namespace SpriteRigStudio.Domain.Geometry;

/// <summary>
/// Application-owned 3x2 transformation matrix with double precision.
/// Used for composing 2D transforms in domain space.
/// </summary>
public readonly record struct Matrix3x2D(
    double M11, double M12,
    double M21, double M22,
    double M31, double M32)
{
    public static readonly Matrix3x2D Identity = new(1, 0, 0, 1, 0, 0);

    public static Matrix3x2D CreateTranslation(Vector2D position) =>
        new(1, 0, 0, 1, position.X, position.Y);

    public static Matrix3x2D CreateRotation(double angleDegrees)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return new(cos, sin, -sin, cos, 0, 0);
    }

    public static Matrix3x2D CreateScale(double scaleX, double scaleY) =>
        new(scaleX, 0, 0, scaleY, 0, 0);

    public static Matrix3x2D CreateScale(double uniform) =>
        new(uniform, 0, 0, uniform, 0, 0);

    public static Matrix3x2D operator *(Matrix3x2D a, Matrix3x2D b) =>
        new(
            a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,
            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22,
            a.M31 * b.M11 + a.M32 * b.M21 + b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + b.M32);

    public Vector2D Transform(Vector2D point) => new(
        point.X * M11 + point.Y * M21 + M31,
        point.X * M12 + point.Y * M22 + M32);

    public Matrix3x2D Inverted()
    {
        var det = M11 * M22 - M12 * M21;
        if (Math.Abs(det) < double.Epsilon)
            return Identity;
        var invDet = 1.0 / det;
        return new(
            M22 * invDet, -M12 * invDet,
            -M21 * invDet, M11 * invDet,
            (M21 * M32 - M22 * M31) * invDet,
            (M12 * M31 - M11 * M32) * invDet);
    }

    public static implicit operator Matrix3x2(Matrix3x2D m) =>
        new((float)m.M11, (float)m.M12, (float)m.M21, (float)m.M22, (float)m.M31, (float)m.M32);

    public static explicit operator Matrix3x2D(Matrix3x2 m) =>
        new(m.M11, m.M12, m.M21, m.M22, m.M31, m.M32);
}
