using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Prefetch;
using Sherlock.Business.Core.Optimization;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sherlock.Business.Services;

public class CartOptimizationService : ICartOptimizationService
{
    private readonly W16Engine _engine;
    private readonly CartOptimizer _optimizer;
    private readonly IQueryHistoryService _queryHistoryService;
    private readonly SearchPriority _priority;
    private readonly CartRequerySettings _requerySettings;
    private readonly ILogger<CartOptimizationService> _logger;

    public CartOptimizationService(
        W16Engine engine,
        CartOptimizer optimizer,
        IQueryHistoryService queryHistoryService,
        SearchPriority priority,
        IOptions<CartRequerySettings> requerySettings,
        ILogger<CartOptimizationService> logger)
    {
        _engine = engine;
        _optimizer = optimizer;
        _queryHistoryService = queryHistoryService;
        _priority = priority;
        _requerySettings = requerySettings?.Value ?? new CartRequerySettings();
        _logger = logger;
    }

    public async Task<CartOptimizationResult> OptimizeCartAsync(
        CartOptimizationRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Tem gente esperando na tela: o aquecedor cede a vez enquanto isto roda.
        using var vez = _priority.EntrarBuscaDeGente();

        _logger.LogInformation(
            "Iniciando otimização de carrinho com {BookCount} livros para usuário {UserId}",
            request.Books.Count, userId);

        // Nota: Transactions NÃO são cacheadas. Apenas as Queries individuais são cacheadas pelo W16Engine.
        // Uma transação de carrinho com 3 livros em 10 providers = 30 queries (cada uma pode ser cacheada)

        // Busca preços para todos os livros
        var allPrices = new List<BookPriceOption>();
        var providerQueries = new List<ProviderQueryDetail>();
        var searchTasks = new List<Task<(CartBookItem book, SearchResult result)>>();
        int creditsUsed = 0;

        // Determina quais providers usar
        var sourcesToSearch = GetSourcesToSearch(request.ProviderUrls);

        // Mais de 1 livro = carrinho; 1 livro = consulta unitária (mesmo serviço atende ambos)
        var isCart = request.Books.Count > 1;

        foreach (var book in request.Books)
        {
            searchTasks.Add(SearchBookPricesAsync(book, sourcesToSearch, userId, isCart, cancellationToken));
        }

        var searchResults = await Task.WhenAll(searchTasks);

        foreach (var (book, result) in searchResults)
        {
            creditsUsed += result.CustoCreditos;

            // Detalhe bruto por provider (inclui falhas e tempos) para a tela de resultado
            foreach (var q in result.AllQueryResults ?? new List<QueryResult>())
            {
                providerQueries.Add(new ProviderQueryDetail
                {
                    Isbn = book.Isbn,
                    ProviderId = q.ProviderId,
                    ProviderName = q.ProviderName,
                    ProviderUrl = q.ProviderUrl,
                    Success = q.Success,
                    HasResult = q.HasValidResult,
                    Title = q.Title,
                    Author = q.Author,
                    Price = q.Price > 0 ? q.Price : null,
                    Discount = q.Discount > 0 ? q.Discount : null,
                    ProductUrl = q.ProductUrl,
                    ImageUrl = q.ImageUrl,
                    ResponseTimeMs = q.ResponseTimeMs,
                    ErrorMessage = q.ErrorMessage,
                    ErrorType = q.ErrorType?.ToString(),
                    FromCache = q.FromCache
                });
            }

            // Preferimos AllQueryResults: carrega dados completos por provider
            // (ProviderId, ProviderUrl e ProductUrl — a página do livro na loja).
            var validQueries = result.AllQueryResults?
                .Where(q => q.HasValidResult)
                .ToList() ?? new List<QueryResult>();

            if (validQueries.Count > 0)
            {
                foreach (var q in validQueries)
                {
                    allPrices.Add(new BookPriceOption
                    {
                        BookTitle = q.Title ?? book.Isbn,
                        Isbn = book.Isbn,
                        Author = q.Author,
                        ProviderId = q.ProviderId,
                        ProviderName = q.ProviderName,
                        ProviderUrl = q.ProviderUrl,
                        Price = q.Price,
                        Discount = q.Discount,
                        ProductUrl = q.ProductUrl,
                        ImageUrl = q.ImageUrl,
                        Available = true
                    });
                }
            }
            else if (result.AllResults != null && result.AllResults.Any())
            {
                // Fallback legado (BookPriceResult não carrega URL do produto/provider).
                foreach (var priceResult in result.AllResults)
                {
                    allPrices.Add(new BookPriceOption
                    {
                        BookTitle = priceResult.Title ?? book.Isbn,
                        Isbn = book.Isbn,
                        Author = priceResult.Author,
                        ProviderName = priceResult.Website,
                        Price = priceResult.Price,
                        Discount = priceResult.Discount,
                        Available = true
                    });
                }
            }
        }

        // Conta total de queries executadas
        int totalQueries = searchResults.Sum(r => r.result.TotalSourcesQueried);

        _logger.LogInformation(
            "Encontrados {PriceCount} preços para {BookCount} livros ({TotalQueries} queries executadas)",
            allPrices.Count, request.Books.Count, totalQueries);

        // Segunda e última chance para quem não respondeu.
        var semResposta = await RepescarAsync(
            providerQueries, allPrices, sourcesToSearch, isCart, cancellationToken);

        // Executa otimização
        var optimizationResult = _optimizer.Optimize(allPrices, request, semResposta);
        optimizationResult.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        optimizationResult.CreditsUsed = creditsUsed;
        optimizationResult.TotalQueriesExecuted = totalQueries;
        optimizationResult.ProviderQueries = providerQueries;

        stopwatch.Stop();

        _logger.LogInformation(
            "Otimização concluída em {Elapsed}ms. Total: R${Total:F2}, Economia: R${Savings:F2} ({SavingsPercent:F1}%)",
            stopwatch.ElapsedMilliseconds,
            optimizationResult.TotalCost,
            optimizationResult.Savings,
            optimizationResult.SavingsPercent);

        return optimizationResult;
    }

