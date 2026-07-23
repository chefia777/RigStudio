namespace SpriteRigStudio.Domain.Geometry;

/// <summary>Application-owned axis-aligned rectangle with double precision.</summary>
public readonly record struct RectangleD(double X, double Y, double Width, double Height)
{
    public static readonly RectangleD Empty = new(0, 0, 0, 0);

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public Vector2D Position => new(X, Y);
    public Vector2D Size => new(Width, Height);
    public Vector2D Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Vector2D point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    public bool Intersects(RectangleD other) =>
        Left < other.Right && Right > other.Left &&
        Top < other.Bottom && Bottom > other.Top;

    public RectangleD Inflate(double amount) => new(
        X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);

    public override string ToString() => $"({X:F2}, {Y:F2}) {Width:F2} x {Height:F2}";
}
