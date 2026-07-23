using System;
using System.Numerics;

namespace SpriteRigStudio.Domain.Geometry;

/// <summary>
/// Application-owned 2D vector with double precision.
/// Domain coordinates: Y increases upward.
/// </summary>
public readonly record struct Vector2D(double X, double Y)
{
    public static readonly Vector2D Zero = new(0, 0);
    public static readonly Vector2D UnitX = new(1, 0);
    public static readonly Vector2D UnitY = new(0, 1);

    public double Length => Math.Sqrt(X * X + Y * Y);
    public double LengthSquared => X * X + Y * Y;
    public Vector2D Normalized => Length > 0 ? this / Length : Zero;

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator -(Vector2D v) => new(-v.X, -v.Y);
    public static Vector2D operator *(Vector2D v, double s) => new(v.X * s, v.Y * s);
    public static Vector2D operator *(double s, Vector2D v) => new(v.X * s, v.Y * s);
    public static Vector2D operator /(Vector2D v, double s) => new(v.X / s, v.Y / s);

    public static implicit operator Vector2(Vector2D v) => new((float)v.X, (float)v.Y);
    public static explicit operator Vector2D(Vector2 v) => new(v.X, v.Y);

    public double Dot(Vector2D other) => X * other.X + Y * other.Y;
    public double Cross(Vector2D other) => X * other.Y - Y * other.X;

    public Vector2D Rotate(double angleDegrees)
    {
        var rad = angleDegrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return new(X * cos - Y * sin, X * sin + Y * cos);
    }

    public override string ToString() => $"({X:F2}, {Y:F2})";
}
