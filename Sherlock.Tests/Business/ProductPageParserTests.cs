using FluentAssertions;
using Sherlock.Business.Services;

namespace Sherlock.Tests.Business;

/// <summary>
/// A página do produto é aberta uma vez, para achar o ISBN, e de quebra traz capa,
/// autor e editora (docs/PLANO-OBSERVADOS.md §3.2). Esses três não têm tela que
/// grite quando param de vir: a capa some e o cartão fica com o placeholder, o que
/// parece decisão de design e não defeito. Daí os testes.
/// </summary>
public class ProductPageParserTests
{
    [Fact]
    public void Capa_sai_da_og_image()
    {
        const string html = """
            <meta property="og:image" content="https://loja.com.br/image/cache/livro-500x500.jpg" />
            """;

        CatalogService.ExtrairCapa(html, "https://loja.com.br/produto/x")
            .Should().Be("https://loja.com.br/image/cache/livro-500x500.jpg");
    }

    [Fact]
    public void Capa_aceita_a_ordem_invertida_dos_atributos()
    {
        // Metade das lojas escreve content antes de property. Um padrão só com a
        // ordem "property, content" perderia essas sem erro nenhum.
        const string html = """
            <meta content="https://loja.com.br/capa.jpg" property="og:image">
            """;

        CatalogService.ExtrairCapa(html, null).Should().Be("https://loja.com.br/capa.jpg");
    }

    [Fact]
    public void Capa_relativa_vira_absoluta_pela_pagina()
    {
        const string html = """<meta property="og:image" content="/image/capa.jpg">""";

        CatalogService.ExtrairCapa(html, "https://loja.com.br/index.php?route=product/product&id=9")
            .Should().Be("https://loja.com.br/image/capa.jpg");
    }

    [Fact]
    public void Sem_og_image_nao_inventa_capa()
    {
        CatalogService.ExtrairCapa("<html><body>sem meta</body></html>", "https://loja.com.br")
            .Should().BeNull();
    }

    [Fact]
    public void Autor_e_editora_saem_da_ficha()
    {
        const string html = """
            <ul class="list-unstyled">
              <li><span>Autor:</span> Jos&eacute; de Alencar</li>
              <li><span>Editora:</span> Martin Claret</li>
            </ul>
            """;

        CatalogService.ExtrairAutor(html).Should().Be("José de Alencar");
        CatalogService.ExtrairEditora(html).Should().Be("Martin Claret");
    }

    [Fact]
    public void Rotulo_sem_valor_nao_vira_autor_em_branco()
    {
        // "Autor:" seguido de nada devolveria uma string de espaços, e um cartão
        // com autor vazio é pior que um cartão sem autor: o espaço fica lá.
        const string html = "<li><span>Autor:</span>   \n   </li>";

        CatalogService.ExtrairAutor(html).Should().BeNull();
    }
}
