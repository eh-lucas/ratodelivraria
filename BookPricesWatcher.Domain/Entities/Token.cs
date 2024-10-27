namespace Sherlock.Domain.Entities;
public class Token
{
    public int Id { get; set; }
    public string TokenUid { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime DeletedAt { get; set; }
}
