using FluentAssertions;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Domain;

public class ResultTypeTests
{
    [Fact]
    public void Success_HasCorrectProperties()
    {
        // Act
        var result = ResultType.Success;

        // Assert
        result.Name.Should().Be("Success");
        result.IsSuccess.Should().BeTrue();
        result.IsBillable.Should().BeTrue();
    }

    [Fact]
    public void PartialSuccess_HasCorrectProperties()
    {
        // Act
        var result = ResultType.PartialSuccess;

        // Assert
        result.Name.Should().Be("PartialSuccess");
        result.IsSuccess.Should().BeTrue();
        result.IsBillable.Should().BeTrue();
    }

    [Fact]
    public void NoResults_HasCorrectProperties()
    {
        // Act
        var result = ResultType.NoResults;

        // Assert
        result.Name.Should().Be("NoResults");
        result.IsSuccess.Should().BeFalse();
        result.IsBillable.Should().BeFalse();
    }

    [Fact]
    public void AllFailed_HasCorrectProperties()
    {
        // Act
        var result = ResultType.AllFailed;

        // Assert
        result.Name.Should().Be("AllFailed");
        result.IsSuccess.Should().BeFalse();
        result.IsBillable.Should().BeFalse();
    }

    [Fact]
    public void AllResultTypes_HaveDescriptions()
    {
        // Assert
        ResultType.Success.Description.Should().NotBeNullOrEmpty();
        ResultType.PartialSuccess.Description.Should().NotBeNullOrEmpty();
        ResultType.NoResults.Description.Should().NotBeNullOrEmpty();
        ResultType.AllFailed.Description.Should().NotBeNullOrEmpty();
    }
}
