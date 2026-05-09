namespace Sherlock.Api.DTOs;

public class BookDTO
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn13 { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Discount { get; set; }
    public string WebSite { get; set; } = string.Empty;
}
