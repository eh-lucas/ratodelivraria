namespace Sherlock.Domain.Entities;

/// <summary>
/// Em relação a quê a queda é medida. Ver docs/PLANO-OBSERVADOS.md §3.3.
/// </summary>
public enum WatchRule
{
    /// <summary>Avisa quando o preço chegar ao valor que a pessoa escolheu.</summary>
    TargetPrice = 0,

    /// <summary>
    /// Avisa em queda considerável. Não é "caiu X% desde ontem" — é isso com um
    /// piso: ou hoje é o melhor preço dos últimos 30 dias com folga, ou caiu forte
    /// de ontem para hoje E não está acima do melhor do mês. O guarda-corpo existe
    /// porque 98,8% dos produtos dessas lojas vivem em "promoção", então queda
    /// diária sozinha avisaria sobre preço que não é oportunidade nenhuma.
    /// </summary>
    PercentDrop = 1,
}

/// <summary>
/// Um livro que alguém pediu para o rato vigiar.
///
/// Não existe entidade "lista": a lista de uma pessoa são as linhas dela com
/// <see cref="Active"/> — um agregado em volta disso seria uma tabela a mais sem
/// nada a ganhar.
///
/// O histórico de preço também não mora aqui. A varredura noturna grava
/// `Transaction` + `Query` como qualquer busca, e a série sai de `queries`; o que
/// esta tabela guarda são os dois últimos pontos, que são os que a tela desenha.
/// </summary>
public class WatchedBook
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Isbn { get; set; } = string.Empty;

    // ---- congelados na adição ----
    // O cartão não pode depender de a loja continuar com o produto no ar: se
    // amanhã o anúncio sai, a pessoa ainda precisa reconhecer o que ela pediu
    // para observar.

    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ImageUrl { get; set; }

    // ---- a regra ----

    public WatchRule Rule { get; set; } = WatchRule.PercentDrop;

    /// <summary>Preenchido quando <see cref="Rule"/> é <c>TargetPrice</c>.</summary>
    public decimal? TargetPrice { get; set; }

    /// <summary>Usado quando <see cref="Rule"/> é <c>PercentDrop</c>.</summary>
    public int DropPercent { get; set; } = 15;

    // ---- o que a varredura escreve ----

    /// <summary>Menor preço no momento em que o livro entrou na lista.</summary>
    public decimal? ReferencePrice { get; set; }
    public DateTime? ReferencePriceAt { get; set; }

    /// <summary>Menor preço na varredura mais recente.</summary>
    public decimal? LastPrice { get; set; }

    /// <summary>
    /// O preço da varredura anterior. Existe só para a tela poder desenhar
    /// "−R$12 desde ontem" sem ir consultar `queries` a cada cartão (§3.3.1).
    /// </summary>
    public decimal? PreviousPrice { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    /// <summary>Loja onde o <see cref="LastPrice"/> foi encontrado.</summary>
    public string? LastProviderName { get; set; }
    public string? LastProductUrl { get; set; }

    // ---- o que o aviso escreve ----

    /// <summary>
    /// Preço que já foi avisado. É contra ele que se mede o "caiu mais 10%" que
    /// quebra o silêncio de 7 dias.
    /// </summary>
    public decimal? LastNotifiedPrice { get; set; }

    /// <summary>Base do silêncio de 7 dias por livro (§3.3.1).</summary>
    public DateTime? LastNotifiedAt { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
