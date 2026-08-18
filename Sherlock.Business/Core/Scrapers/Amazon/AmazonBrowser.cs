using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Sherlock.Business.Core.Scrapers.Amazon;

/// <summary>
/// Um Chrome de verdade, ligado o tempo todo, respondendo consultas de ISBN.
///
/// Por que navegador e não HttpClient, como nas livrarias: a Amazon serve uma
/// variante sem preço para quem não parece navegador. Medido em 2026-08-18 na
/// mesma URL e no mesmo minuto — no Chrome vem "R$ 49,74 (-40%)", no HttpClient
/// vem o bloco "adicione este item ao seu carrinho". As respostas sem preço
/// voltam byte a byte idênticas (828.367 B em 0,27s) enquanto a renderização de
/// verdade leva ~2,6s: é resposta enlatada, e cabeçalho nenhum a desvia.
/// Cookies de sessão colhidos de um Chrome real resolveram 1 ASIN em 5.
///
/// O navegador fica de pé entre as buscas porque subir Chrome custa ~1s; com ele
/// quente, cada ISBN sai em ~1s — mais rápido que as nossas próprias livrarias
/// (p50 4,7s), então a Amazon nunca é o gargalo da busca.
/// </summary>
public class AmazonBrowser : IAmazonBrowser, IAsyncDisposable
{
    private const string SearchUrl = "https://www.amazon.com.br/s?k={0}&i=stripbooks";

    /// <summary>Card de resultado da busca. É o mesmo seletor no desktop e no mobile.</summary>
    private const string ResultSelector = "[data-component-type=\"s-search-result\"]";

    /// <summary>
    /// Roda dentro da página: lê o primeiro card e devolve texto cru. Conversão
    /// para número fica no <see cref="AmazonSearchResultParser"/>, que é testável.
    /// </summary>
    private const string Probe = """
        (() => {
          const card = document.querySelector('[data-component-type="s-search-result"]');
          if (!card) return null;
          const texto = (el) => el ? el.textContent.trim() : null;
          // O preço cobrado e o "De:" moram os dois em .a-price; o riscado é o
          // que está dentro de .a-text-price.
          const cobrado = card.querySelector('.a-price:not(.a-text-price) .a-offscreen')
                       || card.querySelector('.a-price .a-offscreen');
          const riscado = card.querySelector('.a-text-price .a-offscreen');
          const titulo = card.querySelector('h2');
          const formato = card.querySelector('a.a-text-bold, .a-size-base.a-link-normal');
          return JSON.stringify({
            asin: card.getAttribute('data-asin'),
            title: titulo ? titulo.innerText.trim() : null,
            price: texto(cobrado),
            listPrice: texto(riscado),
            format: formato ? formato.innerText.trim() : null,
          });
        })()
        """;

    private static readonly string[] BlockedResourceTypes = ["image", "media", "font", "stylesheet"];

    private readonly AmazonSettings _settings;
    private readonly ILogger<AmazonBrowser> _logger;
    private readonly SemaphoreSlim _launchLock = new(1, 1);
    private readonly SemaphoreSlim _pageLimit;

    private IBrowser? _browser;

    public AmazonBrowser(IOptions<AmazonSettings> settings, ILogger<AmazonBrowser> logger)
    {
        _settings = settings?.Value ?? new AmazonSettings();
        _logger = logger;
        _pageLimit = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrentPages));
    }

    public async Task<AmazonOffer?> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(isbn))
            return null;

        var browser = await GetBrowserAsync(cancellationToken);
        if (browser is null)
            return null;

        await _pageLimit.WaitAsync(cancellationToken);
        try
        {
            return await LookupAsync(browser, isbn, cancellationToken);
        }
        finally
        {
            _pageLimit.Release();
        }
    }

    private async Task<AmazonOffer?> LookupAsync(IBrowser browser, string isbn, CancellationToken cancellationToken)
    {
        var timeoutMs = Math.Max(1, _settings.TimeoutSeconds) * 1000;

        // Uma aba por consulta: o Chrome caro é o processo, não a aba, e aba nova
        // não carrega estado da busca anterior.
        await using var page = await browser.NewPageAsync();

        await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
        {
            ["Accept-Language"] = "pt-BR,pt;q=0.9",
        });

        // Imagem, fonte e CSS não têm preço dentro. Cortar reduz o que baixamos
        // da Amazon e o que ela gasta para nos servir.
        await page.SetRequestInterceptionAsync(true);
        page.Request += async (_, e) =>
        {
            try
            {
                if (BlockedResourceTypes.Contains(e.Request.ResourceType.ToString().ToLowerInvariant()))
                    await e.Request.AbortAsync();
                else
                    await e.Request.ContinueAsync();
            }
            catch (PuppeteerException)
            {
                // Requisição já resolvida enquanto decidíamos; nada a fazer.
            }
        };

        var url = string.Format(SearchUrl, Uri.EscapeDataString(isbn));

        await page.GoToAsync(url, new NavigationOptions
        {
            Timeout = timeoutMs,
            WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
        });

        try
        {
            await page.WaitForSelectorAsync(ResultSelector, new WaitForSelectorOptions { Timeout = timeoutMs });
        }
        catch (WaitTaskTimeoutException)
        {
            // Busca sem resultado nenhum: a Amazon não vende esse ISBN.
            _logger.LogDebug("[Amazon] ISBN {Isbn} sem resultado", isbn);
            return null;
        }

        var payload = await page.EvaluateExpressionAsync<string?>(Probe);
        return AmazonSearchResultParser.TryParse(payload);
    }

    /// <summary>
    /// Sobe o navegador na primeira consulta e o mantém. Se ele tiver morrido,
    /// sobe outro — derrubar a busca inteira por causa de um Chrome caído seria
    /// pior que consultar a Amazon de novo.
    /// </summary>
    private async Task<IBrowser?> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsClosed: false })
            return _browser;

        await _launchLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is { IsClosed: false })
                return _browser;

            _browser = await LaunchAsync();
            return _browser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Amazon] Não foi possível subir o navegador; a Amazon fica fora desta busca");
            _browser = null;
            return null;
        }
        finally
        {
            _launchLock.Release();
        }
    }

    private async Task<IBrowser> LaunchAsync()
    {
        var options = new LaunchOptions
        {
            Headless = true,
            ExecutablePath = string.IsNullOrWhiteSpace(_settings.ChromePath) ? null : _settings.ChromePath,
            Args =
            [
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--no-first-run",
                "--lang=pt-BR",
                // Sem isso o Chrome dorme as abas em segundo plano e a consulta
                // seguinte acorda lenta.
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-renderer-backgrounding",
            ],
        };

        _logger.LogInformation("[Amazon] Subindo navegador ({Path})",
            options.ExecutablePath ?? "detecção automática");

        return await Puppeteer.LaunchAsync(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _launchLock.Dispose();
        _pageLimit.Dispose();
        GC.SuppressFinalize(this);
    }
}
