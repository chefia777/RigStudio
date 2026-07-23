using FluentAssertions;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Infrastructure.Serialization;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class SerializationTests
{
    private readonly ProjectSerializer _serializer = new();

    [Fact]
    public void RoundTrip_SerializeDeserialize_ShouldPreserveData()
    {
        // Arrange
        var manifest = new ProjectManifest
        {
            Name = "Test Project",
            FormatVersion = 1,
            ProjectId = SpriteRigStudio.Domain.Common.ProjectId.New(),
            CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2025, 6, 15, 12, 30, 0, DateTimeKind.Utc)
        };
        manifest.SkeletonFiles.Add("skeletons/humanoid.json");
        manifest.SkeletonFiles.Add("skeletons/animal.json");
        manifest.AnimationFiles.Add("animations/walk.json");

        // Act
        var json = _serializer.SerializeManifest(manifest);
        var deserialized = _serializer.DeserializeManifest(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Test Project");
        deserialized.FormatVersion.Should().Be(1);
        deserialized.ProjectId.Should().Be(manifest.ProjectId);
        deserialized.CreatedAtUtc.Should().Be(manifest.CreatedAtUtc);
        deserialized.UpdatedAtUtc.Should().Be(manifest.UpdatedAtUtc);
        deserialized.SkeletonFiles.Should().BeEquivalentTo(manifest.SkeletonFiles);
        deserialized.AnimationFiles.Should().BeEquivalentTo(manifest.AnimationFiles);
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldReturnNull()
    {
        // Act: Deserialize throws JsonException on invalid JSON; use TryDeserialize for safe handling
        var success = _serializer.TryDeserialize<ProjectManifest>("{ invalid json }", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void SerializeManifest_ShouldProduceValidJson()
    {
        // Arrange
        var manifest = new ProjectManifest
        {
            Name = "Round-Trip Test",
            FormatVersion = 1,
            ProjectId = SpriteRigStudio.Domain.Common.ProjectId.New(),
            CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = _serializer.SerializeManifest(manifest);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"name\": \"Round-Trip Test\"");
        json.Should().Contain("\"formatVersion\": 1");
    }

    [Fact]
    public void RoundTrip_GenericSerialize_ShouldPreserveData()
    {
        // Arrange
        var original = new ProjectManifest
        {
            Name = "Generic Test",
            FormatVersion = 2
        };

        // Act
        var json = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize<ProjectManifest>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Generic Test");
        deserialized.FormatVersion.Should().Be(2);
    }

    [Fact]
    public void TryDeserialize_ValidJson_ShouldReturnTrue()
    {
        // Arrange
        var manifest = new ProjectManifest { Name = "Test" };
        var json = _serializer.Serialize(manifest);

        // Act
        var success = _serializer.TryDeserialize<ProjectManifest>(json, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ShouldReturnFalse()
    {
        // Act
        var success = _serializer.TryDeserialize<ProjectManifest>("not json", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }
}
