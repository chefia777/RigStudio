using Xunit;
using FluentAssertions;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Rigs;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Transforms;
using SpriteRigStudio.Rendering.Poses;

using System;

namespace SpriteRigStudio.Rendering.Tests;

public class PoseEvaluationTests
{
    [Fact]
    public void EvaluateSetupPose_WithSingleBone_ShouldReturnRootTransform()
    {
        var evaluator = new RigPoseEvaluator();
        var skeleton = new SkeletonDefinition { Name = "Test" };
        var root = new BoneDefinition
        {
            BoneId = BoneId.New(),
            Name = "Root",
            Role = BoneRole.Root,
            RestLocalTransform = new Transform2D(new Vector2D(100, 200), 0, new Vector2D(1, 1))
        };
        skeleton.Bones[root.BoneId.ToKeyString()] = root;
        skeleton.RootBoneId = root.BoneId;

        var rig = new CharacterRigDefinition
        {
            Name = "Test",
            SkeletonId = skeleton.SkeletonId,
            SetupTransform = new CharacterSetupTransform { Position = new Vector2D(50, 0) },
            GroundAnchor = Vector2D.Zero
        };

        var pose = evaluator.EvaluateSetupPose(skeleton, rig);

        pose.BoneWorldTransforms.Should().ContainKey(root.BoneId);
        var rootTransform = pose.BoneWorldTransforms[root.BoneId];
        rootTransform.M31.Should().BeApproximately(150, 0.001); // 100 + 50
        rootTransform.M32.Should().BeApproximately(200, 0.001);
    }

    [Fact]
    public void EvaluateAnimationPose_WithKeyframes_ShouldInterpolate()
    {
        var evaluator = new RigPoseEvaluator();
        var skeleton = new SkeletonDefinition { Name = "Test" };
        var root = new BoneDefinition
        {
            BoneId = BoneId.New(),
            Name = "Root",
            Role = BoneRole.Root,
            RestLocalTransform = Transform2D.Identity
        };
        skeleton.Bones[root.BoneId.ToKeyString()] = root;
        skeleton.RootBoneId = root.BoneId;

        var rig = new CharacterRigDefinition
        {
            Name = "Test",
            SkeletonId = skeleton.SkeletonId,
            SetupTransform = new CharacterSetupTransform(),
            GroundAnchor = Vector2D.Zero
        };

        var anim = new AnimationClipDefinition
        {
            Name = "Test",
            SkeletonId = skeleton.SkeletonId,
            FramesPerSecond = 10,
            DurationSeconds = 1.0,
            LoopMode = LoopMode.Loop
        };

        var track = new BoneTrack { BoneId = root.BoneId };
        track.Keyframes.Add(new TransformKeyframe
        {
            TimeSeconds = 0,
            Position = Vector2D.Zero,
            RotationDegrees = 0,
            Scale = new Vector2D(1, 1),
            Interpolation = InterpolationMode.Linear
        });
        track.Keyframes.Add(new TransformKeyframe
        {
            TimeSeconds = 1.0,
            Position = new Vector2D(100, 0),
            RotationDegrees = 0,
            Scale = new Vector2D(1, 1),
            Interpolation = InterpolationMode.Linear
        });
        anim.BoneTracks[root.BoneId.ToKeyString()] = track;

        // At 0.5s, should be at position (50, 0)
        var pose = evaluator.EvaluateAnimationPose(skeleton, rig, anim, 0.5);
        pose.BoneWorldTransforms[root.BoneId].M31.Should().BeApproximately(50, 0.001);
    }
}
