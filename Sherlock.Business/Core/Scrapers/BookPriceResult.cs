namespace Sherlock.Business.Core.Scrapers
{
    public class BookPriceResult
    {
        public string Website { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }
}
