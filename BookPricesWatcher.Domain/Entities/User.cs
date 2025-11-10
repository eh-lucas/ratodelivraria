namespace Sherlock.Domain.Entities;
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public required string Email { get; set; }
    public long Cpf { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool Active { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime DeletedAt { get; set; }
}
