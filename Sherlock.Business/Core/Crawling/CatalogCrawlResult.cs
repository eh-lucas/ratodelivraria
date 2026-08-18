namespace Sherlock.Business.Core.Crawling;

public class CatalogCrawlResult
{
    public int ProvidersAttempted { get; set; }
    public int ProvidersSucceeded { get; set; }

    /// <summary>Lojas ignoradas por terem sido varridas há pouco tempo.</summary>
    public int ProvidersSkipped { get; set; }
    public int ItemsCollected { get; set; }
    public int ItemsSaved { get; set; }
    public long ElapsedMs { get; set; }
    public List<ProviderCrawlReport> Providers { get; set; } = new();
}

public class ProviderCrawlReport
{
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>Páginas que a loja declara ter para o tamanho de página pedido.</summary>
    public int ReportedPages { get; set; }
    public int ItemsCollected { get; set; }
    public int PagesFetched { get; set; }
    public long ElapsedMs { get; set; }
    /// <summary>Produtos que ainda não existiam no catálogo local.</summary>
    public int NewProducts { get; set; }

    /// <summary>Encerrou antes do fim por não achar mais novidade.</summary>
    public bool StoppedEarly { get; set; }

    public bool Success { get; set; }
    public string? Error { get; set; }
}
