using FluentAssertions;
using Sherlock.Domain.Entities;

namespace Sherlock.Tests.Domain;

public class TransactionResultTests
{
    [Fact]
    public void Success_HasCorrectProperties()
    {
        // Act
        var result = TransactionResult.Success;

        // Assert
        result.Name.Should().Be("Success");
        result.IsSuccess.Should().BeTrue();
        result.IsBillable.Should().BeTrue();
    }

    [Fact]
    public void PartialSuccess_HasCorrectProperties()
    {
        // Act
        var result = TransactionResult.PartialSuccess;

        // Assert
        result.Name.Should().Be("PartialSuccess");
        result.IsSuccess.Should().BeTrue();
        result.IsBillable.Should().BeTrue();
    }

    [Fact]
    public void NoResults_HasCorrectProperties()
    {
        // Act
        var result = TransactionResult.NoResults;

        // Assert
        result.Name.Should().Be("NoResults");
        result.IsSuccess.Should().BeFalse();
        result.IsBillable.Should().BeFalse();
    }

    [Fact]
    public void AllFailed_HasCorrectProperties()
    {
        // Act
        var result = TransactionResult.AllFailed;

        // Assert
        result.Name.Should().Be("AllFailed");
        result.IsSuccess.Should().BeFalse();
        result.IsBillable.Should().BeFalse();
    }

    [Fact]
    public void AllTransactionResults_HaveDescriptions()
    {
        // Assert
        TransactionResult.Success.Description.Should().NotBeNullOrEmpty();
        TransactionResult.PartialSuccess.Description.Should().NotBeNullOrEmpty();
        TransactionResult.NoResults.Description.Should().NotBeNullOrEmpty();
        TransactionResult.AllFailed.Description.Should().NotBeNullOrEmpty();
    }
}
