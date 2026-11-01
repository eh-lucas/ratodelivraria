namespace Sherlock.Domain.Entities;

/// <summary>
/// Título, autor e capa de um livro, reaproveitados do que já está no banco.
///
/// A ordem de busca é a de docs/PLANO-OBSERVADOS.md §3.2, da mais barata para a
/// mais cara: `queries` (livro já consultado alguma vez tem capa guardada),
/// depois o catálogo (espelho local, sem capa), e só então a página do produto.
/// Nenhuma das duas primeiras custa requisição.
/// </summary>
public record BookMetadata(string? Title, string? Author, string? ImageUrl)
{
    public bool Completo => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(ImageUrl);
}
