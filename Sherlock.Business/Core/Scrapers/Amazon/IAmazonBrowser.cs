namespace Sherlock.Business.Core.Scrapers.Amazon;

/// <summary>
/// Consulta a Amazon por ISBN usando um navegador de verdade.
/// </summary>
public interface IAmazonBrowser
{
    /// <summary>
    /// Devolve a oferta do primeiro resultado, ou null quando a Amazon não tem
    /// o livro (ou não está disponível para consulta agora).
    /// </summary>
    Task<AmazonOffer?> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}
