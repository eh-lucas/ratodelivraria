namespace Sherlock.Business.Core.Scrapers.Amazon;

/// <summary>
/// Configuração do navegador usado para consultar a Amazon.
/// </summary>
public class AmazonSettings
{
    public const string SectionName = "Amazon";

    /// <summary>
    /// Liga ou desliga a consulta à Amazon. Desligado, o provider responde
    /// "sem resultado" sem subir navegador nenhum.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Caminho do Chrome/Chromium. Vazio deixa o PuppeteerSharp procurar sozinho.
    /// No container é /usr/bin/chromium.
    /// </summary>
    public string? ChromePath { get; set; }

    /// <summary>
    /// Quantas abas atendem em paralelo. A Amazon responde em ~1s e cada busca
    /// consulta um livro só, então 2 já cobre usuários simultâneos sem virar
    /// um sinal de robô.
    /// </summary>
    public int MaxConcurrentPages { get; set; } = 2;

    /// <summary>
    /// Teto por consulta. Medido em ~1s com a aba quente; 15s é folga para a
    /// primeira consulta depois de subir o navegador.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}
