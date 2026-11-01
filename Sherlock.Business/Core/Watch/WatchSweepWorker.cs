using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
/// Varre de madrugada o preço de tudo que está sendo observado.
///
/// Duas escolhas explicam o desenho inteiro:
///
/// **O laço é por ISBN distinto, não por pessoa.** Se 500 pessoas observam *O
/// Hobbit*, ele é varrido uma vez e as outras 499 leem o mesmo resultado. O custo
/// da feature é o número de livros diferentes — por isso ela escala para milhares
/// de usuários sem mudar nada (§4.1).
///
/// **03h, não 09h.** Varrer na hora do e-mail seria pôr 67 × N requisições no
/// mesmo instante em que as pessoas começam a usar o site, e o e-mail só sairia ao
/// meio-dia. Varrendo de madrugada, espaçado, as lojas levam o trabalho enquanto
/// estão ociosas e o <see cref="WatchNotifyWorker"/> acha tudo pronto às 09h (§4.3).
/// </summary>
public class WatchSweepWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WatchSettings _settings;
    private readonly SearchPriority _priority;
    private readonly ILogger<WatchSweepWorker> _logger;

    /// <summary>
    /// Chave do advisory lock. Número arbitrário e fixo — só precisa não colidir
    /// com outro lock da aplicação.
    /// </summary>
    private const long LockKey = 8_160_031;

    /// <summary>
    /// A quem as transações da varredura são creditadas. Mesmo id do aquecedor:
    /// o que separa trabalho de robô de busca de gente é a marca `IsPrefetch`.
    /// </summary>
    private const int SweepUserId = 1;

    public WatchSweepWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WatchSettings> settings,
        SearchPriority priority,
        ILogger<WatchSweepWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings?.Value ?? new WatchSettings();
        _priority = priority;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.SweepEnabled)
        {
            _logger.LogInformation("[WatchSweep] Desligado por configuração");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = DailyWindow.AteProxima(DateTime.UtcNow, _settings.SweepHourUtc);
            _logger.LogInformation("[WatchSweep] Próxima rodada em {Horas:F1}h", espera.TotalHours);

            try
            {
                await Task.Delay(espera, stoppingToken);
                await RodadaAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Rodada que falha não derruba o worker: o vigia tenta de novo
                // amanhã, e um dia sem varredura é um dia sem aviso — não um
                // serviço fora do ar.
                _logger.LogError(ex, "[WatchSweep] Rodada falhou");
            }
        }
    }

    private async Task RodadaAsync(CancellationToken cancellationToken)
    {
        // Trava entre instâncias. Com uma réplica é inócua; no dia em que o
        // compose subir duas, evita varredura dobrada em cima das lojas — e é o
        // tipo de coisa que só se descobre que faltou em produção (§4.4).
        using var scope = _scopeFactory.CreateScope();
        var travas = scope.ServiceProvider.GetRequiredService<IAdvisoryLock>();

        await using var trava = await travas.TryAcquireAsync(LockKey, cancellationToken);
        if (trava == null)
        {
            _logger.LogInformation("[WatchSweep] Outra instância já está varrendo; esta rodada passa");
            return;
        }

        var repositorio = scope.ServiceProvider.GetRequiredService<IWatchedBookRepository>();
        var isbns = await OrdenarAsync(repositorio, cancellationToken);

        if (isbns.Count == 0)
        {
            _logger.LogInformation("[WatchSweep] Nada sendo observado");
            return;
        }

        _logger.LogInformation("[WatchSweep] Rodada começando: {Count} ISBNs distintos", isbns.Count);

        var vagas = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrentBooks));
        var tarefas = new List<Task>();
        var varridos = 0;

        foreach (var isbn in isbns)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (!DailyWindow.DentroDaJanela(DateTime.UtcNow, _settings.SweepHourUtc, _settings.SweepUntilHourUtc))
            {
                // A janela existe para as lojas levarem o trabalho enquanto estão
                // ociosas. Passou dela, o resto fica para amanhã — terminar a
                // rodada a qualquer custo seria justamente o que ela evita.
                _logger.LogWarning(
                    "[WatchSweep] Janela encerrada com {Restantes} ISBNs pendentes", isbns.Count - varridos);
                break;
            }

            await vagas.WaitAsync(cancellationToken);
            varridos++;

            tarefas.Add(Task.Run(async () =>
            {
                try
                {
                    await VarrerIsbnAsync(isbn, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Falha de um ISBN não derruba a rodada.
                    _logger.LogError(ex, "[WatchSweep] ISBN {Isbn} falhou", isbn);
                }
                finally
                {
                    vagas.Release();
                }
            }, cancellationToken));

            await Task.Delay(TimeSpan.FromSeconds(_settings.DelayBetweenBooksSeconds), cancellationToken);
        }

        await Task.WhenAll(tarefas);
        _logger.LogInformation("[WatchSweep] Rodada terminada: {Count} ISBNs varridos", varridos);
    }

    /// <summary>
    /// Quem nunca foi varrido vai na frente.
    ///
    /// Livro recém-adicionado tem referência nula — o <c>WatchedBookService</c> só
    /// preenche a partir do histórico que já existia. Até a primeira varredura o
    /// cartão aparece sem preço nenhum, e é essa a espera que mais incomoda quem
    /// acabou de usar a tela.
    /// </summary>
    private static async Task<List<string>> OrdenarAsync(
        IWatchedBookRepository repositorio, CancellationToken cancellationToken)
    {
        var ativos = await repositorio.GetAllActiveWithUserAsync(cancellationToken);

        return ativos
            .GroupBy(w => w.Isbn)
            .OrderByDescending(g => g.Any(w => w.LastCheckedAt == null))
            .ThenBy(g => g.Min(w => w.LastCheckedAt ?? DateTime.MinValue))
            .Select(g => g.Key)
            .ToList();
    }

    private async Task VarrerIsbnAsync(string isbn, CancellationToken cancellationToken)
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
            _logger.LogInformation("[WatchSweep] ISBN {Isbn} interrompido: a vez é de quem está na tela", isbn);
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

        await RegistrarAsync(isbn, melhor, lojas.Count, repositorio, cancellationToken);
    }

    /// <summary>
    /// Modo enxuto: só as lojas que já devolveram este livro, com uma varredura
    /// completa a cada <see cref="WatchSettings.FullSweepEveryDays"/> dias para
    /// pegar loja que passou a ter estoque. Livro novo não tem histórico, então
    /// cai na varredura completa naturalmente.
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
            "[WatchSweep] ISBN {Isbn}: {Lojas} lojas, melhor {Preco}, {Linhas} observados atualizados",
            isbn, lojasConsultadas, melhor?.Price, observados.Count);
    }
}
