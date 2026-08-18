using FluentAssertions;
using Sherlock.Business.Core.Scrapers.Cedet.HttpClient;

namespace Sherlock.Tests.Business;

/// <summary>
/// O caminho HTML só entra quando a loja não fala o protocolo JSON. Foi lá que
/// nasceu o livro fantasma: uma loja cuja página de busca ignora o termo e
/// devolve a vitrine, com o parser pegando o primeiro produto.
/// </summary>
public class CedetHtmlFallbackTests
{
    [Theory]
    // Busca por ISBN casa com um produto; dois ou três quando há kits.
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void PareceResultadoDeBusca_AceitaPoucosProdutos(int produtos)
    {
        CedetSingleSearchHttpClient.PareceResultadoDeBusca(produtos).Should().BeTrue();
    }

    [Theory]
    // Nada encontrado não é resultado de busca válido para pescar candidato.
    [InlineData(0)]
    // 36 é o número medido na Livraria da Marcela em 2026-08-18: a mesma página
    // de vitrine vinha para qualquer termo, inclusive 0000000000000.
    [InlineData(36)]
    [InlineData(6)]
    [InlineData(200)]
    public void PareceResultadoDeBusca_RecusaVitrine(int produtos)
    {
        CedetSingleSearchHttpClient.PareceResultadoDeBusca(produtos).Should().BeFalse();
    }
}
