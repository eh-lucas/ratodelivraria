namespace Sherlock.Business.Configuration;

/// <summary>
/// Ajustes da busca de preços.
/// </summary>
public class SearchSettings
{
    public const string SectionName = "Search";

    /// <summary>
    /// Quantas lojas são consultadas ao mesmo tempo.
    ///
    /// O tempo total de uma busca é (nº de lojas ÷ este valor) × tempo médio por loja.
    /// Com 67 lojas a ~18s cada, o valor 10 obriga 7 rodadas — mais de dois minutos de
    /// espera para o usuário, sem que nenhuma loja esteja lenta.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 10;
}
