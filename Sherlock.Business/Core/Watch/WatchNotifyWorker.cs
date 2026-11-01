using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Core.Watch;

/// <summary>
/// Manda o resumo diário, às 09h de Brasília.
///
/// Separado da varredura de propósito (§4.3): a pessoa percebe o e-mail às 9h,
/// mas as lojas levaram o trabalho às 3h, quando estavam ociosas. Este worker não
/// faz requisição nenhuma para fora — ele só lê o que a varredura já gravou e
/// decide quem merece e-mail.
///
/// O trabalho dele é, inteiro, não incomodar à toa. Ver
/// <see cref="WatchRuleEvaluator"/> para a regra e o porquê dela.
/// </summary>
public class WatchNotifyWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WatchSettings _settings;
    private readonly ILogger<WatchNotifyWorker> _logger;

    private const long LockKey = 8_160_032;

    public WatchNotifyWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WatchSettings> settings,
        ILogger<WatchNotifyWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings?.Value ?? new WatchSettings();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.NotifyEnabled)
        {
            _logger.LogInformation("[WatchNotify] Desligado por configuração");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = DailyWindow.AteProxima(DateTime.UtcNow, _settings.NotifyHourUtc);
            _logger.LogInformation("[WatchNotify] Próximo envio em {Horas:F1}h", espera.TotalHours);

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
                _logger.LogError(ex, "[WatchNotify] Rodada falhou");
            }
        }
    }

    private async Task RodadaAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        // Duas instâncias mandando o mesmo resumo é pior que nenhuma: o mesmo
        // e-mail duas vezes é o jeito mais rápido de a pessoa marcar como spam.
        var travas = scope.ServiceProvider.GetRequiredService<IAdvisoryLock>();
        await using var trava = await travas.TryAcquireAsync(LockKey, cancellationToken);
        if (trava == null)
        {
            _logger.LogInformation("[WatchNotify] Outra instância já está enviando; esta rodada passa");
            return;
        }

        var repositorio = scope.ServiceProvider.GetRequiredService<IWatchedBookRepository>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var observados = await repositorio.GetAllActiveWithUserAsync(cancellationToken);
        if (observados.Count == 0)
        {
            _logger.LogInformation("[WatchNotify] Nada sendo observado");
            return;
        }

        var menores = await repositorio.GetLowestPricesAsync(
            observados.Select(o => o.Isbn), _settings.LowestPriceWindowDays, cancellationToken);

        var avaliador = new WatchRuleEvaluator(_settings);
        var agora = DateTime.UtcNow;

        var porPessoa = new Dictionary<int, List<WatchDigestItem>>();

        foreach (var livro in observados)
        {
            menores.TryGetValue(livro.Isbn, out var menor);
            var decisao = avaliador.Avaliar(livro, menor > 0 ? menor : null, agora);
            if (!decisao.Notify) continue;

            if (!porPessoa.TryGetValue(livro.UserId, out var lista))
                porPessoa[livro.UserId] = lista = new List<WatchDigestItem>();

            lista.Add(new WatchDigestItem(livro, decisao.Reason));
        }

        if (porPessoa.Count == 0)
        {
            _logger.LogInformation("[WatchNotify] Nenhum livro passou pela regra hoje");
            return;
        }

        var enviados = 0;

        foreach (var (userId, itens) in porPessoa)
        {
            var dono = itens[0].Book.User;
            if (dono == null || string.IsNullOrWhiteSpace(dono.Email))
            {
                _logger.LogWarning("[WatchNotify] Usuário {UserId} sem e-mail; {Count} avisos descartados",
                    userId, itens.Count);
                continue;
            }

            // Mais barato no topo. Quem abre o e-mail no celular vê o maior
            // achado primeiro, não o primeiro que entrou na lista.
            var ordenados = itens.OrderBy(i => i.Book.LastPrice ?? decimal.MaxValue).ToList();

            try
            {
                await email.SendAsync(
                    dono.Email,
                    WatchDigest.Assunto(ordenados),
                    WatchDigest.Html(dono.FirstName, ordenados, _settings.SiteUrl),
                    WatchDigest.Texto(dono.FirstName, ordenados, _settings.SiteUrl),
                    cancellationToken);

                MarcarComoAvisados(ordenados, agora);
                enviados++;
            }
            catch (Exception ex)
            {
                // Um e-mail perdido não pode derrubar a rodada — e, principalmente,
                // não marca os livros como avisados: se não saiu, amanhã tenta de
                // novo em vez de entrar em silêncio por sete dias.
                //
                // Sem o endereço no log: o Serilog guarda 30 dias de arquivo, e
                // isso poria a base de e-mails em texto puro fora do banco (§2.3).
                _logger.LogError(ex, "[WatchNotify] Falha ao enviar para o usuário {UserId}", userId);
            }
        }

        await repositorio.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[WatchNotify] {Enviados} de {Total} resumos enviados", enviados, porPessoa.Count);
    }

    /// <summary>
    /// Fecha o silêncio de 7 dias para os livros que acabaram de ir no e-mail, e
    /// guarda o preço avisado — é contra ele que a próxima queda é medida.
    ///
    /// Preço-alvo atingido põe o livro em pausa: o alvo era o trabalho, e o
    /// trabalho acabou. Quem quiser continuar observando reativa na tela (§3.3.1).
    /// </summary>
    private static void MarcarComoAvisados(IEnumerable<WatchDigestItem> itens, DateTime agora)
    {
        foreach (var item in itens)
        {
            var livro = item.Book;
            livro.LastNotifiedAt = agora;
            livro.LastNotifiedPrice = livro.LastPrice;

            if (livro.Rule == WatchRule.TargetPrice) livro.Active = false;
        }
    }
}
