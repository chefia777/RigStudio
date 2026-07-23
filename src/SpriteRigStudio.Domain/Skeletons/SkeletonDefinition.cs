using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Domain.Skeletons;

/// <summary>
/// Defines a hierarchical skeleton that can be reused across multiple character rigs.
/// </summary>
public class SkeletonDefinition
{
    /// <summary>Stable identifier for this skeleton.</summary>
    public SkeletonId SkeletonId { get; init; } = SkeletonId.New();

    /// <summary>Display name for this skeleton.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The ID of the root bone. Must reference a bone in <see cref="Bones"/>.</summary>
    public BoneId RootBoneId { get; set; }

    /// <summary>All bones in this skeleton, keyed by ID.</summary>
    public Dictionary<BoneId, BoneDefinition> Bones { get; init; } = new();

    /// <summary>Reference dimensions for scaling animations to different characters.</summary>
    public Size2D ReferenceDimensions { get; set; }

    /// <summary>Default ground anchor position in skeleton space.</summary>
    public Vector2D DefaultGroundAnchor { get; set; }

    /// <summary>Optional metadata dictionary.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets children of the specified bone.
    /// </summary>
    public IEnumerable<BoneDefinition> GetChildren(BoneId parentId)
    {
        return Bones.Values.Where(b => b.ParentBoneId == parentId);
    }

    /// <summary>
    /// Validates that the bone hierarchy contains no cycles.
    /// </summary>
    public bool HasCycles()
    {
        var visited = new HashSet<BoneId>();
        var inProgress = new HashSet<BoneId>();

        bool Dfs(BoneId id)
        {
            if (inProgress.Contains(id))
                return true;
            if (visited.Contains(id))
                return false;

            inProgress.Add(id);
            if (Bones.TryGetValue(id, out var bone) && bone.ParentBoneId.HasValue)
            {
                if (Dfs(bone.ParentBoneId.Value))
                    return true;
            }
            inProgress.Remove(id);
            visited.Add(id);
            return false;
        }

        foreach (var boneId in Bones.Keys)
        {
            if (Dfs(boneId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the root bone. Returns null if not found.
    /// </summary>
    public BoneDefinition? GetRootBone() =>
        Bones.TryGetValue(RootBoneId, out var root) ? root : null;

    public override string ToString() => $"{Name} ({Bones.Count} bones) [{SkeletonId}]";
}
