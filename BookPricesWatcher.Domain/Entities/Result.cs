namespace Sherlock.Domain.Entities;
public class Result
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsBillable { get; set; }
}
