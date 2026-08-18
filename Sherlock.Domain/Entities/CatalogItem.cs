namespace Sherlock.Domain.Entities;

/// <summary>
/// Espelho bruto de um produto no catálogo de uma loja, coletado pelo crawler.
///
/// Serve para sugerir títulos quando o usuário busca por nome. O preço aqui é do
/// momento da coleta e envelhece — o preço mostrado ao usuário continua vindo da
/// busca ao vivo, nunca desta tabela.
/// </summary>
public class CatalogItem
{
    public int Id { get; set; }

    /// <summary>Loja de onde o item veio.</summary>
    public int ProviderId { get; set; }

    /// <summary>Identificador do produto dentro da loja (product_id do OpenCart).</summary>
    public string ProductId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Título normalizado (minúsculo, sem acento) usado na busca por nome.</summary>
    public string NameNormalized { get; set; } = string.Empty;

    /// <summary>Autores concatenados, como a loja devolve.</summary>
    public string? Authors { get; set; }

    /// <summary>Preço no momento da coleta. Referência interna, não exibir como preço atual.</summary>
    public decimal? Price { get; set; }

    /// <summary>URL da página do produto — usada para resolver o ISBN sob demanda.</summary>
    public string? Href { get; set; }

    /// <summary>ISBN, preenchido sob demanda ao abrir a página do produto.</summary>
    public string? Isbn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
