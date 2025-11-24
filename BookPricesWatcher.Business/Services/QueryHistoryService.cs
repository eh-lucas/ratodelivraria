using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;
using System.Text.Json;

namespace Sherlock.Business.Services;

public class QueryHistoryService : IQueryHistoryService
{
    private readonly IQueryRepository _queryRepository;

    public QueryHistoryService(IQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task LogQueryAsync(SearchResult result, string inputParameters, int? userId = null, int? bookId = null)
    {
        var query = new Query
        {
            UserId = userId,
            BookId = bookId,
            StartDateTime = result.InicioConsulta,
            EndDateTime = result.FimConsulta,
            ExecutionTimeMs = result.TempoDecorrido,
            ProvidersQueried = result.TotalSourcesQueried,
            SuccessfulQueries = result.SuccessfulQueries,
            FailedQueries = result.FailedQueries,
            CostCredits = result.CustoCreditos,
            ResultTypeId = result.ResultadoTransacao.IsSuccess ? 1 : 0,
            InputParameters = inputParameters,
            Result = JsonSerializer.Serialize(new
            {
                result.BookPriceResult.Title,
                result.BookPriceResult.Price,
                result.BookPriceResult.Website,
                result.BookPriceResult.Discount,
                result.Errors
            })
        };

        await _queryRepository.LogQueryAsync(query);
    }

    public async Task<IEnumerable<QueryHistoryDto>> GetUserHistoryAsync(int userId, int limit = 20)
    {
        // Nota: Para implementar isso corretamente, precisaríamos adicionar UserId
        // à interface IQueryRepository ou criar um método específico
        var queries = await _queryRepository.GetRecentAsync(limit);
        return queries.Select(MapToDto);
    }

    public async Task<IEnumerable<QueryHistoryDto>> GetRecentQueriesAsync(int limit = 10)
    {
        var queries = await _queryRepository.GetRecentAsync(limit);
        return queries.Select(MapToDto);
    }

    private static QueryHistoryDto MapToDto(Query query)
    {
        return new QueryHistoryDto
        {
            Id = query.Id,
            StartDateTime = query.StartDateTime,
            EndDateTime = query.EndDateTime,
            ExecutionTimeMs = query.ExecutionTimeMs,
            ProvidersQueried = query.ProvidersQueried,
            SuccessfulQueries = query.SuccessfulQueries,
            CostCredits = query.CostCredits,
            InputParameters = query.InputParameters,
            IsSuccess = query.ResultTypeId == 1
        };
    }
}