    private async Task<(CartBookItem book, SearchResult result)> SearchBookPricesAsync(
        CartBookItem book,
        List<Provider> sources,
        int userId,
        bool isCart,
        CancellationToken cancellationToken)
    {
        var requestor = new Requestor
        {
            SearchParameters = new SearchParameter
            {
                Isbn = book.Isbn,
                IsCart = isCart
            },
            SourcesToSearch = sources
        };

        var result = await _engine.ExecuteTransaction(requestor, userId, cancellationToken);
        return (book, result);
    }


    /// <summary>
    /// Dispara mais uma vez, e só mais uma, as consultas que não responderam.
    ///
    /// Em 31/10/2026 a Brodbeck perdeu um único volume de sete por timeout de
    /// 20s e, por causa disso, ficou atrás da Guilherme Freire num carrinho em
    /// que saía R$ 10,10 mais barata. O que a loja precisava era de uma segunda
    /// chance, não de um lugar diferente no ranking.
    ///
    /// Devolve os pares loja+ISBN que continuaram mudos, para a comparação poder
    /// marcá-los em vez de deixá-los sumir em silêncio.
    /// </summary>
    private async Task<HashSet<(string ProviderName, string Isbn)>> RepescarAsync(
        List<ProviderQueryDetail> providerQueries,
        List<BookPriceOption> allPrices,
        List<Provider> sources,
        bool isCart,
        CancellationToken cancellationToken)
    {
        var semResposta = new HashSet<(string, string)>();

        var mudas = providerQueries
            .Where(q => !q.Success && EhFalhaDeResposta(q.ErrorType))
            .ToList();

        if (mudas.Count == 0)
            return semResposta;

        if (!_requerySettings.Enabled)
        {
            foreach (var q in mudas)
                semResposta.Add((q.ProviderName, q.Isbn.ToLowerInvariant()));
            return semResposta;
        }

        // Loja que não devolveu preço nenhum no carrinho inteiro está fora do ar
        // ou não atende — repescar seria insistir contra um host morto. Em
        // 31/10/2026 foi o caso de Constantino e Paula Marisa, mudas nos 4
        // volumes; a Brodbeck, que tinha 6 dos 7, é exatamente quem isto busca.
        var lojasVivas = allPrices
            .Select(p => p.ProviderName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var alvos = mudas.Where(q => lojasVivas.Contains(q.ProviderName)).ToList();

        foreach (var q in mudas.Where(q => !lojasVivas.Contains(q.ProviderName)))
            semResposta.Add((q.ProviderName, q.Isbn.ToLowerInvariant()));

        if (alvos.Count == 0)
            return semResposta;

        var porUrl = sources.ToDictionary(p => p.Id);

        using var teto = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        teto.CancelAfter(TimeSpan.FromSeconds(_requerySettings.TotalBudgetSeconds));

        _logger.LogInformation(
            "[Repescagem] {Count} consultas sem resposta, tentando mais uma vez (teto {Teto}s)",
            alvos.Count, _requerySettings.TotalBudgetSeconds);

        var tentativas = alvos.Select(async alvo =>
        {
            if (!porUrl.TryGetValue(alvo.ProviderId, out var provider))
                return (alvo, resultado: (QueryResult?)null);

            var requestor = new Requestor
            {
                SearchParameters = new SearchParameter
                {
                    Isbn = alvo.Isbn,
                    IsCart = isCart,
                    // Tentativa única: o retry do scraper aqui devolveria o teto
                    // de 20s que a repescagem existe para evitar.
                    RequestTimeoutSeconds = _requerySettings.RequestTimeoutSeconds,
                    SkipRetry = true
                },
                SourcesToSearch = new List<Provider> { provider }
            };

            var sacola = new ConcurrentBag<QueryResult>();
            try
            {
                await _engine.ExecuteSearch(requestor, new ParallelismMetrics(), sacola, teto.Token);
            }
            catch (OperationCanceledException)
            {
                return (alvo, resultado: (QueryResult?)null);
            }

            return (alvo, resultado: sacola.FirstOrDefault(r => r.HasValidResult));
        });

        var resultados = await Task.WhenAll(tentativas);

        int recuperadas = 0;

        foreach (var (alvo, resultado) in resultados)
        {
            if (resultado is null)
            {
                semResposta.Add((alvo.ProviderName, alvo.Isbn.ToLowerInvariant()));
                continue;
            }

            recuperadas++;

            allPrices.Add(new BookPriceOption
            {
                BookTitle = resultado.Title ?? alvo.Isbn,
                Isbn = alvo.Isbn,
                Author = resultado.Author,
                ProviderId = resultado.ProviderId,
                ProviderName = resultado.ProviderName,
                ProviderUrl = resultado.ProviderUrl,
                Price = resultado.Price,
                Discount = resultado.Discount,
                ProductUrl = resultado.ProductUrl,
                ImageUrl = resultado.ImageUrl,
                Available = true
            });

            // A tela mostra o detalhe bruto por loja: sem isto, a linha
            // continuaria contando a história do timeout já superado.
            var detalhe = providerQueries.FirstOrDefault(
                q => q.ProviderId == alvo.ProviderId && q.Isbn == alvo.Isbn);

            if (detalhe is not null)
            {
                detalhe.Success = true;
                detalhe.HasResult = true;
                detalhe.Title = resultado.Title;
                detalhe.Author = resultado.Author;
                detalhe.Price = resultado.Price > 0 ? resultado.Price : null;
                detalhe.Discount = resultado.Discount > 0 ? resultado.Discount : null;
                detalhe.ProductUrl = resultado.ProductUrl;
                detalhe.ImageUrl = resultado.ImageUrl;
                detalhe.ResponseTimeMs = resultado.ResponseTimeMs;
                detalhe.ErrorMessage = null;
                detalhe.ErrorType = null;
            }
        }

        _logger.LogInformation(
            "[Repescagem] {Ok} de {Total} recuperadas; {Mudas} seguem sem resposta",
            recuperadas, alvos.Count, semResposta.Count);

        return semResposta;
    }

    /// <summary>
    /// Falha de resposta — a loja não falou. Distinta de "a loja respondeu que
    /// não tem o livro", que é informação boa e não se repesca.
    /// </summary>
    private static bool EhFalhaDeResposta(string? errorType) =>
        errorType is nameof(QueryErrorType.Timeout) or nameof(QueryErrorType.Network);

    private List<Provider> GetSourcesToSearch(List<string>? providerUrls)
    {
        if (providerUrls != null && providerUrls.Count > 0)
        {
            var urls = providerUrls.ToHashSet();
            var filtered = Provider.AllSources
                .Where(p => urls.Contains(p.Url))
                .ToList();

            if (filtered.Count > 0)
            {
                return filtered;
            }
        }

        // Fallback: usa todos os providers ativos
        return Provider.AllSources
            .Where(s => s.IsActive)
            .ToList();
    }
}
