using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;
using System.Diagnostics;

namespace Sherlock.Business.Core.Base;

/// <summary>
/// Motor principal de execução de transações de busca de preços.
/// Orquestra scrapers, cache, comparação e registro de resultados.
/// </summary>
public class W16Engine
{
    private readonly Comparator _comparator;
    private readonly ScraperFactory _scraperFactory;
    private readonly ILogger<W16Engine> _logger;

    public W16Engine() : this(NullLogger<W16Engine>.Instance)
    {
    }

    public W16Engine(ILogger<W16Engine> logger)
    {
        _comparator = new Comparator();
        _scraperFactory = new ScraperFactory();
        _logger = logger;
    }

    public async Task<SearchResult> ExecuteTransaction(Requestor requestor)
    {
        var stopwatch = Stopwatch.StartNew();
        var transactionId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("Iniciando transação {TransactionId} com {SourceCount} fontes",
            transactionId, requestor.SourcesToSearch.Count);

        var preResults = new List<BookPriceResult>();
        var result = new SearchResult
        {
            InicioConsulta = DateTime.Now,
            TotalSourcesQueried = requestor.SourcesToSearch.Count
        };

        try
        {
            // TODO: Verificar cache antes de fazer scraping
            // if (TryGetFromCache(requestor, out var cachedResult))
            //     return cachedResult;

            var scrapers = _scraperFactory.CreateScrapers(requestor);

            foreach (var scraper in scrapers)
            {
                foreach (var source in requestor.SourcesToSearch)
                {
                    try
                    {
                        _logger.LogDebug("Consultando fonte {SourceName}", source.Name);

                        var parameters = requestor.SearchParameters;
                        parameters.Source = source;

                        var singleResult = await scraper.ExecuteSearch(parameters);

                        // Só adiciona se teve resultado válido
                        if (!string.IsNullOrEmpty(singleResult.Title) && singleResult.Price > 0)
                        {
                            preResults.Add(singleResult);
                            result.SuccessfulQueries++;

                            _logger.LogInformation("Resultado encontrado em {SourceName}: {Title} - R${Price:F2}",
                                source.Name, singleResult.Title, singleResult.Price);
                        }
                        else
                        {
                            _logger.LogDebug("Fonte {SourceName} não retornou resultado válido", source.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedQueries++;
                        result.Errors.Add($"{source.Name}: {ex.Message}");

                        _logger.LogWarning(ex, "Falha ao consultar fonte {SourceName}", source.Name);
                        // Continua para próxima fonte ao invés de quebrar tudo
                    }
                }
            }

            // Compara e seleciona o melhor resultado
            result.BookPriceResult = _comparator.Compare(preResults);

            // Define o status da transação
            result.ResultadoTransacao = DetermineResultType(result, preResults.Count);

            // Calcula custo baseado em queries bem-sucedidas
            result.CustoCreditos = CalculateCost(result);

            _logger.LogInformation(
                "Transação {TransactionId} concluída: {Status} - {Successful}/{Total} fontes, melhor preço: R${BestPrice:F2} em {Elapsed}ms",
                transactionId,
                result.ResultadoTransacao.Name,
                result.SuccessfulQueries,
                result.TotalSourcesQueried,
                result.BookPriceResult?.Price ?? 0,
                stopwatch.ElapsedMilliseconds);

            // TODO: Persistir resultados no banco
            // await SaveResults(result, preResults);
        }
        catch (Exception ex)
        {
            result.ResultadoTransacao = ResultType.AllFailed;
            result.Errors.Add($"Erro fatal: {ex.Message}");

            _logger.LogError(ex, "Erro fatal na transação {TransactionId}", transactionId);
        }
        finally
        {
            stopwatch.Stop();
            result.TempoDecorrido = stopwatch.ElapsedMilliseconds;
            result.FimConsulta = DateTime.Now;
        }

        return result;
    }

    private static ResultType DetermineResultType(SearchResult result, int validResults)
    {
        if (result.FailedQueries == result.TotalSourcesQueried)
            return ResultType.AllFailed;

        if (validResults == 0)
            return ResultType.NoResults;

        if (result.FailedQueries > 0)
            return ResultType.PartialSuccess;

        return ResultType.Success;
    }

    private static int CalculateCost(SearchResult result)
    {
        // Custo base por transação + custo por query bem-sucedida
        const int baseCost = 1;
        const int costPerSuccessfulQuery = 1;

        if (!result.ResultadoTransacao.IsBillable)
            return 0;

        return baseCost + (result.SuccessfulQueries * costPerSuccessfulQuery);
    }
}
