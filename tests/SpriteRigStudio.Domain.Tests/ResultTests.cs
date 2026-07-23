using FluentAssertions;
using SpriteRigStudio.Domain.Common;
using Xunit;

namespace SpriteRigStudio.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldHaveIsSuccessTrue()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_ShouldHaveIsSuccessFalse()
    {
        // Act
        var result = Result.Failure("ERR_CODE", "Error message");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Failure_ShouldStoreErrorCodeAndMessage()
    {
        // Act
        var result = Result.Failure("ERR001", "Something went wrong");

        // Assert
        result.ErrorCode.Should().Be("ERR001");
        result.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void ResultOfT_Success_ShouldHaveValue()
    {
        // Act
        var result = Result<int>.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultOfT_Failure_ShouldHaveNoValue()
    {
        // Act
        var result = Result<int>.Failure("ERR001", "Failed");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("ERR001");
        result.ErrorMessage.Should().Be("Failed");
        result.Value.Should().Be(0); // default(int)
    }

    [Fact]
    public void ResultOfT_Success_WithStringValue()
    {
        // Act
        var result = Result<string>.Success("hello");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ResultOfT_Failure_WithStringType_ShouldHaveNullValue()
    {
        // Act
        var result = Result<string>.Failure("ERR001", "Failed");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void StaticHelper_SuccessOfT_ShouldCreateSuccessResult()
    {
        // Act
        var result = Result.Success("value");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
    }

    [Fact]
    public void StaticHelper_FailureOfT_ShouldCreateFailureResult()
    {
        // Act
        var result = Result.Failure<string>("ERR001", "Failed");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ERR001");
        result.ErrorMessage.Should().Be("Failed");
    }
}
