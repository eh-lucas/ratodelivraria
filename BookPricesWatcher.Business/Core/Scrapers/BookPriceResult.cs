namespace Sherlock.Business.Core.Scrapers
{
    public class BookPriceResult
    {
        public string Website { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
    }
}
