using FluentAssertions;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Domain.Common;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class SkeletonHierarchyTests
{
    [Fact]
    public void CreateSkeleton_WithRootBone_ShouldHaveRootBoneSet()
    {
        // Arrange
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        skeleton.Bones[root.BoneId] = root;
        skeleton.RootBoneId = root.BoneId;

        // Act
        var retrievedRoot = skeleton.GetRootBone();

        // Assert
        retrievedRoot.Should().NotBeNull();
        retrievedRoot!.BoneId.Should().Be(root.BoneId);
    }

    [Fact]
    public void AddChildBone_ShouldAppearInGetChildren()
    {
        // Arrange
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        var child = new BoneDefinition { BoneId = BoneId.New(), Name = "Child", ParentBoneId = root.BoneId };
        skeleton.Bones[root.BoneId] = root;
        skeleton.Bones[child.BoneId] = child;
        skeleton.RootBoneId = root.BoneId;

        // Act
        var children = skeleton.GetChildren(root.BoneId);

        // Assert
        children.Should().ContainSingle().Which.BoneId.Should().Be(child.BoneId);
    }

    [Fact]
    public void HasCycles_WithValidHierarchy_ShouldReturnFalse()
    {
        // Arrange
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        var child = new BoneDefinition { BoneId = BoneId.New(), Name = "Child", ParentBoneId = root.BoneId };
        var grandchild = new BoneDefinition { BoneId = BoneId.New(), Name = "Grandchild", ParentBoneId = child.BoneId };
        skeleton.Bones[root.BoneId] = root;
        skeleton.Bones[child.BoneId] = child;
        skeleton.Bones[grandchild.BoneId] = grandchild;
        skeleton.RootBoneId = root.BoneId;

        // Act
        var hasCycles = skeleton.HasCycles();

        // Assert
        hasCycles.Should().BeFalse();
    }

    [Fact]
    public void HasCycles_WithCycle_ShouldReturnTrue()
    {
        // Arrange: root -> child -> grandchild -> root (cycle)
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        var child = new BoneDefinition { BoneId = BoneId.New(), Name = "Child", ParentBoneId = root.BoneId };
        var grandchild = new BoneDefinition { BoneId = BoneId.New(), Name = "Grandchild", ParentBoneId = child.BoneId };
        // Create cycle: root's parent is grandchild
        root.ParentBoneId = grandchild.BoneId;
        skeleton.Bones[root.BoneId] = root;
        skeleton.Bones[child.BoneId] = child;
        skeleton.Bones[grandchild.BoneId] = grandchild;
        skeleton.RootBoneId = root.BoneId;

        // Act
        var hasCycles = skeleton.HasCycles();

        // Assert
        hasCycles.Should().BeTrue();
    }

    [Fact]
    public void HasCycles_WithSelfCycle_ShouldReturnTrue()
    {
        // Arrange: a bone that is its own parent
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root", ParentBoneId = BoneId.New() };
        // root.ParentBoneId is some non-existent bone; make parent point to root itself
        root.ParentBoneId = root.BoneId;
        skeleton.Bones[root.BoneId] = root;
        skeleton.RootBoneId = root.BoneId;

        // Act
        var hasCycles = skeleton.HasCycles();

        // Assert
        hasCycles.Should().BeTrue();
    }

    [Fact]
    public void ReparentBone_ShouldUpdateParent()
    {
        // Arrange
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        var oldParent = new BoneDefinition { BoneId = BoneId.New(), Name = "Old Parent" };
        var newParent = new BoneDefinition { BoneId = BoneId.New(), Name = "New Parent" };
        var child = new BoneDefinition { BoneId = BoneId.New(), Name = "Child", ParentBoneId = oldParent.BoneId };
        skeleton.Bones[root.BoneId] = root;
        skeleton.Bones[oldParent.BoneId] = oldParent;
        skeleton.Bones[newParent.BoneId] = newParent;
        skeleton.Bones[child.BoneId] = child;
        skeleton.RootBoneId = root.BoneId;

        // Act
        child.ParentBoneId = newParent.BoneId;

        // Assert
        skeleton.GetChildren(oldParent.BoneId).Should().BeEmpty();
        skeleton.GetChildren(newParent.BoneId).Should().ContainSingle().Which.BoneId.Should().Be(child.BoneId);
    }

    [Fact]
    public void GetChildren_WithNoChildren_ShouldReturnEmpty()
    {
        // Arrange
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        skeleton.Bones[root.BoneId] = root;
        skeleton.RootBoneId = root.BoneId;

        // Act
        var children = skeleton.GetChildren(root.BoneId);

        // Assert
        children.Should().BeEmpty();
    }

    [Fact]
    public void DefaultHumanoidSkeleton_Create_ShouldHave24Bones()
    {
        // Act
        var skeleton = DefaultHumanoidSkeleton.Create();

        // Assert
        skeleton.Bones.Should().HaveCount(24);
    }

    [Fact]
    public void DefaultHumanoidSkeleton_Create_ShouldHaveCorrectHierarchy()
    {
        // Act
        var skeleton = DefaultHumanoidSkeleton.Create();

        // Assert
        skeleton.HasCycles().Should().BeFalse();
        skeleton.GetRootBone().Should().NotBeNull();
        skeleton.GetRootBone()!.Name.Should().Be("Root");

        // Root should have children (Pelvis should have ParentBoneId = root)
        var root = skeleton.GetRootBone()!;
        var rootChildren = skeleton.GetChildren(root.BoneId).ToList();
        rootChildren.Should().NotBeEmpty();
    }

    [Fact]
    public void DefaultHumanoidSkeleton_Create_ShouldHaveNoCycles()
    {
        // Act
        var skeleton = DefaultHumanoidSkeleton.Create();

        // Assert
        skeleton.HasCycles().Should().BeFalse();
    }

    [Fact]
    public void RemoveBone_ShouldReparentChildrenToGrandparent()
    {
        // Arrange
        var skeleton = new SkeletonDefinition();
        var root = new BoneDefinition { BoneId = BoneId.New(), Name = "Root" };
        var middle = new BoneDefinition { BoneId = BoneId.New(), Name = "Middle", ParentBoneId = root.BoneId };
        var child = new BoneDefinition { BoneId = BoneId.New(), Name = "Child", ParentBoneId = middle.BoneId };
        skeleton.Bones[root.BoneId] = root;
        skeleton.Bones[middle.BoneId] = middle;
        skeleton.Bones[child.BoneId] = child;
        skeleton.RootBoneId = root.BoneId;

        // Act: Remove "middle" and reparent its children to root
        skeleton.Bones.Remove(middle.BoneId);
        child.ParentBoneId = root.BoneId;

        // Assert
        skeleton.Bones.Should().NotContainKey(middle.BoneId);
        skeleton.GetChildren(root.BoneId).Should().Contain(child);
        child.ParentBoneId.Should().Be(root.BoneId);
    }
}
