using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Domain.Skeletons;

/// <summary>
/// Defines a single bone within a skeleton definition.
/// Bones form a hierarchy via ParentBoneId.
/// </summary>
public class BoneDefinition
{
    /// <summary>Stable identifier for this bone.</summary>
    public BoneId BoneId { get; init; }

    /// <summary>Display name for the bone.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Anatomic or functional role for workflow assistance.</summary>
    public BoneRole Role { get; set; } = BoneRole.Custom;

    /// <summary>
    /// Identifier of the parent bone. Null for the root bone.
    /// </summary>
    public BoneId? ParentBoneId { get; set; }

    /// <summary>
    /// Local transform relative to the parent bone in rest (setup) pose.
    /// For the root bone, this is relative to the skeleton origin.
    /// </summary>
    public Transform2D RestLocalTransform { get; set; } = Transform2D.Identity;

    /// <summary>Reference length of the bone in pixels.</summary>
    public double ReferenceLength { get; set; } = 1.0;

    /// <summary>Constraints that apply to this bone during manipulation and evaluation.</summary>
    public BoneConstraints? Constraints { get; set; }

    /// <summary>Default render-order hint for parts bound to this bone.</summary>
    public int DefaultRenderOrderHint { get; set; }

    /// <summary>Optional metadata dictionary.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.Ordinal);

    public override string ToString() => $"{Name} ({Role}) [{BoneId}]";
}
