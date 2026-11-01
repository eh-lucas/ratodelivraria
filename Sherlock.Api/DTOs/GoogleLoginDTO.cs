using System.ComponentModel.DataAnnotations;

namespace Sherlock.Api.DTOs;

public class GoogleLoginRequestDTO
{
    /// <summary>
    /// O ID token devolvido pelo Google Identity Services no navegador — um JWT
    /// assinado por eles. Chega cru; quem decide se vale é o backend.
    /// </summary>
    [Required(ErrorMessage = "Credential é obrigatório")]
    public string Credential { get; set; } = string.Empty;
}
