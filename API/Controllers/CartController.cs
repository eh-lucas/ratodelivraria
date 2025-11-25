using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using System.Security.Claims;

namespace SherlockAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly ICartOptimizationService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(
        ICartOptimizationService cartService,
        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Otimiza o carrinho de compras para encontrar a melhor combinação de preços
    /// </summary>
    /// <param name="request">Lista de livros e configurações de otimização</param>
    /// <returns>Resultado da otimização com carrinhos por provider</returns>
    [HttpPost("optimize")]
    [EnableRateLimiting("authenticated")]
    [ProducesResponseType(typeof(CartOptimizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> OptimizeCart(
        [FromBody] CartOptimizationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Books == null || !request.Books.Any())
        {
            return BadRequest(new { error = "A lista de livros não pode estar vazia." });
        }

        if (request.Books.Count > 20)
        {
            return BadRequest(new { error = "Máximo de 20 livros por otimização." });
        }

        // Valida cada livro
        foreach (var book in request.Books)
        {
            if (string.IsNullOrWhiteSpace(book.Title))
            {
                return BadRequest(new { error = "Todos os livros devem ter um título." });
            }

            if (book.Quantity <= 0)
            {
                book.Quantity = 1;
            }

            if (book.Quantity > 10)
            {
                return BadRequest(new { error = $"Quantidade máxima de 10 unidades por livro. Livro: {book.Title}" });
            }
        }

        var userId = GetUserId();

        _logger.LogInformation(
            "Requisição de otimização de carrinho: {BookCount} livros, estratégia: {Strategy}, usuário: {UserId}",
            request.Books.Count, request.Strategy, userId);

        try
        {
            var result = await _cartService.OptimizeCartAsync(request, userId, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new
            {
                error = "A requisição foi cancelada ou excedeu o tempo limite."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao otimizar carrinho para usuário {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Ocorreu um erro ao processar a otimização. Tente novamente."
            });
        }
    }

    /// <summary>
    /// Busca preço de um único livro em todos os providers ou providers específicos
    /// </summary>
    [HttpGet("search")]
    [EnableRateLimiting("authenticated")]
    [ProducesResponseType(typeof(CartOptimizationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchBook(
        [FromQuery] string title,
        [FromQuery] string? isbn = null,
        [FromQuery] string? author = null,
        [FromQuery] string? providerUrls = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { error = "O título do livro é obrigatório." });
        }

        // Parse provider URLs se fornecido
        List<string>? parsedProviderUrls = null;
        if (!string.IsNullOrEmpty(providerUrls))
        {
            parsedProviderUrls = providerUrls
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .ToList();
        }

        var request = new CartOptimizationRequest
        {
            Books = new List<CartBookItem>
            {
                new CartBookItem
                {
                    Title = title,
                    Isbn = isbn,
                    Author = author,
                    Quantity = 1
                }
            },
            Strategy = OptimizationStrategy.LowestTotal,
            IncludeShipping = false,
            ProviderUrls = parsedProviderUrls
        };

        var userId = GetUserId();

        try
        {
            var result = await _cartService.OptimizeCartAsync(request, userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar livro '{Title}'", title);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Ocorreu um erro ao buscar o livro. Tente novamente."
            });
        }
    }

    /// <summary>
    /// Retorna as estratégias de otimização disponíveis
    /// </summary>
    [HttpGet("strategies")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public IActionResult GetStrategies()
    {
        var strategies = Enum.GetValues<OptimizationStrategy>()
            .Select(s => new
            {
                value = (int)s,
                name = s.ToString(),
                description = GetStrategyDescription(s)
            });

        return Ok(strategies);
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static string GetStrategyDescription(OptimizationStrategy strategy)
    {
        return strategy switch
        {
            OptimizationStrategy.LowestTotal => "Menor custo total (livros + frete)",
            OptimizationStrategy.FewestOrders => "Menor número de pedidos",
            OptimizationStrategy.PrioritizeFreeShipping => "Prioriza frete grátis",
            OptimizationStrategy.SingleProvider => "Comprar tudo em um único site",
            _ => "Estratégia desconhecida"
        };
    }
}
