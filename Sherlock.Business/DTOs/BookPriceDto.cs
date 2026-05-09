namespace Sherlock.Business.DTOs;

public class BookPriceDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public DateTime QueryDateTime { get; set; }
}
