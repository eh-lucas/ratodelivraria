using System.ComponentModel.DataAnnotations;

namespace SherlockAPI.DTOs;

public class RegisterRequestDTO
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [MaxLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Senha deve ter no mínimo 8 caracteres")]
    [MaxLength(128, ErrorMessage = "Senha deve ter no máximo 128 caracteres")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string? Username { get; set; }
}

public class LoginRequestDTO
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public DateTime ExpiresAt { get; set; }
}
