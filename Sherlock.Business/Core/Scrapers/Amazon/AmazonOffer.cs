namespace Sherlock.Business.Core.Scrapers.Amazon;

/// <summary>
/// Uma oferta lida do primeiro card da busca por ISBN na Amazon.
/// </summary>
public class AmazonOffer
{
    public string Asin { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Discount { get; set; }
    public string? Format { get; set; }
    public string? ProductUrl { get; set; }
    public string? ImageUrl { get; set; }
}
