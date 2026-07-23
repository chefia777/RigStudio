namespace SpriteRigStudio.Domain.Geometry;

/// <summary>Application-owned 2D size value.</summary>
public readonly record struct Size2D(double Width, double Height)
{
    public static readonly Size2D Zero = new(0, 0);

    public bool IsZero => Width == 0 && Height == 0;

    public override string ToString() => $"{Width:F2} x {Height:F2}";
}
