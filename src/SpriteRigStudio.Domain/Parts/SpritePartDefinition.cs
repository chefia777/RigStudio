using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Exporting;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Domain.Parts;

/// <summary>
/// Defines a single visible sprite part that is bound to a bone.
/// </summary>
public class SpritePartDefinition
{
    /// <summary>Stable identifier for this sprite part.</summary>
    public SpritePartId SpritePartId { get; init; } = SpritePartId.New();

    /// <summary>Display name for this part.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>How the source pixels are obtained.</summary>
    public SourceType SourceType { get; set; } = SourceType.FlattenedImageMask;

    /// <summary>
    /// Reference to the source image (managed asset relative path).
    /// For FlattenedImageMask, refers to the full character image.
    /// For SeparateImage, refers to the individual part image.
    /// </summary>
    public string? ImageReference { get; set; }

    /// <summary>Rectangle within the source image for this part (null = full image).</summary>
    public RectangleD? SourceRectangle { get; set; }

    /// <summary>Identifier of the mask used for this part (for FlattenedImageMask).</summary>
    public Guid? MaskId { get; set; }

    /// <summary>Pivot point in part-local space.</summary>
    public Vector2D Pivot { get; set; }

    /// <summary>The bone this part is bound to.</summary>
    public BoneId? BoundBoneId { get; set; }

    /// <summary>Local transform relative to the bound bone.</summary>
    public Transform2D LocalSetupTransform { get; set; } = Transform2D.Identity;

    /// <summary>Render order (lower = drawn first / further back).</summary>
    public int RenderOrder { get; set; }

    /// <summary>Whether this part is visible.</summary>
    public bool Visibility { get; set; } = true;

    /// <summary>Sampling mode override (null = use export profile default).</summary>
    public SamplingMode? SamplingMode { get; set; }

    /// <summary>Opacity (0 = transparent, 1 = fully opaque).</summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>Whether this part is rigid (no mesh deformation).</summary>
    public bool IsRigid { get; set; } = true;

    /// <summary>Optional metadata dictionary.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.Ordinal);

    public override string ToString() => $"{Name} [{SpritePartId}]";
}
