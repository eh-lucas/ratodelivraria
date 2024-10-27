using Microsoft.AspNetCore.Mvc;
using SherlockAPI.Configurations;
using SherlockAPI.Services;
using Sherlock.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;

namespace SherlockAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;
    private readonly SherlockDbContext _context;

    public AuthController(TokenService tokenService, SherlockDbContext context)
    {
        _tokenService = tokenService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Name == loginRequest.Username);

        if (user == null || !VerifyPasswordHash(loginRequest.Password, user.Password))
        {
            return Unauthorized("Usuário ou senha inválidos");
        }


        // Exemplo de verificação de credenciais
        //if (user.Name == "user" && user.Password == "password")
        //{
        //    var token = _tokenService.GenerateToken("user-id");
        //    return Ok(new { Token = token });
        //}

        return Unauthorized();
    }

    // Método para verificar a senha com o hash armazenado
    private bool VerifyPasswordHash(string password, string storedHash)
    {
        using (var sha256 = SHA256.Create())
        {
            var computedHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return computedHash == storedHash;
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
