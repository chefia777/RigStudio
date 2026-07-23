using FluentAssertions;
using SpriteRigStudio.Domain.Geometry;
using SpriteRigStudio.Domain.Transforms;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class TransformTests
{
    [Fact]
    public void IdentityTransform_ShouldHaveZeroPositionZeroRotationUnitScale()
    {
        // Act
        var identity = Transform2D.Identity;

        // Assert
        identity.Position.Should().Be(Vector2D.Zero);
        identity.RotationDegrees.Should().Be(0);
        identity.Scale.Should().Be(new Vector2D(1, 1));
        identity.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void IdentityTransform_ToMatrix_ShouldReturnIdentityMatrix()
    {
        // Act
        var matrix = Transform2D.Identity.ToMatrix();

        // Assert
        matrix.Should().Be(Matrix3x2D.Identity);
    }

    [Fact]
    public void SetPosition_ShouldBeReflectedInToMatrix()
    {
        // Arrange
        var position = new Vector2D(100, 200);
        var transform = new Transform2D(position, 0, new Vector2D(1, 1));

        // Act
        var matrix = transform.ToMatrix();

        // Assert
        matrix.M31.Should().Be(100);
        matrix.M32.Should().Be(200);
    }

    [Fact]
    public void SetRotation_ShouldBeReflectedInToMatrix()
    {
        // Arrange
        var transform = new Transform2D(Vector2D.Zero, 90, new Vector2D(1, 1));

        // Act
        var matrix = transform.ToMatrix();

        // Assert
        // Rotation 90 deg: cos=0, sin=1
        // Matrix = T(0,0) * R(90) * S(1,1) = R(90)
        // M11=cos=0, M12=sin=1
        Math.Abs(matrix.M11).Should().BeLessThan(1e-9);
        Math.Abs(matrix.M12 - 1).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void SetScale_ShouldBeReflectedInToMatrix()
    {
        // Arrange
        var transform = new Transform2D(Vector2D.Zero, 0, new Vector2D(2, 3));

        // Act
        var matrix = transform.ToMatrix();

        // Assert
        matrix.M11.Should().Be(2);
        matrix.M22.Should().Be(3);
    }

    [Fact]
    public void TRSMatrix_ShouldComposeAsTranslateTimesRotateTimesScale()
    {
        // Arrange
        var position = new Vector2D(10, 20);
        var rotation = 45.0;
        var scale = new Vector2D(2, 2);
        var transform = new Transform2D(position, rotation, scale);

        // Act
        var matrix = transform.ToMatrix();

        // Manually compute T * R * S
        var expectedT = Matrix3x2D.CreateTranslation(position);
        var expectedR = Matrix3x2D.CreateRotation(rotation);
        var expectedS = Matrix3x2D.CreateScale(scale.X, scale.Y);
        var expected = expectedT * expectedR * expectedS;

        // Assert
        Math.Abs(matrix.M11 - expected.M11).Should().BeLessThan(1e-9);
        Math.Abs(matrix.M12 - expected.M12).Should().BeLessThan(1e-9);
        Math.Abs(matrix.M21 - expected.M21).Should().BeLessThan(1e-9);
        Math.Abs(matrix.M22 - expected.M22).Should().BeLessThan(1e-9);
        Math.Abs(matrix.M31 - expected.M31).Should().BeLessThan(1e-9);
        Math.Abs(matrix.M32 - expected.M32).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void MatrixMultiplication_ShouldComposeTransforms()
    {
        // Arrange
        var t = Matrix3x2D.CreateTranslation(new Vector2D(10, 20));
        var s = Matrix3x2D.CreateScale(2, 2);

        // Act: translate then scale
        var composed = t * s;

        // Assert: point (0,0) transformed by T then S => (10,20) scaled => (20,40)
        var result = composed.Transform(Vector2D.Zero);
        result.X.Should().Be(20);
        result.Y.Should().Be(40);
    }

    [Fact]
    public void Vector2D_Rotate_ShouldRotateCorrectly()
    {
        // Arrange
        var v = new Vector2D(1, 0);

        // Act
        var rotated = v.Rotate(90);

        // Assert: (1,0) rotated 90° CCW => (0,1)
        Math.Abs(rotated.X).Should().BeLessThan(1e-9);
        Math.Abs(rotated.Y - 1).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void Transform2D_Compose_ShouldCombineParentAndChild()
    {
        // Arrange
        var parent = new Transform2D(new Vector2D(100, 0), 0, new Vector2D(1, 1));
        var child = new Transform2D(new Vector2D(50, 0), 0, new Vector2D(1, 1));

        // Act
        var composed = Transform2D.Compose(parent, child);

        // Assert: parent then child => position should be (150, 0)
        Math.Abs(composed.Position.X - 150).Should().BeLessThan(1e-6);
        Math.Abs(composed.Position.Y).Should().BeLessThan(1e-6);
    }

    [Fact]
    public void MatrixInversion_ShouldProduceInverse()
    {
        // Arrange
        var t = Matrix3x2D.CreateTranslation(new Vector2D(100, 200));
        var r = Matrix3x2D.CreateRotation(45);
        var m = t * r;

        // Act
        var inv = m.Inverted();
        var product = m * inv;

        // Assert: m * inv(m) should be identity (approximately)
        Math.Abs(product.M11 - 1).Should().BeLessThan(1e-9);
        Math.Abs(product.M12).Should().BeLessThan(1e-9);
        Math.Abs(product.M21).Should().BeLessThan(1e-9);
        Math.Abs(product.M22 - 1).Should().BeLessThan(1e-9);
        Math.Abs(product.M31).Should().BeLessThan(1e-9);
        Math.Abs(product.M32).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void MatrixInversion_OfIdentity_ShouldReturnIdentity()
    {
        // Act
        var inv = Matrix3x2D.Identity.Inverted();

        // Assert
        inv.Should().Be(Matrix3x2D.Identity);
    }

    [Fact]
    public void CoordinateFlip_NegateYScale_ShouldFlipY()
    {
        // Arrange
        var flipY = new Transform2D(Vector2D.Zero, 0, new Vector2D(1, -1));
        var matrix = flipY.ToMatrix();

        // Act
        var point = new Vector2D(10, 20);
        var transformed = matrix.Transform(point);

        // Assert: (10, 20) with Y scale -1 => (10, -20)
        Math.Abs(transformed.X - 10).Should().BeLessThan(1e-9);
        Math.Abs(transformed.Y - (-20)).Should().BeLessThan(1e-9);
    }

    [Fact]
    public void CoordinateFlip_NegateXScale_ShouldFlipX()
    {
        // Arrange
        var flipX = new Transform2D(Vector2D.Zero, 0, new Vector2D(-1, 1));
        var matrix = flipX.ToMatrix();

        // Act
        var point = new Vector2D(10, 20);
        var transformed = matrix.Transform(point);

        // Assert: (10, 20) with X scale -1 => (-10, 20)
        Math.Abs(transformed.X - (-10)).Should().BeLessThan(1e-9);
        Math.Abs(transformed.Y - 20).Should().BeLessThan(1e-9);
    }
}
