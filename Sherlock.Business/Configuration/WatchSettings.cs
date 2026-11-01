namespace Sherlock.Business.Configuration;

/// <summary>
/// Configuração do vigia de preço — docs/PLANO-OBSERVADOS.md §3.4 e §4.4.
/// </summary>
public class WatchSettings
{
    public const string SectionName = "Watch";

    /// <summary>
    /// Teto de livros observados por pessoa. É a variável que dimensiona o
    /// trabalho noturno inteiro, e o único controle que existe — observar é de
    /// graça, como buscar (§3.4).
    ///
    /// Subiu de 20 para 30 em 01/11/2026. O custo está no pior caso da
    /// varredura: a janela de 5h no modo enxuto comporta ~1.800 ISBNs distintos
    /// por noite, então sem nenhuma sobreposição entre pessoas isto cai de 90
    /// para 60 usuários. Na prática a sobreposição é alta — o livro que uma
    /// pessoa observa, outra também observa, e o custo marginal dele é zero
    /// (§4.1) — e por isso o teto de usuários real fica bem acima disso.
    /// </summary>
    public int MaxPerUser { get; set; } = 30;

    /// <summary>Janela do "menor preço do mês", em dias.</summary>
    public int LowestPriceWindowDays { get; set; } = 30;

    /// <summary>
    /// Silêncio depois de avisar sobre um livro, em dias. Quebrado só se o preço
    /// cair mais <see cref="RenotifyDropPercent"/>% abaixo do já avisado.
    /// </summary>
    public int QuietDays { get; set; } = 7;

    /// <summary>Queda adicional que fura o silêncio.</summary>
    public int RenotifyDropPercent { get; set; } = 10;

    // ---- varredura (§4.3, §4.4) ----

    /// <summary>Desligado, nenhuma requisição sai do vigia.</summary>
    public bool SweepEnabled { get; set; } = false;

    /// <summary>
    /// Hora UTC em que a varredura começa. 06h UTC = 03h em Brasília.
    ///
    /// UTC explícito, e não `TimeZoneInfo` em tempo de execução: o servidor roda
    /// em UTC e este repositório já se queimou nisso uma vez (commit 406c453,
    /// "fix(ranking): marco de reset em UTC"). O Brasil não tem horário de verão
    /// desde 2019, então −03:00 é constante e a conversão cabe neste comentário.
    /// </summary>
    public int SweepHourUtc { get; set; } = 6;

    /// <summary>
    /// Até que hora UTC a varredura pode seguir. Passou disso, para e deixa o
    /// resto para amanhã: a janela existe para as lojas levarem o trabalho
    /// enquanto estão ociosas, não para a rodada terminar a qualquer custo.
    /// </summary>
    public int SweepUntilHourUtc { get; set; } = 11;

    /// <summary>Espera entre um livro e o outro, como no aquecedor.</summary>
    public int DelayBetweenBooksSeconds { get; set; } = 20;

    /// <summary>
    /// Vagas do portão global que o vigia pode ocupar. O teto do portão é 20; com
    /// 5 aqui, busca de gente acordada sempre encontra vaga (§4.4).
    /// </summary>
    public int MaxConcurrentBooks { get; set; } = 5;

    /// <summary>
    /// De quantos em quantos dias cada livro leva uma varredura completa. Nos
    /// outros dias consulta só as lojas que já devolveram aquele livro — é o
    /// "modo enxuto" que leva a rodada de ~270 para ~1.800 ISBNs (§4.2).
    /// </summary>
    public int FullSweepEveryDays { get; set; } = 7;

    // ---- aviso (§4.3) ----

    /// <summary>Desligado, nenhum e-mail sai do vigia.</summary>
    public bool NotifyEnabled { get; set; } = false;

    /// <summary>
    /// Hora UTC do e-mail. 12h UTC = 09h em Brasília, que é o que a pessoa
    /// percebe — a varredura já terminou há uma hora.
    /// </summary>
    public int NotifyHourUtc { get; set; } = 12;

    /// <summary>
    /// Endereço do site, para os links do e-mail. Sem isto o resumo diário manda
    /// a pessoa para lugar nenhum.
    /// </summary>
    public string SiteUrl { get; set; } = "https://ratodelivraria.com.br";
}
