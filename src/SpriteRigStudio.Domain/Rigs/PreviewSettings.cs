using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Rigs;

public class PreviewSettings
{
    public bool ShowSkeleton { get; set; } = true;
    public bool ShowArtwork { get; set; } = true;
    public bool ShowMasks { get; set; }
    public bool ShowPartBounds { get; set; }
    public bool ShowPivots { get; set; }
    public double SkeletonOpacity { get; set; } = 1.0;
    public double ArtworkOpacity { get; set; } = 1.0;
    public ColorRgba32 BackgroundColor { get; set; } = ColorRgba32.FromRgba(45, 45, 48);
}
