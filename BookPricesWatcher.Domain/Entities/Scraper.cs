namespace Sherlock.Domain.Entities;
public class Scraper
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ScraperCategoryId { get; set; }
    public bool Active { get; set; }
}
