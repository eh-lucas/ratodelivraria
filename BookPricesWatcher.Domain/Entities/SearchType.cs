namespace Sherlock.Domain.Entities;
public class SearchType
{
    public int SearchTypeId { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    public int Cost { get; set; }
}
