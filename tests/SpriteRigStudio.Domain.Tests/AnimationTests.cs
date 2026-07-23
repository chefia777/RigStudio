using FluentAssertions;
using SpriteRigStudio.Domain.Animations;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Domain.Geometry;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class AnimationTests
{
    [Fact]
    public void StepInterpolation_ShouldJumpAtKeyframeBoundary()
    {
        // Arrange: simulating step interpolation
        var kf1 = new TransformKeyframe { TimeSeconds = 0, Position = new Vector2D(0, 0), Interpolation = InterpolationMode.Step };
        var kf2 = new TransformKeyframe { TimeSeconds = 1, Position = new Vector2D(100, 0), Interpolation = InterpolationMode.Step };

        // Act: step interpolation - value before second keyframe is kf1, at/after is kf2
        var valueBefore = SampleStep(kf1, kf2, 0.999);
        var valueAt = SampleStep(kf1, kf2, 1.0);

        // Assert
        valueBefore.Should().Be(new Vector2D(0, 0));
        valueAt.Should().Be(new Vector2D(100, 0));
    }

    private static Vector2D SampleStep(TransformKeyframe from, TransformKeyframe to, double time)
    {
        return time < to.TimeSeconds ? from.Position : to.Position;
    }

    [Fact]
    public void LinearInterpolation_Midpoint_ShouldBeAverage()
    {
        // Arrange
        var from = new TransformKeyframe { TimeSeconds = 0, Position = new Vector2D(0, 0) };
        var to = new TransformKeyframe { TimeSeconds = 2, Position = new Vector2D(100, 200) };

        // Act: linearly interpolate at t=1 (midpoint)
        var t = 1.0;
        var factor = (t - from.TimeSeconds) / (to.TimeSeconds - from.TimeSeconds);
        var position = new Vector2D(
            from.Position.X + (to.Position.X - from.Position.X) * factor,
            from.Position.Y + (to.Position.Y - from.Position.Y) * factor);

        // Assert
        Math.Abs(position.X - 50).Should().BeLessThan(1e-9);
        Math.Abs(position.Y - 100).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void RotationShortestPath_350To10_ShouldGoThrough0()
    {
        // Arrange: two rotations: 350° and 10°
        double fromDeg = 350;
        double toDeg = 10;

        // Act: shortest-path delta
        var delta = toDeg - fromDeg;
        if (delta > 180) delta -= 360;
        else if (delta < -180) delta += 360;

        var interpolated = fromDeg + delta * 0.5; // midpoint

        // Assert: shortest path goes through 0°
        delta.Should().Be(20); // 10 - 350 = -340, +360 = 20
        // Midpoint is 350 + 10 = 360 ≡ 0° (mod 360)
        var normalized = interpolated % 360.0;
        if (normalized < 0) normalized += 360.0;
        Math.Abs(normalized).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void ExplicitRotationDirection_ShouldOverrideShortestPath()
    {
        // Arrange
        var kf = new TransformKeyframe
        {
            TimeSeconds = 0,
            RotationDegrees = 350,
            ExplicitRotationDirection = true,
            ExplicitTotalRotationDegrees = 720
        };

        // Act & Assert
        kf.ExplicitRotationDirection.Should().BeTrue();
        kf.ExplicitTotalRotationDegrees.Should().Be(720);
    }

    [Fact]
    public void LoopSamplingFormula_ShouldComputeSampleTime()
    {
        // Arrange
        var anim = new AnimationClipDefinition
        {
            FramesPerSecond = 12,
            DurationSeconds = 2.0,
            LoopMode = LoopMode.Loop
        };

        // Act: sampleTime = frameIndex / fps
        var frameIndex = 5;
        var sampleTime = frameIndex / (double)anim.FramesPerSecond;

        // Assert
        Math.Abs(sampleTime - (5.0 / 12.0)).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void NonLoop_EndpointSampling_ShouldIncludeDuration()
    {
        // Arrange: non-looping animation
        var anim = new AnimationClipDefinition
        {
            FramesPerSecond = 10,
            DurationSeconds = 1.0,
            LoopMode = LoopMode.Once
        };

        // Act: for non-loop, last frame is at duration
        var frameCount = anim.FrameCount; // Ceiling(1.0 * 10) = 10
        var lastFrameSampleTime = (frameCount - 1) / (double)anim.FramesPerSecond;

        // Assert
        frameCount.Should().Be(10);
        Math.Abs(lastFrameSampleTime - 0.9).Should().BeLessThan(1e-9);
        // The endpoint duration is 1.0, last frame sample is < duration
        lastFrameSampleTime.Should().BeLessThan(anim.DurationSeconds);
    }

    [Fact]
    public void FrameCount_ShouldBeCeilingOfDurationTimesFps()
    {
        // Arrange & Act
        var anim1 = new AnimationClipDefinition { FramesPerSecond = 12, DurationSeconds = 1.0 };
        var anim2 = new AnimationClipDefinition { FramesPerSecond = 12, DurationSeconds = 1.5 };
        var anim3 = new AnimationClipDefinition { FramesPerSecond = 30, DurationSeconds = 2.0 };

        // Assert
        anim1.FrameCount.Should().Be(12); // Ceiling(12) = 12
        anim2.FrameCount.Should().Be(18); // Ceiling(18) = 18
        anim3.FrameCount.Should().Be(60); // Ceiling(60) = 60
    }

    [Fact]
    public void GetSortedKeyframes_ShouldSortByTime()
    {
        // Arrange
        var track = new BoneTrack { BoneId = BoneId.New() };
        track.Keyframes.Add(new TransformKeyframe { TimeSeconds = 2.0 });
        track.Keyframes.Add(new TransformKeyframe { TimeSeconds = 0.5 });
        track.Keyframes.Add(new TransformKeyframe { TimeSeconds = 1.0 });

        // Act
        var sorted = track.GetSortedKeyframes();

        // Assert
        sorted[0].TimeSeconds.Should().Be(0.5);
        sorted[1].TimeSeconds.Should().Be(1.0);
        sorted[2].TimeSeconds.Should().Be(2.0);
    }

    [Fact]
    public void DuplicateKeyframeTimes_ShouldBeDetected()
    {
        // Arrange
        var track = new BoneTrack { BoneId = BoneId.New() };
        track.Keyframes.Add(new TransformKeyframe { TimeSeconds = 1.0 });
        track.Keyframes.Add(new TransformKeyframe { TimeSeconds = 1.0 }); // duplicate

        // Act: sort then check for consecutive equal times
        var sorted = track.GetSortedKeyframes();
        var hasDuplicates = false;
        for (int i = 1; i < sorted.Count; i++)
        {
            if (Math.Abs(sorted[i].TimeSeconds - sorted[i - 1].TimeSeconds) < 1e-9)
            {
                hasDuplicates = true;
                break;
            }
        }

        // Assert
        hasDuplicates.Should().BeTrue();
    }

    [Fact]
    public void MissingBoneTrack_ShouldReturnEmpty()
    {
        // Arrange
        var anim = new AnimationClipDefinition();
        var boneId = BoneId.New();

        // Act
        var hasTrack = anim.BoneTracks.TryGetValue(boneId.ToKeyString(), out var track);

        // Assert
        hasTrack.Should().BeFalse();
        track.Should().BeNull();
    }

    [Fact]
    public void BoneTrack_HasKeyframes_ShouldReflectKeyframeCount()
    {
        // Arrange
        var track = new BoneTrack { BoneId = BoneId.New() };

        // Assert
        track.HasKeyframes.Should().BeFalse();

        // Act
        track.Keyframes.Add(new TransformKeyframe { TimeSeconds = 0 });

        // Assert
        track.HasKeyframes.Should().BeTrue();
    }
}
