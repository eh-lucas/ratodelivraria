namespace Sherlock.Business.DTOs;

/// <summary>Sugestão de título para o autocomplete da busca por nome.</summary>
public class CatalogSuggestionDto
{
    /// <summary>Item de catálogo que representa esta sugestão.</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Só vem preenchido se o ISBN já tiver sido resolvido antes.</summary>
    public string? Isbn { get; set; }
}

public class ResolveIsbnResultDto
{
    public bool Found { get; set; }
    public string? Isbn { get; set; }
    public string? Title { get; set; }
    public string? Error { get; set; }
}
