using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Base;
using Sherlock.Business.Core.Prefetch;
using Sherlock.Business.Core.Scrapers;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Core.Watch;

/// <summary>
/// Varre o preço de um ISBN e grava o resultado em todos os observados dele.
///
/// Mora aqui, e não dentro do <see cref="WatchSweepWorker"/>, porque dois
/// caminhos precisam exatamente do mesmo trabalho: a rodada da madrugada e a
/// varredura imediata de quem acabou de adicionar um livro
/// (<see cref="ImmediateSweepWorker"/>). Duplicar isso seria manter duas versões
/// da regra que decide quais lojas consultar e o que fazer quando nenhuma
/// responde.
/// </summary>
public class WatchSweepRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WatchSettings _settings;
    private readonly SearchPriority _priority;
    private readonly ILogger<WatchSweepRunner> _logger;

    /// <summary>
    /// A quem as transações da varredura são creditadas. Mesmo id do aquecedor:
    /// o que separa trabalho de robô de busca de gente é a marca `IsPrefetch`.
    /// </summary>
    private const int SweepUserId = 1;

    public WatchSweepRunner(
        IServiceScopeFactory scopeFactory,
        IOptions<WatchSettings> settings,
        SearchPriority priority,
        ILogger<WatchSweepRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings?.Value ?? new WatchSettings();
        _priority = priority;
        _logger = logger;
    }

    /// <param name="origem">Só para o log distinguir a rodada noturna da imediata.</param>
    public async Task VarrerIsbnAsync(string isbn, string origem, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<W16Engine>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryRepository>();
        var repositorio = scope.ServiceProvider.GetRequiredService<IWatchedBookRepository>();

        var lojas = await EscolherLojasAsync(isbn, queries, cancellationToken);

        var requestor = new Requestor
        {
            SearchParameters = new SearchParameter { Isbn = isbn, IsPrefetch = true },
            SourcesToSearch = lojas,
        };

        // Mesmo tratamento do aquecedor: se alguém buscar enquanto a varredura
        // roda, a vez é de quem está na tela.
        var cts = _priority.AbrirAquecimento(cancellationToken);
        SearchResult resultado;
        try
        {
            resultado = await engine.ExecuteTransaction(requestor, SweepUserId, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("[{Origem}] ISBN {Isbn} interrompido: a vez é de quem está na tela", origem, isbn);
            return;
        }
        finally
        {
            _priority.FecharAquecimento(cts);
            cts.Dispose();
        }

        var melhor = resultado.AllQueryResults
            .Where(q => q.Success && q.Price > 0)
            .OrderBy(q => q.Price)
            .FirstOrDefault();

        await RegistrarAsync(isbn, melhor, lojas.Count, origem, repositorio, cancellationToken);
    }

    /// <summary>
    /// Modo enxuto: só as lojas que já devolveram este livro, com uma varredura
    /// completa a cada <see cref="WatchSettings.FullSweepEveryDays"/> dias para
    /// pegar loja que passou a ter estoque. Livro novo não tem histórico, então
    /// cai na varredura completa naturalmente — que é justamente o que a
    /// varredura imediata precisa.
    /// </summary>
    private async Task<List<Provider>> EscolherLojasAsync(
        string isbn, IQueryRepository queries, CancellationToken cancellationToken)
    {
        var todas = Provider.AllSources.Where(p => p.IsActive).ToList();

        var diaDaVarreduraCompleta = DateTime.UtcNow.DayOfYear % Math.Max(1, _settings.FullSweepEveryDays) == 0;
        if (diaDaVarreduraCompleta) return todas;

        var comResultado = await queries.GetProvidersWithResultsAsync(
            isbn, _settings.FullSweepEveryDays, cancellationToken);

        if (comResultado.Count == 0) return todas;

        var enxuto = todas.Where(p => comResultado.Contains(p.Id)).ToList();
        return enxuto.Count > 0 ? enxuto : todas;
    }

    private async Task RegistrarAsync(
        string isbn,
        QueryResult? melhor,
        int lojasConsultadas,
        string origem,
        IWatchedBookRepository repositorio,
        CancellationToken cancellationToken)
    {
        var observados = await repositorio.GetActiveByIsbnAsync(isbn, cancellationToken);
        var agora = DateTime.UtcNow;

        foreach (var livro in observados)
        {
            livro.LastCheckedAt = agora;

            if (melhor == null)
            {
                // Nenhuma loja devolveu o livro hoje. Zerar o LastPrice aqui
                // apagaria o último preço conhecido e a seta "desde ontem" com
                // ele — e "sumiu do estoque" não é informação de preço.
                continue;
            }

            livro.PreviousPrice = livro.LastPrice;
            livro.LastPrice = melhor.Price;
            livro.LastProviderName = melhor.ProviderName;
            livro.LastProductUrl = melhor.ProductUrl;

            if (livro.ReferencePrice == null)
            {
                livro.ReferencePrice = melhor.Price;
                livro.ReferencePriceAt = agora;
            }

            // Capa que faltava na adição: a varredura trouxe sem custo nenhum.
            if (string.IsNullOrWhiteSpace(livro.ImageUrl) && !string.IsNullOrWhiteSpace(melhor.ImageUrl))
                livro.ImageUrl = melhor.ImageUrl;

            if (livro.Title == livro.Isbn && !string.IsNullOrWhiteSpace(melhor.Title))
                livro.Title = melhor.Title;
        }

        await repositorio.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[{Origem}] ISBN {Isbn}: {Lojas} lojas, melhor {Preco}, {Linhas} observados atualizados",
            origem, isbn, lojasConsultadas, melhor?.Price, observados.Count);
    }
}
