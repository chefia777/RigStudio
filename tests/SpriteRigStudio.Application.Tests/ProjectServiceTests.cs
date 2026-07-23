using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SpriteRigStudio.Application.Projects;
using SpriteRigStudio.Domain.Projects;
using SpriteRigStudio.Domain.Skeletons;
using SpriteRigStudio.Infrastructure.FileSystem;
using SpriteRigStudio.Infrastructure.Hashing;
using SpriteRigStudio.Infrastructure.Persistence;
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

    [Fact]
    public async Task SaveAs_ShouldCreateProjectLayoutAndReloadProject()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "SpriteRigStudio_SaveAs_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileSystem = new WindowsFileSystem();
            var serializer = new ProjectSerializer();
            var repository = new ProjectRepository(
                fileSystem,
                new Sha256HashProvider(),
                serializer,
                new AtomicProjectFileWriter(fileSystem, NullLogger<AtomicProjectFileWriter>.Instance),
                NullLogger<ProjectRepository>.Instance);
            var project = new SpriteRigProject { Name = "Save As Test" };
            var skeleton = DefaultHumanoidSkeleton.Create();
            project.Skeletons[skeleton.SkeletonId.ToKeyString()] = skeleton;

            var saveResult = await repository.SaveAsAsync(project, projectDirectory);

            saveResult.IsSuccess.Should().BeTrue(saveResult.ErrorMessage);
            File.Exists(Path.Combine(projectDirectory, "project.srsproject")).Should().BeTrue();
            Directory.Exists(Path.Combine(projectDirectory, "Skeletons")).Should().BeTrue();
            Directory.Exists(Path.Combine(projectDirectory, "Sources")).Should().BeTrue();

            var openResult = await repository.OpenAsync(projectDirectory);
            openResult.IsSuccess.Should().BeTrue(openResult.ErrorMessage);
            openResult.Project.Should().NotBeNull();
            openResult.Project!.Skeletons.Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
                Directory.Delete(projectDirectory, recursive: true);
        }
    }
}
