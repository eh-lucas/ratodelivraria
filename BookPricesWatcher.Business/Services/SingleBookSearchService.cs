using Microsoft.Extensions.Logging;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using System.Diagnostics;

namespace Sherlock.Business.Services;

public class SingleBookSearchService : ISingleBookSearchService
{
    private readonly W16Engine _engine;
    private readonly ILogger<SingleBookSearchService> _logger;

    public SingleBookSearchService(
        W16Engine engine,
        ILogger<SingleBookSearchService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<SingleBookSearchResult> SearchAsync(
        SingleBookSearchRequest request,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var searchTerm = !string.IsNullOrEmpty(request.Isbn) ? $"ISBN:{request.Isbn}" : request.Title;
        _logger.LogInformation(
            "Iniciando busca de livro único: {SearchTerm} para usuário {UserId}",
            searchTerm, userId ?? 0);

        var result = new SingleBookSearchResult();

        try
        {
            var searchParam = new SearchParameter
            {
                BookTitle = request.Title ?? string.Empty,
                Isbn = request.Isbn,
                AuthorName = request.Author,
                IsExactSearch = false
            };

            var sources = GetSourcesToSearch(request.ProviderUrls);
            var requestor = new Requestor(searchParam, sources);

            var searchResult = await _engine.ExecuteTransaction(requestor, cancellationToken);

            result.TotalProvidersSearched = searchResult.TotalSourcesQueried;
            result.ProvidersWithResults = searchResult.SuccessfulQueries;
            result.CreditsUsed = searchResult.CustoCreditos;
            result.FromCache = searchResult.FromCache;

            // Converte resultados para BookPriceOption ordenados por preço
            var allOptions = ConvertToBookPriceOptions(searchResult, request.Title ?? string.Empty);

            if (allOptions.Count > 0)
            {
                result.Success = true;
                result.Message = $"Encontrados {allOptions.Count} resultados";
                result.BestOption = allOptions[0];
                result.Alternatives = allOptions.Skip(1).Take(2).ToList();
            }
            else
            {
                result.Success = false;
                result.Message = "Nenhum resultado encontrado";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar livro: {Title}", request.Title);
            result.Success = false;
            result.Message = $"Erro na busca: {ex.Message}";
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Busca concluída em {Elapsed}ms. Resultado: {Success}, Melhor preço: {Price}",
            result.ExecutionTimeMs,
            result.Success,
            result.BestOption?.Price ?? 0);

        return result;
    }

    private List<BookPriceOption> ConvertToBookPriceOptions(SearchResult searchResult, string bookTitle)
    {
        var options = new List<BookPriceOption>();

        // Usa AllResults se disponível
        if (searchResult.AllResults != null && searchResult.AllResults.Count > 0)
        {
            foreach (var priceResult in searchResult.AllResults)
            {
                if (priceResult.Price <= 0) continue;

                var provider = FindProviderByName(priceResult.Website);
                options.Add(new BookPriceOption
                {
                    BookTitle = !string.IsNullOrEmpty(priceResult.Title) ? priceResult.Title : bookTitle,
                    Author = priceResult.Author,
                    ProviderId = provider?.Id ?? 0,
                    ProviderName = priceResult.Website,
                    ProviderUrl = provider?.Url ?? string.Empty,
                    Price = priceResult.Price,
                    Discount = priceResult.Discount > 0 ? priceResult.Discount : null,
                    Available = true
                });
            }
        }

        // Adiciona BookPriceResult se não estiver em AllResults
        if (searchResult.BookPriceResult != null &&
            searchResult.BookPriceResult.Price > 0 &&
            !string.IsNullOrEmpty(searchResult.BookPriceResult.Website))
        {
            var exists = options.Any(o =>
                o.ProviderName == searchResult.BookPriceResult.Website &&
                o.Price == searchResult.BookPriceResult.Price);

            if (!exists)
            {
                var provider = FindProviderByName(searchResult.BookPriceResult.Website);
                options.Add(new BookPriceOption
                {
                    BookTitle = !string.IsNullOrEmpty(searchResult.BookPriceResult.Title)
                        ? searchResult.BookPriceResult.Title
                        : bookTitle,
                    Author = searchResult.BookPriceResult.Author,
                    ProviderId = provider?.Id ?? 0,
                    ProviderName = searchResult.BookPriceResult.Website,
                    ProviderUrl = provider?.Url ?? string.Empty,
                    Price = searchResult.BookPriceResult.Price,
                    Discount = searchResult.BookPriceResult.Discount > 0
                        ? searchResult.BookPriceResult.Discount
                        : null,
                    Available = true
                });
            }
        }

        // Ordena por preço (menor primeiro)
        return options.OrderBy(o => o.Price).ToList();
    }

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

        return Provider.AllSources.Where(s => s.IsActive).ToList();
    }

    private Provider? FindProviderByName(string name)
    {
        return Provider.AllSources.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            p.Url.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}
