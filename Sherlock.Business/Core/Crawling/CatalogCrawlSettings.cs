namespace Sherlock.Business.Core.Crawling;

/// <summary>
/// Limites do crawler. Os padrões são deliberadamente conservadores: a carga que
/// geramos nas lojas deve ser menor que a de um punhado de visitantes normais.
/// </summary>
public class CatalogCrawlSettings
{
    public const string SectionName = "CatalogCrawl";

    /// <summary>
    /// Lojas varridas ao mesmo tempo.
    ///
    /// O limite que realmente protege é o semáforo por IP no crawler: 62 das 93 lojas
    /// ficam no mesmo servidor, então elas se enfileiram entre si mesmo com este valor
    /// alto. Aqui só limitamos quantas frentes abrimos ao todo.
    /// </summary>
    public int MaxParallelProviders { get; set; } = 4;

    /// <summary>
    /// Pausa entre requisições ao mesmo servidor.
    ///
    /// Com ~15s de resposta, 5s de pausa deixam a taxa em cerca de 3 requisições por
    /// minuto por servidor — bem abaixo do que um punhado de visitantes normais gera.
    /// </summary>
    public int DelayBetweenPagesMs { get; set; } = 3000;

    /// <summary>
    /// Produtos por página.
    ///
    /// Medido em produção:
    ///  - 200: mesma latência de 500 (o custo é fixo por requisição) e 2,5x mais páginas;
    ///  - 1000: as lojas devolvem 504 a partir da página ~8, porque o OFFSET fica alto demais;
    ///  - 500: varredura completa sem um único erro. É o valor comprovado.
    /// </summary>
    public int PageSize { get; set; } = 500;

    /// <summary>Erros seguidos que fazem desistir da loja.</summary>
    public int MaxConsecutiveErrors { get; set; } = 3;

    /// <summary>Teto de páginas por loja — trava contra paginação infinita.</summary>
    public int MaxPagesPerProvider { get; set; } = 200;

    public int RequestTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Páginas seguidas sem nenhum produto novo antes de encerrar a loja.
    ///
    /// Com a listagem ordenada da mais recente para a mais antiga, os produtos que ainda
    /// não conhecemos ficam no começo. Ao emendar algumas páginas só com conhecidos,
    /// o resto do catálogo é redundante e não vale as requisições.
    /// </summary>
    public int StopAfterKnownPages { get; set; } = 3;

    /// <summary>
    /// Lojas varridas há menos de tantos dias são puladas. Evita refazer o catálogo
    /// inteiro num refresh semanal — passe <c>force</c> para ignorar.
    /// </summary>
    public int SkipIfCrawledWithinDays { get; set; } = 6;
}
