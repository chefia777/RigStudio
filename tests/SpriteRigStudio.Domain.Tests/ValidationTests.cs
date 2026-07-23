using FluentAssertions;
using SpriteRigStudio.Domain.Validation;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class ValidationTests
{
    [Fact]
    public void ValidationResult_WithNoErrors_ShouldBeValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act & Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void AddError_ShouldMakeIsValidFalse()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("ERR001", "An error occurred");

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddWarning_ShouldNotAffectIsValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddWarning("WARN001", "A warning");

        // Assert
        result.IsValid.Should().BeTrue(); // Warnings don't affect IsValid
    }

    [Fact]
    public void AddError_ShouldStoreCodeAndMessage()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("ERR001", "Something went wrong");

        // Assert
        result.Errors.Should().ContainSingle().Which.Should().Match<ValidationIssue>(e =>
            e.Code == "ERR001" && e.Message == "Something went wrong");
    }

    [Fact]
    public void AddWarning_ShouldStoreCodeAndMessage()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddWarning("WARN001", "Something to note");

        // Assert
        result.Warnings.Should().ContainSingle().Which.Should().Match<ValidationIssue>(w =>
            w.Code == "WARN001" && w.Message == "Something to note");
    }

    [Fact]
    public void MultipleIssues_ShouldAllBeCollected()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("ERR001", "First error");
        result.AddError("ERR002", "Second error");
        result.AddWarning("WARN001", "First warning");
        result.AddWarning("WARN002", "Second warning");

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Warnings.Should().HaveCount(2);
        result.All.Should().HaveCount(4);
    }

    [Fact]
    public void Issue_ShouldStoreSeverityCorrectly()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("ERR001", "Error message");
        result.AddWarning("WARN001", "Warning message");

        // Assert
        result.Errors[0].Severity.Should().Be(ValidationSeverity.Error);
        result.Warnings[0].Severity.Should().Be(ValidationSeverity.Warning);
    }

    [Fact]
    public void Success_ShouldReturnEmptyValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void AddError_ShouldSupportAllOptionalParameters()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("ERR001", "Error", entityId: "entity1", propertyPath: "prop1", suggestedAction: "Fix it");

        // Assert
        var issue = result.Errors[0];
        issue.EntityId.Should().Be("entity1");
        issue.PropertyPath.Should().Be("prop1");
        issue.SuggestedAction.Should().Be("Fix it");
    }

    [Fact]
    public void FulentApi_AddError_ShouldReturnSameInstance()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        var returned = result.AddError("ERR001", "msg");

        // Assert
        returned.Should().BeSameAs(result);
    }
}
