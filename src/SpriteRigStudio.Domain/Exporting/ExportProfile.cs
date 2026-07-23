using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;

namespace SpriteRigStudio.Domain.Exporting;

/// <summary>
/// Configuration for exporting animations to spritesheets and individual frames.
/// </summary>
public class ExportProfile
{
    /// <summary>Stable identifier for this export profile.</summary>
    public ExportProfileId ExportProfileId { get; init; } = ExportProfileId.New();

    /// <summary>Display name for this profile.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Width of each frame in pixels.</summary>
    public int FrameWidth { get; set; } = 300;

    /// <summary>Height of each frame in pixels.</summary>
    public int FrameHeight { get; set; } = 300;

    /// <summary>Number of columns in the spritesheet.</summary>
    public int Columns { get; set; } = 4;

    /// <summary>Number of rows in the spritesheet.</summary>
    public int Rows { get; set; } = 4;

    /// <summary>Frame rate for export sampling. If 0, uses the animation's FPS.</summary>
    public int FrameRate { get; set; }

    /// <summary>Background rendering mode.</summary>
    public BackgroundMode BackgroundMode { get; set; } = BackgroundMode.Transparent;

    /// <summary>Background color (used when BackgroundMode is SolidColor).</summary>
    public ColorRgba32 BackgroundColor { get; set; } = ColorRgba32.White;

    /// <summary>Image sampling mode.</summary>
    public SamplingMode SamplingMode { get; set; } = SamplingMode.NearestNeighbor;

    /// <summary>Position snapping mode.</summary>
    public PositionSnappingMode PositionSnappingMode { get; set; } = PositionSnappingMode.PartOrigin;

    /// <summary>Anchor pixel position in the frame where the ground anchor is placed.</summary>
    public Vector2D AnchorPixel { get; set; } = new(150, 260);

    /// <summary>Scaling mode (1.0 = no scaling).</summary>
    public double ScalingMode { get; set; } = 1.0;

    /// <summary>Overflow policy when character exceeds frame bounds.</summary>
    public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.FailExport;

    /// <summary>Whether to include the spritesheet image in export output.</summary>
    public bool IncludeSpritesheet { get; set; } = true;

    /// <summary>Whether to include individual frame images.</summary>
    public bool IncludeIndividualFrames { get; set; }

    /// <summary>Whether to include metadata JSON.</summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>Optional file naming pattern.</summary>
    public string? FileNamingPattern { get; set; }

    /// <summary>Output directory relative to project Exports folder.</summary>
    public string? OutputDirectory { get; set; }

    /// <summary>Spritesheet total width.</summary>
    public int SheetWidth => FrameWidth * Columns;

    /// <summary>Spritesheet total height.</summary>
    public int SheetHeight => FrameHeight * Rows;

    /// <summary>Maximum frames this spritesheet can hold.</summary>
    public int MaxFrames => Columns * Rows;

    public ExportProfile Clone() => new()
    {
        ExportProfileId = ExportProfileId.New(),
        Name = Name + " (copy)",
        FrameWidth = FrameWidth,
        FrameHeight = FrameHeight,
        Columns = Columns,
        Rows = Rows,
        FrameRate = FrameRate,
        BackgroundMode = BackgroundMode,
        BackgroundColor = BackgroundColor,
        SamplingMode = SamplingMode,
        PositionSnappingMode = PositionSnappingMode,
        AnchorPixel = AnchorPixel,
        ScalingMode = ScalingMode,
        OverflowPolicy = OverflowPolicy,
        IncludeSpritesheet = IncludeSpritesheet,
        IncludeIndividualFrames = IncludeIndividualFrames,
        IncludeMetadata = IncludeMetadata,
        FileNamingPattern = FileNamingPattern,
        OutputDirectory = OutputDirectory
    };

    public override string ToString() => $"{Name} ({FrameWidth}x{FrameHeight}, {Columns}x{Rows}) [{ExportProfileId}]";
}
