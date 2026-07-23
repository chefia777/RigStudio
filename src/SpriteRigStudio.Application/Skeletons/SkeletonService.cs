using System;
using Microsoft.Extensions.Logging;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Application.Skeletons;

/// <summary>
/// Application service for skeleton management use cases.
/// </summary>
public class SkeletonService
{
    private readonly ILogger<SkeletonService> _logger;

    public SkeletonService(ILogger<SkeletonService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new skeleton with a root bone.
    /// </summary>
    public SkeletonDefinition CreateSkeleton(string name)
    {
        var skeleton = new SkeletonDefinition
        {
            SkeletonId = SkeletonId.New(),
            Name = name
        };

        var rootBone = new BoneDefinition
        {
            BoneId = BoneId.New(),
            Name = "Root",
            Role = BoneRole.Root,
            ParentBoneId = null,
            RestLocalTransform = Transform2D.Identity,
            ReferenceLength = 1.0
        };

        skeleton.Bones[rootBone.BoneId.ToKeyString()] = rootBone;
        skeleton.RootBoneId = rootBone.BoneId;

        return skeleton;
    }

    /// <summary>
    /// Adds a bone to a skeleton.
    /// </summary>
    public Result AddBone(SkeletonDefinition skeleton, string name, BoneRole role, BoneId parentId, Transform2D localTransform)
    {
        if (!skeleton.Bones.ContainsKey(parentId.ToKeyString()))
            return Result.Failure("PARENT_NOT_FOUND", $"Parent bone {parentId} not found.");

        var bone = new BoneDefinition
        {
            BoneId = BoneId.New(),
            Name = name,
            Role = role,
            ParentBoneId = parentId,
            RestLocalTransform = localTransform,
            ReferenceLength = Math.Max(1.0, localTransform.Position.Length)
        };

        skeleton.Bones[bone.BoneId.ToKeyString()] = bone;
        return Result.Success();
    }

    /// <summary>
    /// Removes a bone and reparents its children to the parent of the removed bone.
    /// </summary>
    public Result RemoveBone(SkeletonDefinition skeleton, BoneId boneId)
    {
        if (!skeleton.Bones.TryGetValue(boneId.ToKeyString(), out var bone))
            return Result.Failure("BONE_NOT_FOUND", $"Bone {boneId} not found.");

        if (bone.BoneId == skeleton.RootBoneId)
            return Result.Failure("CANNOT_REMOVE_ROOT", "Cannot remove the root bone.");

        // Reparent children to the removed bone's parent
        var parentId = bone.ParentBoneId;
        foreach (var child in skeleton.GetChildren(boneId))
        {
            child.ParentBoneId = parentId;
        }

        skeleton.Bones.Remove(boneId.ToKeyString());
        return Result.Success();
    }

    /// <summary>
    /// Reparents a bone to a new parent.
    /// </summary>
    public Result ReparentBone(SkeletonDefinition skeleton, BoneId boneId, BoneId newParentId)
    {
        if (!skeleton.Bones.TryGetValue(boneId.ToKeyString(), out var bone))
            return Result.Failure("BONE_NOT_FOUND", $"Bone {boneId} not found.");

        if (!skeleton.Bones.ContainsKey(newParentId.ToKeyString()))
            return Result.Failure("PARENT_NOT_FOUND", $"New parent bone {newParentId} not found.");

        if (boneId == newParentId)
            return Result.Failure("SELF_PARENT", "A bone cannot be its own parent.");

        // Check for cycles
        if (WouldCreateCycle(skeleton, boneId, newParentId))
            return Result.Failure("CYCLE", "Reparenting would create a cycle.");

        bone.ParentBoneId = newParentId;
        return Result.Success();
    }

    private static bool WouldCreateCycle(SkeletonDefinition skeleton, BoneId boneId, BoneId potentialParent)
    {
        var current = potentialParent;
        var visited = new HashSet<BoneId>();

        while (current != boneId)
        {
            if (!visited.Add(current))
                return true;

            if (!skeleton.Bones.TryGetValue(current.ToKeyString(), out var bone) || !bone.ParentBoneId.HasValue)
                return false;

            current = bone.ParentBoneId.Value;
        }

        return true; // potential parent is a descendant of boneId
    }

    /// <summary>
    /// Duplicates an existing skeleton.
    /// </summary>
    public SkeletonDefinition DuplicateSkeleton(SkeletonDefinition source)
    {
        var idMap = new Dictionary<BoneId, BoneId>();
        var duplicate = new SkeletonDefinition
        {
            SkeletonId = SkeletonId.New(),
            Name = source.Name + " (copy)",
            ReferenceDimensions = source.ReferenceDimensions,
            DefaultGroundAnchor = source.DefaultGroundAnchor
        };

        foreach (var (boneKey, bone) in source.Bones)
        {
            var boneId = new BoneId(Guid.Parse(boneKey));
            var newId = BoneId.New();
            idMap[boneId] = newId;

            duplicate.Bones[newId.ToKeyString()] = new BoneDefinition
            {
                BoneId = newId,
                Name = bone.Name,
                Role = bone.Role,
                ParentBoneId = bone.ParentBoneId.HasValue ? idMap[bone.ParentBoneId.Value] : null,
                RestLocalTransform = bone.RestLocalTransform,
                ReferenceLength = bone.ReferenceLength,
                DefaultRenderOrderHint = bone.DefaultRenderOrderHint
            };
        }

        duplicate.RootBoneId = idMap[source.RootBoneId];
        return duplicate;
    }

    /// <summary>
    /// Mirrors a bone chain (left to right or right to left).
    /// </summary>
    public void MirrorBoneChain(SkeletonDefinition skeleton, BoneId sourceBoneId, bool toRight)
    {
        if (!skeleton.Bones.TryGetValue(sourceBoneId.ToKeyString(), out var sourceBone))
            return;

        // Find the corresponding bone on the other side by swapping Left/Right in role
        var targetRole = GetMirrorRole(sourceBone.Role);
        if (targetRole == sourceBone.Role)
            return;

        var targetBone = skeleton.Bones.Values
            .FirstOrDefault(b => b.Role == targetRole && b.ParentBoneId == sourceBone.ParentBoneId);

        if (targetBone == null)
            return;

        // Mirror: negate X position, negate Z rotation
        var mirrorPosition = sourceBone.RestLocalTransform.Position with { X = -sourceBone.RestLocalTransform.Position.X };
        targetBone.RestLocalTransform = new Transform2D(
            mirrorPosition,
            -sourceBone.RestLocalTransform.RotationDegrees,
            sourceBone.RestLocalTransform.Scale);
    }

    private static BoneRole GetMirrorRole(BoneRole role) => role switch
    {
        BoneRole.LeftShoulder => BoneRole.RightShoulder,
        BoneRole.RightShoulder => BoneRole.LeftShoulder,
        BoneRole.LeftUpperArm => BoneRole.RightUpperArm,
        BoneRole.RightUpperArm => BoneRole.LeftUpperArm,
        BoneRole.LeftForearm => BoneRole.RightForearm,
        BoneRole.RightForearm => BoneRole.LeftForearm,
        BoneRole.LeftHand => BoneRole.RightHand,
        BoneRole.RightHand => BoneRole.LeftHand,
        BoneRole.LeftHip => BoneRole.RightHip,
        BoneRole.RightHip => BoneRole.LeftHip,
        BoneRole.LeftThigh => BoneRole.RightThigh,
        BoneRole.RightThigh => BoneRole.LeftThigh,
        BoneRole.LeftLowerLeg => BoneRole.RightLowerLeg,
        BoneRole.RightLowerLeg => BoneRole.LeftLowerLeg,
        BoneRole.LeftFoot => BoneRole.RightFoot,
        BoneRole.RightFoot => BoneRole.LeftFoot,
        _ => role
    };
}
