using FluentAssertions;
using Sherlock.Business.DTOs;

namespace Sherlock.Tests.Business;

public class BestProviderCartTests
{
    [Fact]
    public void BestProviderCartRequest_HasDefaultValues()
    {
        // Act
        var request = new BestProviderCartRequest();

        // Assert
        request.Books.Should().NotBeNull();
        request.Books.Should().BeEmpty();
        request.ProviderUrls.Should().BeNull();
        request.IncludeShipping.Should().BeTrue();
    }

    [Fact]
    public void BestProviderCartRequest_CanSetBooks()
    {
        // Arrange
        var books = new List<CartBookItem>
        {
            new() { Title = "Clean Code", Quantity = 1 },
            new() { Title = "Clean Architecture", Quantity = 2 }
        };

        var request = new BestProviderCartRequest
        {
            Books = books,
            IncludeShipping = false
        };

        // Assert
        request.Books.Should().HaveCount(2);
        request.Books[0].Title.Should().Be("Clean Code");
        request.Books[1].Quantity.Should().Be(2);
        request.IncludeShipping.Should().BeFalse();
    }

    [Fact]
    public void BestProviderCartResult_HasDefaultValues()
    {
        // Act
        var result = new BestProviderCartResult();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.BestProvider.Should().BeNull();
        result.SecondBestProvider.Should().BeNull();
        result.BooksNotFound.Should().NotBeNull();
        result.BooksNotFound.Should().BeEmpty();
        result.ExecutionTimeMs.Should().Be(0);
        result.CreditsUsed.Should().Be(0);
        result.FromCache.Should().BeFalse();
        result.TotalProvidersSearched.Should().Be(0);
    }

    [Fact]
    public void BestProviderCartResult_CanSetBestProvider()
    {
        // Arrange
        var bestProvider = new ProviderCart
        {
            ProviderId = 1,
            ProviderName = "Amazon",
            ProviderUrl = "http://amazon.com.br",
            Subtotal = 150.00m,
            ShippingCost = 0m,
            Total = 150.00m,
            HasFreeShipping = true
        };

        var result = new BestProviderCartResult
        {
            Success = true,
            Message = "Melhor opção encontrada",
            BestProvider = bestProvider
        };

        // Assert
        result.Success.Should().BeTrue();
        result.BestProvider.Should().NotBeNull();
        result.BestProvider!.ProviderName.Should().Be("Amazon");
        result.BestProvider.Total.Should().Be(150.00m);
        result.BestProvider.HasFreeShipping.Should().BeTrue();
    }

    [Fact]
    public void BestProviderCartResult_CanSetSecondBestProvider()
    {
        // Arrange
        var bestProvider = new ProviderCart
        {
            ProviderId = 1,
            ProviderName = "Amazon",
            Total = 150.00m
        };

        var secondBest = new ProviderCart
        {
            ProviderId = 2,
            ProviderName = "Estante Virtual",
            Total = 165.00m
        };

        var result = new BestProviderCartResult
        {
            Success = true,
            BestProvider = bestProvider,
            SecondBestProvider = secondBest
        };

        // Assert
        result.BestProvider!.Total.Should().Be(150.00m);
        result.SecondBestProvider.Should().NotBeNull();
        result.SecondBestProvider!.Total.Should().Be(165.00m);
        result.SecondBestProvider.ProviderName.Should().Be("Estante Virtual");
    }

    [Fact]
    public void BestProviderCartResult_CanTrackBooksNotFound()
    {
        // Arrange
        var result = new BestProviderCartResult
        {
            Success = false,
            Message = "Alguns livros não foram encontrados",
            BooksNotFound = new List<string> { "Livro Raro 1", "Livro Raro 2" }
        };

        // Assert
        result.BooksNotFound.Should().HaveCount(2);
        result.BooksNotFound.Should().Contain("Livro Raro 1");
    }

    [Fact]
    public void BookPriceOption_HasNewFields()
    {
        // Act
        var option = new BookPriceOption
        {
            BookTitle = "Clean Code",
            Author = "Robert C. Martin",
            ProviderId = 1,
            ProviderName = "Amazon",
            ProviderUrl = "http://amazon.com.br",
            Price = 89.90m,
            Discount = 10.00m,
            ProductUrl = "http://amazon.com.br/clean-code",
            Available = true
        };

        // Assert
        option.Author.Should().Be("Robert C. Martin");
        option.ProviderUrl.Should().Be("http://amazon.com.br");
        option.Discount.Should().Be(10.00m);
    }

    [Fact]
    public void ProviderCart_CanHaveMultipleItems()
    {
        // Arrange
        var items = new List<ProviderCartItem>
        {
            new() { Title = "Clean Code", UnitPrice = 89.90m, Quantity = 1, TotalPrice = 89.90m },
            new() { Title = "Clean Architecture", UnitPrice = 99.90m, Quantity = 2, TotalPrice = 199.80m }
        };

        var cart = new ProviderCart
        {
            ProviderId = 1,
            ProviderName = "Amazon",
            Items = items,
            Subtotal = 289.70m,
            ShippingCost = 15.00m,
            Total = 304.70m
        };

        // Assert
        cart.Items.Should().HaveCount(2);
        cart.Subtotal.Should().Be(289.70m);
        cart.Total.Should().Be(304.70m);
    }
}
