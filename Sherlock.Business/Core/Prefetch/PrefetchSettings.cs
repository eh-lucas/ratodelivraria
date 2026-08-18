namespace Sherlock.Business.Core.Prefetch;

/// <summary>
/// Configuração do aquecimento dos livros mais procurados.
/// </summary>
public class PrefetchSettings
{
    public const string SectionName = "Prefetch";

    /// <summary>Desligado, nenhuma requisição sai daqui.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Quantos livros do ranking manter quentes. Cada um custa uma varredura das
    /// 68 lojas, então o número é carga no servidor delas — não é de graça.
    /// </summary>
    public int TopBooks { get; set; } = 10;

    /// <summary>
    /// De quanto em quanto tempo revisitar a lista. Tem que ser menor que o
    /// QueryCache:DefaultCacheTimeMinutes (60), senão o livro esfria antes da
    /// próxima passada e o aquecimento não serve para nada.
    /// </summary>
    public int IntervalMinutes { get; set; } = 50;

    /// <summary>
    /// Espera entre um livro e o outro. Aquecer 10 livros de enfiada seria pôr
    /// 680 requisições na fila dos 2 IPs que as livrarias compartilham;
    /// espaçados, o trabalho se dilui entre as buscas de gente de verdade.
    /// </summary>
    public int DelayBetweenBooksSeconds { get; set; } = 60;

    /// <summary>
    /// Espera antes da primeira rodada, para não competir com o startup da API.
    /// </summary>
    public int StartupDelaySeconds { get; set; } = 120;
}
