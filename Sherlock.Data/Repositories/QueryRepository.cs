using Microsoft.EntityFrameworkCore;
using Sherlock.Data.Context;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Data.Repositories;

public class QueryRepository : RepositoryBase<Query>, IQueryRepository
{
    private readonly IDbContextFactory<SherlockDbContext> _contextFactory;

    public QueryRepository(
        SherlockDbContext context,
        IDbContextFactory<SherlockDbContext> contextFactory) : base(context)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<Query>> GetByTransactionIdAsync(int transactionId)
    {
        return await _dbSet
            .Where(q => q.TransactionId == transactionId)
            .Include(q => q.Provider)
            .OrderBy(q => q.Price)
            .ToListAsync();
    }

    public async Task<IEnumerable<Query>> GetByProviderIdAsync(int providerId, int limit = 100)
    {
        return await _dbSet
            .Where(q => q.ProviderId == providerId)
            .OrderByDescending(q => q.QueriedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Busca a melhor query de uma transação usando DbContext separado.
    /// Este método é thread-safe.
    /// </summary>
    public async Task<Query?> GetBestQueryForTransactionAsync(int transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Query>()
            .Where(q => q.TransactionId == transactionId && q.Success && q.Price > 0)
            .OrderBy(q => q.Price)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Ranking dos mais consultados. Conta transações distintas, e não linhas de
    /// query: uma busca só bate em 68 lojas e viraria 68 pontos se contássemos
    /// linhas.
    ///
    /// Com marco de reset configurado, o que veio antes dele entra valendo 1 e
    /// só as buscas posteriores somam. Sem o marco, é o total histórico.
    ///
    /// O título sai do catálogo local quando existe. O que a loja devolve na
    /// busca por ISBN nem sempre é o livro certo — o scraper aceita o primeiro
    /// resultado —, e num ranking de dez linhas esse erro fica na cara.
    /// </summary>
    public async Task<IReadOnlyList<PopularBook>> GetMostSearchedAsync(
        int limit, DateTime? marco = null, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // As buscas do aquecedor de cache ficam de fora: se contassem, ele votaria
        // nos proprios livros a cada rodada e o ranking congelaria.
        var transacoesDeGente = context.Set<Transaction>()
            .Where(t => t.InputParameters == null
                        || !EF.Functions.JsonContains(t.InputParameters, "{\"isPrefetch\": true}"));

        var contamPontos = marco.HasValue
            ? transacoesDeGente.Where(t => t.StartedAt >= marco.Value).Select(t => t.Id)
            : transacoesDeGente.Select(t => t.Id);

        // Universo de ISBNs conhecidos. Limitado porque a ordenacao final depende
        // da contagem pos-marco, que so da para calcular depois — mas nao faz
        // sentido carregar historico infinito para montar dez linhas.
        var conhecidos = await context.Set<Query>()
            .Where(q => q.SearchIsbn != null && q.SearchIsbn != "")
            .GroupBy(q => q.SearchIsbn!)
            .Select(g => new
            {
                Isbn = g.Key,
                LastSearchedAt = g.Max(q => q.QueriedAt),
                Recentes = g.Where(q => contamPontos.Contains(q.TransactionId))
                    .Select(q => q.TransactionId).Distinct().Count(),
                // Título de recurso, para ISBN que ainda não está no catálogo.
                FallbackTitle = g.Where(q => q.Success && q.Title != null)
                    .OrderByDescending(q => q.QueriedAt)
                    .Select(q => q.Title)
                    .FirstOrDefault(),
            })
            .OrderByDescending(x => x.LastSearchedAt)
            .Take(UniversoMaximoDeIsbns)
            .ToListAsync(cancellationToken);

        // O ponto de partida: com marco, todo livro conhecido vale 1 e as buscas
        // novas somam em cima. Sem marco, vale o total historico puro.
        var escolhidos = conhecidos
            .Select(x => new { x.Isbn, x.LastSearchedAt, x.FallbackTitle,
                               Searches = marco.HasValue ? x.Recentes + 1 : x.Recentes })
            .OrderByDescending(x => x.Searches)
            .ThenByDescending(x => x.LastSearchedAt)
            .Take(limit)
            .ToList();

        var isbns = escolhidos.Select(x => x.Isbn).ToList();

        var doCatalogo = await context.Set<CatalogItem>()
            .Where(c => c.Isbn != null && isbns.Contains(c.Isbn))
            .GroupBy(c => c.Isbn!)
            .Select(g => new { Isbn = g.Key, Name = g.Min(c => c.Name) })
            .ToDictionaryAsync(x => x.Isbn, x => x.Name, cancellationToken);

        var menorPreco = await MenoresPrecosConfiaveisAsync(context, isbns, cancellationToken);

        return escolhidos
            .Select(x => new PopularBook
            {
                Isbn = x.Isbn,
                Searches = x.Searches,
                Title = doCatalogo.TryGetValue(x.Isbn, out var nome) ? nome : (x.FallbackTitle ?? x.Isbn),
                LowestPrice = menorPreco.TryGetValue(x.Isbn, out var preco) ? preco : null,
                LastSearchedAt = x.LastSearchedAt,
            })
            .ToList();
    }

    /// <summary>
    /// Teto de ISBNs carregados para montar o ranking. A ordenacao final depende
    /// da contagem pos-marco, entao nao da para cortar no banco; este numero
    /// impede que a consulta cresca junto com o historico para sempre.
    /// </summary>
    private const int UniversoMaximoDeIsbns = 500;

    /// <summary>
    /// Quantos ISBNs diferentes um mesmo par (loja, título) pode responder antes
    /// de ser considerado busca quebrada. Um título pertence a um livro, não a
    /// quatro.
    /// </summary>
    private const int IsbnsDistintosParaSuspeita = 3;

    /// <summary>
    /// Menor preço por ISBN, ignorando as lojas cuja busca ignora o termo.
    ///
    /// Existe loja que devolve sempre o mesmo produto, qualquer que seja o ISBN
    /// pedido — e como o scraper usa o primeiro resultado, esse produto entra no
    /// banco preso a ISBNs que não são dele. Medido em 2026-08-18: a regra
    /// marcou uma loja e nenhuma outra.
    ///
    /// Sem esse filtro o preço errado apareceria como "menor visto" na home, que
    /// é o lugar mais visível do site.
    /// </summary>
    private static async Task<Dictionary<string, decimal>> MenoresPrecosConfiaveisAsync(
        SherlockDbContext context, List<string> isbns, CancellationToken cancellationToken)
    {
        var suspeitos = await context.Set<Query>()
            .Where(q => q.SearchIsbn != null && q.Title != null && q.Success)
            .GroupBy(q => new { q.ProviderId, q.Title })
            .Where(g => g.Select(q => q.SearchIsbn).Distinct().Count() >= IsbnsDistintosParaSuspeita)
            .Select(g => new { g.Key.ProviderId, g.Key.Title })
            .ToListAsync(cancellationToken);

        var pares = suspeitos
            .Select(x => $"{x.ProviderId}|{x.Title}")
            .ToHashSet();

        var precos = await context.Set<Query>()
            .Where(q => q.SearchIsbn != null && isbns.Contains(q.SearchIsbn!)
                        && q.Success && q.Price > 0)
            .Select(q => new { Isbn = q.SearchIsbn!, q.ProviderId, q.Title, Price = q.Price!.Value })
            .ToListAsync(cancellationToken);

        return precos
            .Where(x => !pares.Contains($"{x.ProviderId}|{x.Title}"))
            .GroupBy(x => x.Isbn)
            .ToDictionary(g => g.Key, g => g.Min(x => x.Price));
    }

    public async Task<Query> AddQueryAsync(Query query)
    {
        query.QueriedAt = DateTime.UtcNow;
        return await AddAsync(query);
    }

    /// <summary>
    /// Adiciona múltiplas queries usando DbContext separado para permitir operações concorrentes.
    /// Este método é thread-safe.
    /// </summary>
    public async Task AddQueriesAsync(IEnumerable<Query> queries)
    {
        var now = DateTime.UtcNow;
        var queryList = queries.ToList();
        foreach (var query in queryList)
        {
            query.QueriedAt = now;
        }

        // Usa factory para criar DbContext separado, permitindo chamadas concorrentes
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Set<Query>().AddRangeAsync(queryList);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Busca query em cache usando um DbContext separado para permitir operações concorrentes.
    /// Este método é thread-safe e pode ser chamado em paralelo.
    /// </summary>
    public async Task<Query?> GetCachedQueryAsync(string isbn, int providerId, int cacheTimeMinutes)
    {
        var cacheThreshold = DateTime.UtcNow.AddMinutes(-cacheTimeMinutes);

        // Usa factory para criar DbContext separado, permitindo chamadas concorrentes
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Set<Query>()
            .Where(q => q.SearchIsbn == isbn
                && q.ProviderId == providerId
                && q.QueriedAt >= cacheThreshold
                && q.Success)
            .OrderByDescending(q => q.QueriedAt)
            .FirstOrDefaultAsync();
    }
}
