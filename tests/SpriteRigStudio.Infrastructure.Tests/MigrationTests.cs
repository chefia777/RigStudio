using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SpriteRigStudio.Domain.Common;
using SpriteRigStudio.Infrastructure.Migrations;
using SpriteRigStudio.Infrastructure.Migrations.Migrations;
using SpriteRigStudio.Infrastructure.Serialization;

namespace SpriteRigStudio.Domain.Tests;

public class MigrationTests
{
    [Fact]
    public void V1ToV2Migration_ShouldUpdateFormatVersion()
    {
        var migration = new V1ToV2Migration();
        migration.SourceVersion.Should().Be(1);
        migration.TargetVersion.Should().Be(2);
    }

    [Fact]
    public void MigrationRunner_WithNoMigrations_ShouldReturnLatestVersion1()
    {
        var runner = new MigrationRunner(Enumerable.Empty<IProjectMigration>(), NullLogger<MigrationRunner>.Instance!);
        runner.LatestVersion.Should().Be(1);
    }

    [Fact]
    public void MigrationRunner_AlreadyAtLatest_ShouldReturnSuccess()
    {
        var migrations = new[] { new V1ToV2Migration() };
        var runner = new MigrationRunner(migrations, NullLogger<MigrationRunner>.Instance!);
        runner.LatestVersion.Should().Be(2);

        var result = runner.Migrate("{}", 2);
        result.IsSuccess.Should().BeTrue();
        result.MigratedJson.Should().Be("{}");
    }

    [Fact]
    public void MigrationRunner_UnsupportedVersion_ShouldFail()
    {
        var migrations = new[] { new V1ToV2Migration() };
        var runner = new MigrationRunner(migrations, NullLogger<MigrationRunner>.Instance!);

        var result = runner.Migrate("{}", 99);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("UNSUPPORTED_VERSION");
    }
}
