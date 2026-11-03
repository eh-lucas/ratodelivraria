namespace Sherlock.Business.DTOs;

/// <summary>Sugestão de título para o autocomplete da busca por nome.</summary>
public class CatalogSuggestionDto
{
    /// <summary>Item de catálogo que representa esta sugestão.</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Autores como a loja devolve (já coletados pelo crawler).</summary>
    public string? Author { get; set; }

    /// <summary>
    /// Miniatura 150x226 no CDN da loja. O navegador baixa direto de lá; a API só
    /// manda o endereço — o que a coleta gravou ou, para itens antigos, o derivado
    /// do product_id.
    /// </summary>
    public string? ThumbUrl { get; set; }

    /// <summary>Só vem preenchido se o ISBN já tiver sido resolvido antes.</summary>
    public string? Isbn { get; set; }
}

public class ResolveIsbnResultDto
{
    public bool Found { get; set; }
    public string? Isbn { get; set; }
    public string? Title { get; set; }
    public string? Error { get; set; }

    // A página do produto já está aberta para achar o ISBN. Ler capa, autor e
    // editora na mesma passada não custa requisição nenhuma — e é de onde sai a
    // capa do cartão de observado quando o livro é novo para o banco
    // (docs/PLANO-OBSERVADOS.md §3.2).
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>
/// O que a tela precisa para mostrar um livro pelo ISBN sem expor o número:
/// título, autor e capa pequena. Vem do catálogo ou, se ele não conhece o ISBN,
/// da última consulta bem-sucedida.
/// </summary>
public class BookLookupDto
{
    public string Isbn { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? ThumbUrl { get; set; }
}
