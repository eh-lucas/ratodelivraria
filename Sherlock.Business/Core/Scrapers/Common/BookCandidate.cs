namespace Sherlock.Business.Core.Scrapers.Common;

internal class BookCandidate
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Discount { get; set; }
    public string? ProductUrl { get; set; }
}
