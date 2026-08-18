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

    /// <summary>
    /// Padrão do teto global, quando não configurado. É o mesmo 20 medido como
    /// joelho da curva: abaixo disso a busca demora à toa, acima o servidor das
    /// livrarias só fica mais lento sem entregar mais.
    /// </summary>
    public const int PadraoGlobal = 20;

    /// <summary>
    /// Teto de requisições simultâneas às livrarias somando TODAS as buscas.
    ///
    /// O <see cref="MaxDegreeOfParallelism"/> limita uma busca; este limita o
    /// site inteiro. Sem ele, cinco pessoas buscando ao mesmo tempo abrem 100
    /// conexões nos 2 IPs que as 67 lojas dividem.
    /// </summary>
    public int MaxGlobalParallelism { get; set; } = PadraoGlobal;
}
