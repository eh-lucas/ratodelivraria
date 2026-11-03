using FluentAssertions;
using Sherlock.Business.Core.Scrapers.Common;

namespace Sherlock.Tests.Business;

public class IsbnInputTests
{
    [Theory]
    [InlineData("9788535914849", "9788535914849")]
    [InlineData("978-85-359-1484-9", "9788535914849")]
    [InlineData(" 978 85 359 1484 9 ", "9788535914849")]
    [InlineData("ISBN: 9788535914849", "9788535914849")]
    [InlineData("isbn 85359-1484-x", "853591484X")]
    public void TryNormalize_AceitaFormatosUsuais(string raw, string expected)
    {
        IsbnInput.TryNormalize(raw, out var isbn).Should().BeTrue();
        isbn.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("dom casmurro")]
    [InlineData("12345")]
    [InlineData("97885359148491")]      // 14 dígitos
    [InlineData("9788535914849; drop")]
    [InlineData("ISBN 978-85-359-1484-9 extra")]
    public void TryNormalize_RecusaOQueNaoEhIsbn(string? raw)
    {
        IsbnInput.TryNormalize(raw, out var isbn).Should().BeFalse();
        isbn.Should().BeEmpty();
    }
}
