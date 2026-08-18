using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sherlock.Business.DTOs;
using Sherlock.Business.Core.Progress;
using Sherlock.Business.Interfaces;
using Sherlock.Api.Constants;
using System.Security.Claims;

namespace Sherlock.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartOptimizationService _cartService;
    private readonly SearchProgressStore _progressStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CartController> _logger;

    public CartController(
        ICartOptimizationService cartService,
        SearchProgressStore progressStore,
        IServiceScopeFactory scopeFactory,
        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _progressStore = progressStore;
        _scopeFactory = scopeFactory;
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

        if (request.Books.Count > AppConstants.Cart.MaxBooksPerOptimization)
        {
            return BadRequest(new { error = $"Máximo de {AppConstants.Cart.MaxBooksPerOptimization} livros por otimização." });
        }

        // Valida cada livro
        foreach (var book in request.Books)
        {
            if (string.IsNullOrWhiteSpace(book.Isbn))
            {
                return BadRequest(new { error = "Todos os livros devem ter um ISBN." });
            }

            if (book.Quantity <= 0)
            {
                book.Quantity = AppConstants.Cart.DefaultQuantity;
            }

            if (book.Quantity > AppConstants.Cart.MaxQuantityPerBook)
            {
                return BadRequest(new { error = $"Quantidade máxima de {AppConstants.Cart.MaxQuantityPerBook} unidades por livro. ISBN: {book.Isbn}" });
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
        [FromQuery] string isbn,
        [FromQuery] string? providerUrls = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return BadRequest(new { error = "O ISBN do livro é obrigatório." });
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
                    Isbn = isbn,
                    Quantity = 1
                }
            },
            Strategy = OptimizationStrategy.LowestTotal,
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
            _logger.LogError(ex, "Erro ao buscar livro com ISBN '{Isbn}'", isbn);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Ocorreu um erro ao buscar o livro. Tente novamente."
            });
        }
    }

    /// <summary>
    /// Busca o melhor provider único para comprar todos os livros do carrinho
    /// </summary>
    /// <param name="request">Lista de livros a buscar</param>
    /// <returns>Melhor provider e uma alternativa</returns>
    [HttpPost("best-provider")]
    [EnableRateLimiting("authenticated")]
    [ProducesResponseType(typeof(BestProviderCartResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FindBestProvider(
        [FromBody] BestProviderCartRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Books == null || !request.Books.Any())
        {
            return BadRequest(new { error = "A lista de livros não pode estar vazia." });
        }

        if (request.Books.Count > AppConstants.Cart.MaxBooksPerOptimization)
        {
            return BadRequest(new { error = $"Máximo de {AppConstants.Cart.MaxBooksPerOptimization} livros por otimização." });
        }

        foreach (var book in request.Books)
        {
            if (string.IsNullOrWhiteSpace(book.Isbn))
            {
                return BadRequest(new { error = "Todos os livros devem ter um ISBN." });
            }
            if (book.Quantity <= 0) book.Quantity = 1;
        }

        var userId = GetUserId();

        _logger.LogInformation(
            "Requisição de melhor provider para {BookCount} livros, usuário: {UserId}",
            request.Books.Count, userId);

        try
        {
            // Usa estratégia SingleProvider para buscar
            var optimizationRequest = new CartOptimizationRequest
            {
                Books = request.Books,
                Strategy = OptimizationStrategy.SingleProvider,
                ProviderUrls = request.ProviderUrls,
                MaxProviders = 1
            };

            var result = await _cartService.OptimizeCartAsync(optimizationRequest, userId, cancellationToken);

            // Converte para BestProviderCartResult
            var bestProviderResult = new BestProviderCartResult
            {
                Success = result.Success,
                Message = result.Message,
                BestProvider = result.ProviderCarts?.FirstOrDefault(),
                BooksNotFound = result.BooksNotFound,
                ExecutionTimeMs = result.ExecutionTimeMs,
                CreditsUsed = result.CreditsUsed,
                FromCache = result.FromCache,
                TotalProvidersSearched = result.ProviderCarts?.Count ?? 0
            };

            // Busca segundo melhor se houver sucesso (sem cache para simplificar)
            if (result.Success && result.ProviderCarts?.Count > 0)
            {
                // Busca alternativa excluindo o melhor provider
                var bestProviderUrl = bestProviderResult.BestProvider?.ProviderUrl;
                if (!string.IsNullOrEmpty(bestProviderUrl))
                {
                    var alternativeRequest = new CartOptimizationRequest
                    {
                        Books = request.Books,
                        Strategy = OptimizationStrategy.SingleProvider,
                        ProviderUrls = request.ProviderUrls?.Where(u => u != bestProviderUrl).ToList(),
                        MaxProviders = 1
                    };

                    // Só busca alternativa se fizer sentido
                    if (alternativeRequest.ProviderUrls == null || alternativeRequest.ProviderUrls.Count > 0)
                    {
                        var altResult = await _cartService.OptimizeCartAsync(alternativeRequest, userId, cancellationToken);
                        if (altResult.Success && altResult.ProviderCarts?.Count > 0)
                        {
                            var altProvider = altResult.ProviderCarts.FirstOrDefault();
                            if (altProvider?.ProviderUrl != bestProviderUrl)
                            {
                                bestProviderResult.SecondBestProvider = altProvider;
                            }
                        }
                    }
                }
            }

            return Ok(bestProviderResult);
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
            _logger.LogError(ex, "Erro ao buscar melhor provider para usuário {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Ocorreu um erro ao processar a busca. Tente novamente."
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

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("UserId não encontrado no token");
    }

    private static string GetStrategyDescription(OptimizationStrategy strategy)
    {
        return strategy switch
        {
            OptimizationStrategy.LowestTotal => "Menor custo total",
            OptimizationStrategy.FewestOrders => "Menor número de pedidos",
            OptimizationStrategy.SingleProvider => "Comprar tudo em um único site",
            _ => "Estratégia desconhecida"
        };
    }

    /// <summary>
    /// Inicia a otimização em segundo plano e devolve um identificador para acompanhar.
    ///
    /// A busca consulta dezenas de lojas e leva dezenas de segundos; responder na hora
    /// permite ao front mostrar progresso em vez de uma espera opaca.
    /// </summary>
    [HttpPost("optimize-async")]
    public IActionResult StartOptimization([FromBody] CartOptimizationRequest request)
    {
        if (request.Books == null || !request.Books.Any())
        {
            return BadRequest(new { error = "A lista de livros não pode estar vazia." });
        }

        var userId = GetUserId();
        var jobId = Guid.NewGuid().ToString("N");
        var progress = _progressStore.Create(jobId);

        // Escopo próprio: o escopo desta requisição morre assim que respondemos.
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICartOptimizationService>();

            SearchProgressScope.Begin(progress);
            try
            {
                var result = await service.OptimizeCartAsync(request, userId, CancellationToken.None);
                progress.Complete(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na otimização em segundo plano (job {JobId})", jobId);
                progress.Fail("Não foi possível concluir a busca.");
            }
            finally
            {
                SearchProgressScope.End();
            }
        });

        return Ok(new { jobId, total = request.ProviderUrls?.Count ?? 0 });
    }

    /// <summary>Andamento de uma busca iniciada por <c>optimize-async</c>.</summary>
    [HttpGet("progress/{jobId}")]
    public IActionResult GetProgress(string jobId)
    {
        var progress = _progressStore.Get(jobId);

        if (progress is null)
            return NotFound(new { error = "Busca não encontrada ou expirada." });

        return Ok(new
        {
            total = progress.Total,
            completed = progress.Completed,
            done = progress.Done,
            error = progress.Error,
            result = progress.Done ? progress.Result : null,
        });
    }
}
