using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sherlock.Api.Services;
using Sherlock.Api.DTOs;
using Sherlock.Api.Constants;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;

namespace Sherlock.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SherlockDbContext _context;
    private readonly TokenService _tokenService;
    private readonly GoogleLoginService _googleLogin;
    private readonly UsernameService _usernames;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        SherlockDbContext context,
        TokenService tokenService,
        GoogleLoginService googleLogin,
        UsernameService usernames,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _googleLogin = googleLogin;
        _usernames = usernames;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Registra um novo usuário
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (existingUser != null)
        {
            // Sem o e-mail: o Serilog grava em arquivo por 30 dias, e isso punha a
            // base de e-mails em texto puro fora de qualquer controle do banco (§2.3).
            _logger.LogWarning("Tentativa de registro com email já cadastrado");
            return Conflict(new { message = "Email já cadastrado." });
        }

        var user = new User
        {
            Email = normalizedEmail,
            Username = request.Username ?? await _usernames.GerarAPartirDoEmailAsync(normalizedEmail),
            FirstName = request.FirstName ?? request.Username,
            Role = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: AppConstants.Auth.BcryptWorkFactor),
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Novo usuário registrado: {UserId}", user.Id);

            var token = _tokenService.GenerateToken(user.Id.ToString(), user.Role);
            var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? AppConstants.Auth.DefaultTokenExpiryMinutes.ToString());

            return CreatedAtAction(nameof(Register), new LoginResponseDTO
            {
                Token = token,
                Email = user.Email,
                Username = user.FirstName ?? user.Username,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao salvar usuário no banco de dados");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Erro interno ao criar usuário." });
        }
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var normalizedEmail = request.Email.ToLowerInvariant().Trim();

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.Active);

        if (user == null)
        {
            _logger.LogWarning("Tentativa de login com email não cadastrado");
            return Unauthorized(new { message = "Email ou senha inválidos." });
        }

        // Conta nascida do Google nunca teve senha. Responder "senha incorreta" mandaria
        // a pessoa tentar lembrar uma que não existe — ver §1.4 do PLANO-OBSERVADOS.
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogInformation("Login por senha numa conta só-Google: {UserId}", user.Id);
            return Unauthorized(new
            {
                message = "Esta conta entra pelo Google. Use o botão \"Entrar com Google\".",
                useGoogle = true
            });
        }

        var isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isValidPassword)
        {
            _logger.LogWarning("Tentativa de login com senha incorreta para usuário: {UserId}", user.Id);
            return Unauthorized(new { message = "Email ou senha inválidos." });
        }

        var token = _tokenService.GenerateToken(user.Id.ToString(), user.Role);
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "30");

        _logger.LogInformation("Login bem-sucedido para usuário: {UserId}", user.Id);

        return Ok(new LoginResponseDTO
        {
            Token = token,
            Email = user.Email,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        });
    }

    /// <summary>
    /// Entra com a conta Google. Recebe o ID token que o navegador obteve do Google.
    /// </summary>
    [HttpPost("google")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var outcome = await _googleLogin.SignInAsync(request.Credential, cancellationToken);

        if (!outcome.Success || outcome.User == null)
            return Unauthorized(new { message = outcome.Error });

        var user = outcome.User;
        var token = _tokenService.GenerateToken(user.Id.ToString(), user.Role);
        var expiryMinutes = int.Parse(
            _configuration["JwtSettings:ExpiryInMinutes"] ?? AppConstants.Auth.DefaultTokenExpiryMinutes.ToString());

        return Ok(new LoginResponseDTO
        {
            Token = token,
            Email = user.Email,
            Username = user.FirstName ?? user.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        });
    }

    /// <summary>
    /// Diz ao front se o botão do Google deve aparecer, e com qual client id.
    /// O client id é público por desenho — ele vai no HTML da página.
    /// </summary>
    [HttpGet("google/config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GoogleConfig()
    {
        var clientId = _configuration["GoogleAuth:ClientId"] ?? string.Empty;
        return Ok(new { enabled = !string.IsNullOrWhiteSpace(clientId), clientId });
    }

    /// <summary>
    /// Verifica se o token atual é válido
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        // Se chegou aqui com [Authorize], o token é válido
        // Mas como não temos [Authorize], apenas retornamos OK
        return Ok(new { valid = true, timestamp = DateTime.UtcNow });
    }
}
