using Microsoft.Extensions.Logging;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Enums;
using System.Diagnostics;

namespace Sherlock.Business.Core.Scrapers.Amazon;

/// <summary>
/// Provider Amazon. Delega a leitura ao <see cref="IAmazonBrowser"/> e traduz o
/// resultado para o mesmo <see cref="QueryResult"/> das livrarias, para que o
/// motor, o cache e o comparador não precisem saber que este é diferente.
/// </summary>
public class AmazonBrowserScraper : IScraper
{
    private readonly IAmazonBrowser _browser;
    private readonly ILogger<AmazonBrowserScraper> _logger;

    public AmazonBrowserScraper(IAmazonBrowser browser, ILogger<AmazonBrowserScraper> logger)
    {
        _browser = browser;
        _logger = logger;
    }

    public ScraperTypeEnum ScraperType => ScraperTypeEnum.AmazonScraper;

    public async Task<QueryResult> ExecuteSearch(SearchParameter parameters)
    {
        var provider = parameters.Source ?? Provider.Amazon;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(parameters.Isbn))
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);

            var offer = await _browser.SearchByIsbnAsync(parameters.Isbn);

            if (offer is null)
            {
                stopwatch.Stop();
                return QueryResult.CreateNoResult(provider, stopwatch.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            _logger.LogInformation("[Amazon] \"{Title}\" - R${Price:F2} em {ElapsedMs}ms (asin {Asin})",
                offer.Title, offer.Price, stopwatch.ElapsedMilliseconds, offer.Asin);

            return QueryResult.CreateSuccess(
                provider,
                offer.Title,
                author: null,
                offer.Price,
                offer.Discount,
                stopwatch.ElapsedMilliseconds,
                offer.ProductUrl);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("[Amazon] Timeout após {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateFailure(provider, QueryErrorType.Timeout, "Request timeout", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // A Amazon é um provider entre 68: se ela falhar, a busca continua.
            stopwatch.Stop();
            _logger.LogError(ex, "[Amazon] Falha após {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return QueryResult.CreateFailure(provider, QueryErrorType.Unknown, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }
}
