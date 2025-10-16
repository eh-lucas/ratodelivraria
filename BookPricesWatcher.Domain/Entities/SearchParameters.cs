namespace Sherlock.Domain.Entities;

/// <summary>
/// Dados necessários para realizar uma busca
/// </summary>
public abstract class SearchParameters
{
    public string Token { get; set; }
    public Source Source { get; set; }
}
