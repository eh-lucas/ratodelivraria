namespace Sherlock.Domain.Entities;
public class Search
{
    public int Id { get; set; }
    public int SearchTypeId { get; set; }
    public int ResultId { get; set; }
    public int TokenId { get; set; } 
    public DateTime StartDateTime { get; set; } 
    public DateTime EndDateTime { get; set; }
    public string InputParameters { get; set; }
    public string Result { get; set; }
}
