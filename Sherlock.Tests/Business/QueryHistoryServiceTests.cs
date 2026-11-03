using FluentAssertions;
using Moq;
using Sherlock.Business.Services;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Tests.Business;

public class QueryHistoryServiceTests
{
    private static Transaction Transacao(int userId = 7) => new()
    {
        Id = 10,
        UserId = userId,
        ResultTypeId = 2,
        InputParameters = """{"isbn":"9788595075511","isCart":false}""",
        Queries = new List<Query>
        {
            // Amazon: mais barata, mas sem autor e com capa própria
            new() { Id = 1, ProviderId = 94, Success = true, Price = 120m, Title = "Os Graus do Saber",
                    Author = "", ImageUrl = "https://m.media-amazon.com/x.jpg", ProductUrl = "https://amazon/x",
                    Provider = new Provider { Id = 94, Name = "Amazon" } },
            // Cedet: autor e capa do CDN compartilhado
            new() { Id = 2, ProviderId = 1, Success = true, Price = 134.76m, Title = "Os Graus do Saber",
                    Author = "Jacques Maritain", ImageUrl = "https://static.cedet.com.br/p/1.jpg",
                    Provider = new Provider { Id = 1, Name = "Livraria Alexandre Costa" } },
            // Esgotada: não conta como oferta
            new() { Id = 3, ProviderId = 2, Success = true, Price = 99m, Available = false, Title = "Os Graus do Saber",
                    Provider = new Provider { Id = 2, Name = "Loja Esgotada" } },
            // Falhou
            new() { Id = 4, ProviderId = 3, Success = false, ErrorMessage = "timeout",
                    Provider = new Provider { Id = 3, Name = "Loja Fora" } },
        },
    };

    private static QueryHistoryService Servico(Transaction t, out Mock<ITransactionRepository> repo)
    {
        repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { t });
        repo.Setup(r => r.GetWithQueriesAsync(t.Id)).ReturnsAsync(t);
        return new QueryHistoryService(repo.Object, Mock.Of<IQueryRepository>());
    }

    [Fact]
    public async Task Historico_TrazCapaDaCedetEAutorDeOutraLoja_QuandoAMelhorNaoTem()
    {
        var service = Servico(Transacao(), out _);

        var item = (await service.GetUserHistoryAsync(7)).Single();

        item.BestPrice.Should().Be(120m);
        item.BestProvider.Should().Be("Amazon");
        item.BestProductUrl.Should().Be("https://amazon/x");
        item.BestAuthor.Should().Be("Jacques Maritain", "a Amazon não trouxe autor");
        item.ImageUrl.Should().Be("https://static.cedet.com.br/p/1.jpg", "a capa da Cedet é a mesma em todas as lojas");
        item.OffersCount.Should().Be(2, "a esgotada e a que falhou não contam");
    }

    [Fact]
    public async Task Detalhe_OrdenaOfertasDepoisEsgotadasDepoisFalhas_EExpoeLinkDaLoja()
    {
        var service = Servico(Transacao(), out _);

        var detail = await service.GetTransactionDetailAsync(10, ownerUserId: 7);

        detail.Should().NotBeNull();
        detail!.Queries.Select(q => q.Id).Should().ContainInOrder(1, 2, 3, 4);
        detail.Queries[0].ProductUrl.Should().Be("https://amazon/x");
        detail.Queries[2].Available.Should().BeFalse();
        detail.OffersCount.Should().Be(2);
    }

    [Fact]
    public async Task Detalhe_DeOutraPessoa_VoltaNull()
    {
        var service = Servico(Transacao(userId: 7), out _);

        (await service.GetTransactionDetailAsync(10, ownerUserId: 8)).Should().BeNull();
        (await service.GetTransactionDetailAsync(10)).Should().NotBeNull("sem dono informado não há conferência");
    }

    [Fact]
    public async Task Historico_SemNenhumaOferta_NaoInventaMelhorResultado()
    {
        var t = Transacao();
        foreach (var q in t.Queries) q.Success = false;
        var service = Servico(t, out _);

        var item = (await service.GetUserHistoryAsync(7)).Single();

        item.BestPrice.Should().BeNull();
        item.BestProvider.Should().BeNull();
        item.OffersCount.Should().Be(0);
        item.BestTitle.Should().Be("Os Graus do Saber", "o título de qualquer loja ainda identifica o livro");
        item.ImageUrl.Should().Be("https://static.cedet.com.br/p/1.jpg", "a capa continua útil para identificar o livro");
    }
}
