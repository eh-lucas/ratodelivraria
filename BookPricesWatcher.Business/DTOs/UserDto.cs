namespace Sherlock.Business.DTOs;

public class UserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; }
    public long Cpf { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}

