namespace Sherlock.Domain.Entities;
public class Search
{
    public int Id { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string InputParameters { get; set; }
    public int Duration { get; set; }
    public int TokenId { get; set; }
    public string ResultData { get; set; }
}
