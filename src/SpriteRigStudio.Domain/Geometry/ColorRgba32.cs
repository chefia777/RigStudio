namespace SpriteRigStudio.Domain.Geometry;

/// <summary>
/// Application-owned 32-bit RGBA color value.
/// Stored as a packed uint (0xAARRGGBB).
/// </summary>
public readonly record struct ColorRgba32(uint Value)
{
    public byte Alpha => (byte)(Value >> 24);
    public byte Red => (byte)(Value >> 16);
    public byte Green => (byte)(Value >> 8);
    public byte Blue => (byte)(Value);

    public static readonly ColorRgba32 Transparent = new(0x00000000);
    public static readonly ColorRgba32 White = new(0xFFFFFFFF);
    public static readonly ColorRgba32 Black = new(0xFF000000);

    public ColorRgba32(byte red, byte green, byte blue, byte alpha = 255)
        : this((uint)((alpha << 24) | (red << 16) | (green << 8) | blue))
    {
    }

    public static ColorRgba32 FromRgba(byte r, byte g, byte b, byte a = 255) => new(r, g, b, a);

    public override string ToString() => $"#{Value:X8}";
}
