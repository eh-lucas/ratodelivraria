using Sherlock.Business.Core.Scrapers;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;
using System.Text.Json;

namespace Sherlock.Business.Services;

public class QueryHistoryService : IQueryHistoryService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IQueryRepository _queryRepository;

    public QueryHistoryService(
        ITransactionRepository transactionRepository,
        IQueryRepository queryRepository)
    {
        _transactionRepository = transactionRepository;
        _queryRepository = queryRepository;
    }

    public async Task<Transaction> LogTransactionAsync(
        SearchResult result,
        string inputParameters,
        List<QueryResultInfo> queryResults,
        int? userId = null)
    {
        // Cria a transação
        var transaction = new Transaction
        {
            UserId = userId,
            StartedAt = result.InicioConsulta,
            EndedAt = result.FimConsulta,
            ExecutionTimeMs = result.TempoDecorrido,
            TotalProvidersQueried = result.TotalSourcesQueried,
            SuccessfulQueries = result.SuccessfulQueries,
            FailedQueries = result.FailedQueries,
            CostCredits = result.CustoCreditos,
            ResultTypeId = result.ResultadoTransacao.Id,
            InputParameters = inputParameters,
            Errors = result.Errors.Count > 0 ? JsonSerializer.Serialize(result.Errors) : null
        };

        // Salva a transação primeiro para obter o ID
        var savedTransaction = await _transactionRepository.CreateTransactionAsync(transaction);

        // Cria as queries individuais
        var queries = queryResults.Select(qr => new Query
        {
            TransactionId = savedTransaction.Id,
            ProviderId = qr.ProviderId,
            ResponseTimeMs = qr.ResponseTimeMs,
            Success = qr.Success,
            Title = qr.Title,
            Author = qr.Author,
            Price = qr.Price,
            Discount = qr.Discount,
            ProductUrl = qr.ProductUrl,
            ErrorMessage = qr.ErrorMessage
        }).ToList();

        // Salva todas as queries
        if (queries.Count > 0)
        {
            await _queryRepository.AddQueriesAsync(queries);

            // Identifica a melhor query (menor preço com sucesso)
            var bestQuery = await _queryRepository.GetBestQueryForTransactionAsync(savedTransaction.Id);
            if (bestQuery != null)
            {
                await _transactionRepository.UpdateBestQueryAsync(savedTransaction.Id, bestQuery.Id);
            }
        }

        return savedTransaction;
    }

    public async Task<IEnumerable<TransactionHistoryDto>> GetUserHistoryAsync(int userId, int limit = 20)
    {
        var transactions = await _transactionRepository.GetByUserIdAsync(userId, limit);
        return transactions.Select(MapToHistoryDto);
    }

    public async Task<IEnumerable<TransactionHistoryDto>> GetRecentTransactionsAsync(int limit = 10)
    {
        var transactions = await _transactionRepository.GetRecentAsync(limit);
        return transactions.Select(MapToHistoryDto);
    }

    public async Task<TransactionDetailDto?> GetTransactionDetailAsync(int transactionId, int? ownerUserId = null)
    {
        var transaction = await _transactionRepository.GetWithQueriesAsync(transactionId);
        if (transaction == null)
            return null;

        if (ownerUserId.HasValue && transaction.UserId != ownerUserId.Value)
            return null;

        return MapToDetailDto(transaction);
    }

    private static TransactionHistoryDto MapToHistoryDto(Transaction transaction)
    {
        var dto = new TransactionHistoryDto
        {
            Id = transaction.Id,
            StartedAt = transaction.StartedAt,
            EndedAt = transaction.EndedAt,
            ExecutionTimeMs = transaction.ExecutionTimeMs,
            TotalProvidersQueried = transaction.TotalProvidersQueried,
            SuccessfulQueries = transaction.SuccessfulQueries,
            CostCredits = transaction.CostCredits,
            InputParameters = transaction.InputParameters,
            IsSuccess = transaction.ResultTypeId == 1 || transaction.ResultTypeId == 2, // Success ou PartialSuccess
        };

        var offers = transaction.Queries
            .Where(IsOffer)
            .OrderBy(q => q.Price)
            .ToList();
        var best = offers.FirstOrDefault();

        FillBest(dto, offers.Count, best?.Title, best?.Author, best?.Price,
            best?.Provider?.Name, best?.ProductUrl, best?.ImageUrl,
            transaction.Queries.Select(q => (q.ImageUrl, q.Author, q.Title)));
        dto.UnavailableCount = transaction.Queries.Count(IsUnavailable);

        return dto;
    }

    private static bool IsOffer(Query q) => q.Success && q.Available && q.Price > 0;

    /// <summary>A loja achou o livro, mas sem estoque: não é oferta, mas também não é "não encontrado".</summary>
    private static bool IsUnavailable(Query q) => q.Success && !q.Available && q.Price > 0;

    /// <summary>
    /// Capa e autor são do livro, não da loja: se a melhor oferta veio sem eles
    /// (a Amazon, por exemplo, não traz autor), pega de qualquer outra loja que
    /// respondeu. Entre capas, prefere a do CDN da Cedet, que é a mesma em todas
    /// as lojas e já fica em cache no navegador pela caixa de busca.
    /// </summary>
    private static void FillBest(
        TransactionHistoryDto dto, int offersCount,
        string? title, string? author, decimal? price, string? provider, string? productUrl, string? imageUrl,
        IEnumerable<(string? ImageUrl, string? Author, string? Title)> allQueries)
    {
        var all = allQueries.ToList();

        dto.OffersCount = offersCount;
        // Sem oferta, o título de qualquer loja que respondeu ainda identifica o livro
        dto.BestTitle = FirstNonEmpty(title)
            ?? all.Select(q => FirstNonEmpty(q.Title)).FirstOrDefault(t => t != null);
        dto.BestPrice = price;
        dto.BestProvider = provider;
        dto.BestProductUrl = productUrl;
        dto.BestAuthor = FirstNonEmpty(author)
            ?? all.Select(q => FirstNonEmpty(q.Author)).FirstOrDefault(a => a != null);

        var images = all.Select(q => FirstNonEmpty(q.ImageUrl)).Where(i => i != null).ToList();
        dto.ImageUrl = images.FirstOrDefault(i => i!.Contains("static.cedet.com.br", StringComparison.OrdinalIgnoreCase))
            ?? FirstNonEmpty(imageUrl)
            ?? images.FirstOrDefault();
    }

    private static string? FirstNonEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TransactionDetailDto MapToDetailDto(Transaction transaction)
    {
        var dto = new TransactionDetailDto
        {
            Id = transaction.Id,
            StartedAt = transaction.StartedAt,
            EndedAt = transaction.EndedAt,
            ExecutionTimeMs = transaction.ExecutionTimeMs,
            TotalProvidersQueried = transaction.TotalProvidersQueried,
            SuccessfulQueries = transaction.SuccessfulQueries,
            CostCredits = transaction.CostCredits,
            InputParameters = transaction.InputParameters,
            IsSuccess = transaction.ResultTypeId == 1 || transaction.ResultTypeId == 2,
            Queries = transaction.Queries.Select(q => new QueryDetailDto
            {
                Id = q.Id,
                ProviderId = q.ProviderId,
                ProviderName = q.Provider?.Name ?? $"Provider #{q.ProviderId}",
                ResponseTimeMs = q.ResponseTimeMs,
                Success = q.Success,
                Title = q.Title,
                Author = q.Author,
                Price = q.Price,
                Discount = q.Discount,
                ProductUrl = q.ProductUrl,
                ErrorMessage = q.ErrorMessage,
                FromCache = q.FromCache,
                Available = q.Available
            })
            // Ofertas por preço; depois as indisponíveis; por último quem falhou.
            .OrderBy(q => q.Success && q.Price > 0 ? (q.Available ? 0 : 1) : 2)
            .ThenBy(q => q.Price ?? decimal.MaxValue)
            .ToList()
        };

        var offers = transaction.Queries.Where(IsOffer).OrderBy(q => q.Price).ToList();
        var best = offers.FirstOrDefault();

        FillBest(dto, offers.Count, best?.Title, best?.Author, best?.Price,
            best?.Provider?.Name ?? (best == null ? null : $"Provider #{best.ProviderId}"),
            best?.ProductUrl, best?.ImageUrl,
            transaction.Queries.Select(q => (q.ImageUrl, q.Author, q.Title)));
        dto.UnavailableCount = transaction.Queries.Count(IsUnavailable);

        return dto;
    }
}
