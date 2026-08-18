namespace Sherlock.Business.Configuration;

/// <summary>
/// Ajustes do ranking de livros mais consultados.
/// </summary>
public class RankingSettings
{
    public const string SectionName = "Ranking";

    /// <summary>
    /// Marco de reset: só contam buscas a partir desta data.
    ///
    /// Existe porque o ranking acumula desde sempre, e um punhado de buscas de
    /// teste feitas num dia ficaria no topo para todo o sempre. Com o marco, o
    /// que veio antes dele entra valendo 1 — os livros continuam na vitrine,
    /// mas quem manda na ordem é o uso de verdade daqui para frente.
    ///
    /// Vazio desliga o marco: a contagem volta a ser o total histórico.
    /// </summary>
    public DateTime? ResetAt { get; set; }
}
