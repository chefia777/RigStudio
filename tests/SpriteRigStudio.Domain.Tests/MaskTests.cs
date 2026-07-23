using FluentAssertions;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Masks;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class MaskTests
{
    [Fact]
    public void ValidPolygon_With3Vertices_ShouldBeValid()
    {
        // Arrange
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(5, 10));

        // Act
        var isValid = mask.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidPolygon_With4Vertices_ShouldBeValid()
    {
        // Arrange
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));
        mask.OuterContour.Add(new Vector2D(0, 10));

        // Act
        var isValid = mask.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void SelfIntersectingPolygon_ShouldBeDetected()
    {
        // Arrange: bow-tie shape (self-intersecting)
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(0, 10));

        // Act
        var hasSelfIntersections = mask.HasSelfIntersections();
        var isValid = mask.IsValid();

        // Assert
        hasSelfIntersections.Should().BeTrue();
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SelfIntersectingPolygon_CrossingEdges_ShouldBeDetected()
    {
        // Arrange: another crossing shape where edges cross
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));
        mask.OuterContour.Add(new Vector2D(0, 10));
        mask.OuterContour.Add(new Vector2D(10, 0));

        // Act
        var hasSelfIntersections = mask.HasSelfIntersections();

        // Assert
        hasSelfIntersections.Should().BeTrue();
    }

    [Fact]
    public void InsertVertex_ShouldAddAtCorrectPosition()
    {
        // Arrange
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));

        // Act: insert a vertex at index 1
        mask.OuterContour.Insert(1, new Vector2D(5, 0));

        // Assert
        mask.OuterContour[1].Should().Be(new Vector2D(5, 0));
        mask.OuterContour.Should().HaveCount(4);
        mask.VertexCount.Should().Be(4);
    }

    [Fact]
    public void RemoveVertex_ShouldDecreaseCount()
    {
        // Arrange
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));

        // Act
        mask.OuterContour.RemoveAt(1);

        // Assert
        mask.OuterContour.Should().HaveCount(2);
        mask.VertexCount.Should().Be(2);
    }

    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var mask = new PolygonMaskDefinition();
        mask.Name = "Original";
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));
        mask.ExpansionPixels = 5;
        mask.FeatherPixels = 2;

        // Act
        var clone = mask.Clone();

        // Assert
        clone.Should().NotBeSameAs(mask);
        clone.Name.Should().Be("Original (copy)");
        clone.OuterContour.Should().HaveCount(3);
        clone.OuterContour[0].Should().Be(mask.OuterContour[0]);
        clone.ExpansionPixels.Should().Be(5);
        clone.FeatherPixels.Should().Be(2);
        clone.Enabled.Should().Be(mask.Enabled);
        clone.Visible.Should().Be(mask.Visible);
        clone.Locked.Should().BeFalse(); // Clone is unlocked

        // Verify independence: modifying clone doesn't affect original
        clone.OuterContour[0] = new Vector2D(99, 99);
        mask.OuterContour[0].Should().Be(new Vector2D(0, 0));
    }

    [Fact]
    public void IsValid_WithLessThan3Vertices_ShouldReturnFalse()
    {
        // Arrange
        var mask0 = new PolygonMaskDefinition(); // 0 vertices
        var mask1 = new PolygonMaskDefinition();
        mask1.OuterContour.Add(new Vector2D(0, 0)); // 1 vertex
        var mask2 = new PolygonMaskDefinition();
        mask2.OuterContour.Add(new Vector2D(0, 0));
        mask2.OuterContour.Add(new Vector2D(10, 0)); // 2 vertices

        // Act & Assert
        mask0.IsValid().Should().BeFalse();
        mask1.IsValid().Should().BeFalse();
        mask2.IsValid().Should().BeFalse();
    }

    [Fact]
    public void NonIntersectingPolygon_HasSelfIntersections_ShouldReturnFalse()
    {
        // Arrange: a simple convex quadrilateral
        var mask = new PolygonMaskDefinition();
        mask.OuterContour.Add(new Vector2D(0, 0));
        mask.OuterContour.Add(new Vector2D(10, 0));
        mask.OuterContour.Add(new Vector2D(10, 10));
        mask.OuterContour.Add(new Vector2D(0, 10));

        // Act
        var hasSelfIntersections = mask.HasSelfIntersections();

        // Assert
        hasSelfIntersections.Should().BeFalse();
    }
}
