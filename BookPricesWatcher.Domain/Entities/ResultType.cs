namespace Sherlock.Domain.Entities;
public class ResultType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsBillable { get; set; }
}
