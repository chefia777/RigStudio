using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;

namespace SpriteRigStudio.Domain.Skeletons;

/// <summary>
/// Creates the built-in default humanoid skeleton preset.
/// </summary>
public static class DefaultHumanoidSkeleton
{
    /// <summary>
    /// Creates a new skeleton definition with the standard humanoid bone hierarchy.
    /// </summary>
    public static SkeletonDefinition Create()
    {
        var skeleton = new SkeletonDefinition
        {
            SkeletonId = SkeletonId.New(),
            Name = "Humanoid",
            ReferenceDimensions = new Size2D(300, 500),
            DefaultGroundAnchor = new Vector2D(150, 20),
            Metadata = { ["preset"] = "default-humanoid" }
        };

        // Create bones
        var root = CreateBone(BoneRole.Root, "Root", null, new Vector2D(150, 400));
        var pelvis = CreateBone(BoneRole.Pelvis, "Pelvis", root.BoneId, new Vector2D(0, -30));
        var torso = CreateBone(BoneRole.Torso, "Torso", pelvis.BoneId, new Vector2D(0, -60));
        var chest = CreateBone(BoneRole.Chest, "Chest", torso.BoneId, new Vector2D(0, -50));
        var neck = CreateBone(BoneRole.Neck, "Neck", chest.BoneId, new Vector2D(0, -25));
        var head = CreateBone(BoneRole.Head, "Head", neck.BoneId, new Vector2D(0, -40));
        var leftShoulder = CreateBone(BoneRole.LeftShoulder, "Left Shoulder", chest.BoneId, new Vector2D(-25, 5));
        var leftUpperArm = CreateBone(BoneRole.LeftUpperArm, "Left Upper Arm", leftShoulder.BoneId, new Vector2D(-15, -35));
        var leftForearm = CreateBone(BoneRole.LeftForearm, "Left Forearm", leftUpperArm.BoneId, new Vector2D(0, -35));
        var leftHand = CreateBone(BoneRole.LeftHand, "Left Hand", leftForearm.BoneId, new Vector2D(0, -20));
        var rightShoulder = CreateBone(BoneRole.RightShoulder, "Right Shoulder", chest.BoneId, new Vector2D(25, 5));
        var rightUpperArm = CreateBone(BoneRole.RightUpperArm, "Right Upper Arm", rightShoulder.BoneId, new Vector2D(15, -35));
        var rightForearm = CreateBone(BoneRole.RightForearm, "Right Forearm", rightUpperArm.BoneId, new Vector2D(0, -35));
        var rightHand = CreateBone(BoneRole.RightHand, "Right Hand", rightForearm.BoneId, new Vector2D(0, -20));
        var leftHip = CreateBone(BoneRole.LeftHip, "Left Hip", pelvis.BoneId, new Vector2D(-20, 5));
        var leftThigh = CreateBone(BoneRole.LeftThigh, "Left Thigh", leftHip.BoneId, new Vector2D(0, -60));
        var leftLowerLeg = CreateBone(BoneRole.LeftLowerLeg, "Left Lower Leg", leftThigh.BoneId, new Vector2D(0, -60));
        var leftFoot = CreateBone(BoneRole.LeftFoot, "Left Foot", leftLowerLeg.BoneId, new Vector2D(0, -25));
        var rightHip = CreateBone(BoneRole.RightHip, "Right Hip", pelvis.BoneId, new Vector2D(20, 5));
        var rightThigh = CreateBone(BoneRole.RightThigh, "Right Thigh", rightHip.BoneId, new Vector2D(0, -60));
        var rightLowerLeg = CreateBone(BoneRole.RightLowerLeg, "Right Lower Leg", rightThigh.BoneId, new Vector2D(0, -60));
        var rightFoot = CreateBone(BoneRole.RightFoot, "Right Foot", rightLowerLeg.BoneId, new Vector2D(0, -25));
        var cape = CreateBone(BoneRole.Cape, "Cape", chest.BoneId, new Vector2D(0, 5));
        var hair = CreateBone(BoneRole.Hair, "Hair", head.BoneId, new Vector2D(0, 10));

        var allBones = new[] {
            root, pelvis, torso, chest, neck, head,
            leftShoulder, leftUpperArm, leftForearm, leftHand,
            rightShoulder, rightUpperArm, rightForearm, rightHand,
            leftHip, leftThigh, leftLowerLeg, leftFoot,
            rightHip, rightThigh, rightLowerLeg, rightFoot,
            cape, hair
        };

        foreach (var bone in allBones)
        {
            skeleton.Bones[bone.BoneId.ToKeyString()] = bone;
        }

        skeleton.RootBoneId = root.BoneId;
        return skeleton;
    }

    private static BoneDefinition CreateBone(BoneRole role, string name, BoneId? parentId, Vector2D localPosition)
    {
        return new BoneDefinition
        {
            BoneId = BoneId.New(),
            Name = name,
            Role = role,
            ParentBoneId = parentId,
            RestLocalTransform = new Transform2D(localPosition, 0, new Vector2D(1, 1)),
            ReferenceLength = Math.Abs(localPosition.Y) > 0 ? Math.Abs(localPosition.Y) : 1.0,
            Constraints = new BoneConstraints { PreserveLength = true }
        };
    }
}
