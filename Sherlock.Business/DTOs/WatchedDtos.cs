using System.ComponentModel.DataAnnotations;
using Sherlock.Domain.Entities;

namespace Sherlock.Business.DTOs;

/// <summary>Um livro observado, do jeito que o cartão da tela precisa dele.</summary>
public class WatchedBookDto
{
    public int Id { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ImageUrl { get; set; }

    public WatchRule Rule { get; set; }
    public decimal? TargetPrice { get; set; }
    public int DropPercent { get; set; }

    public decimal? ReferencePrice { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal? PreviousPrice { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public string? LastProviderName { get; set; }
    public string? LastProductUrl { get; set; }

    /// <summary>
    /// Menor preço dos últimos 30 dias, lido de `queries`. Vai junto da lista
    /// porque é metade da informação do cartão: sem ele, "−R$12 desde ontem" não
    /// diz se o preço de hoje é bom ou só menos ruim que o de ontem (§3.3.1).
    /// </summary>
    public decimal? LowestPrice30d { get; set; }

    public DateTime? LastNotifiedAt { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Um item do lote que a tela manda ao clicar em OK.</summary>
public class WatchedBookInput
{
    // Só o teto de tamanho fica no atributo. O formato é conferido no serviço de
    // propósito: um `MinimumLength` aqui faria o ModelState recusar o LOTE
    // INTEIRO com 400 por causa de um ISBN torto, e a pessoa perderia os outros
    // nove livros sem saber qual deu problema.
    [Required]
    [StringLength(20)]
    public string Isbn { get; set; } = string.Empty;

    /// <summary>
    /// Opcional. A tela manda o que ela já mostrava no cartão; faltando, o serviço
    /// procura no banco — e se nem lá houver, cai no próprio ISBN, para o cartão
    /// nunca aparecer sem rótulo.
    /// </summary>
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ImageUrl { get; set; }

    public WatchRule Rule { get; set; } = WatchRule.PercentDrop;

    [Range(0.01, 100000)]
    public decimal? TargetPrice { get; set; }

    [Range(1, 90)]
    public int DropPercent { get; set; } = 15;
}

public class WatchedBookBatchRequest
{
    [Required]
    [MinLength(1)]
    public List<WatchedBookInput> Items { get; set; } = new();
}

/// <summary>Edição de um livro já observado. Campo nulo é campo que não muda.</summary>
public class WatchedBookPatchRequest
{
    public WatchRule? Rule { get; set; }

    [Range(0.01, 100000)]
    public decimal? TargetPrice { get; set; }

    [Range(1, 90)]
    public int? DropPercent { get; set; }

    public bool? Active { get; set; }
}

public class WatchedBookBatchResult
{
    public List<WatchedBookDto> Items { get; set; } = new();

    /// <summary>
    /// ISBNs recusados e o porquê — teto atingido, ISBN inválido. O lote não é
    /// tudo-ou-nada: recusar os 5 livros porque o 6º estourou o teto obrigaria a
    /// pessoa a adivinhar qual foi o problema.
    /// </summary>
    public List<WatchedBookRejection> Rejected { get; set; } = new();
}

public class WatchedBookRejection
{
    public string Isbn { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
