using Xunit;
using FluentAssertions;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Infrastructure.FileSystem;
using SpriteRigStudio.Infrastructure.Hashing;
using SpriteRigStudio.Infrastructure.Serialization;

namespace SpriteRigStudio.Application.Tests;

public class ProjectServiceTests
{
    [Fact]
    public void ProjectSerializer_RoundTrip_ShouldPreserveData()
    {
        var serializer = new ProjectSerializer();
        var original = new ManifestTestData { Name = "Test", Value = 42 };
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<ManifestTestData>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Test");
        deserialized.Value.Should().Be(42);
    }

    private class ManifestTestData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Fact]
    public void Sha256Hash_ShouldProduceDeterministicOutput()
    {
        var hasher = new Sha256HashProvider();
        var hash1 = hasher.ComputeHash("hello");
        var hash2 = hasher.ComputeHash("hello");
        hash1.Should().Be(hash2);
        hash1.Should().NotBe(hasher.ComputeHash("world"));
    }

    [Fact]
    public void WindowsFileSystem_PathCombine_ShouldWork()
    {
        var fs = new WindowsFileSystem();
        var path = fs.CombinePath("a", "b", "c.txt");
        path.Should().Be(Path.Combine("a", "b", "c.txt"));
    }
}
