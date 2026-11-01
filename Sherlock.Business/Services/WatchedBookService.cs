using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Core.Watch;
using Sherlock.Business.DTOs;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Business.Services;

/// <summary>
/// O CRUD da lista de observados.
///
/// Uma decisão merece explicação: **adicionar não dispara busca**. O plano dizia
/// "POST /api/Watched → dispara a busca normal, estabelece o preço de referência",
/// e a parte da referência continua valendo — mas uma varredura leva ~17s, então um
/// lote de 20 livros deixaria a pessoa olhando um botão girando por quase seis
/// minutos, e 1.340 requisições sairiam para as lojas de uma vez.
///
/// Em vez disso a referência sai do histórico que já existe em `queries` (custo
/// zero, e a maioria dos livros que alguém observa passou pela busca dele minutos
/// antes). Quem não tiver histórico fica com referência nula, e a primeira
/// varredura noturna estabelece — o <c>WatchSweepWorker</c> atende esses primeiro.
/// </summary>
public class WatchedBookService : IWatchedBookService
{
    private readonly IWatchedBookRepository _repository;
    private readonly IQueryRepository _queries;
    private readonly ICatalogRepository _catalog;
    private readonly WatchSettings _settings;
    private readonly ImmediateSweepQueue _varreduraImediata;
    private readonly ILogger<WatchedBookService> _logger;

    public WatchedBookService(
        IWatchedBookRepository repository,
        IQueryRepository queries,
        ICatalogRepository catalog,
        IOptions<WatchSettings> settings,
        ImmediateSweepQueue varreduraImediata,
        ILogger<WatchedBookService> logger)
    {
        _repository = repository;
        _queries = queries;
        _catalog = catalog;
        _settings = settings?.Value ?? new WatchSettings();
        _varreduraImediata = varreduraImediata;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WatchedBookDto>> ListAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        var itens = await _repository.GetByUserAsync(userId, cancellationToken);
        if (itens.Count == 0) return Array.Empty<WatchedBookDto>();

        var menores = await _repository.GetLowestPricesAsync(
            itens.Select(i => i.Isbn), _settings.LowestPriceWindowDays, cancellationToken: cancellationToken);

        return itens.Select(i => ParaDto(i, menores)).ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<PricePointDto>>> HistoryAsync(
        int userId, int days, CancellationToken cancellationToken = default)
    {
        var meus = await _repository.GetByUserAsync(userId, cancellationToken);
        if (meus.Count == 0) return new Dictionary<string, IReadOnlyList<PricePointDto>>();

        // Janela limitada para a resposta não crescer sem teto, e porque o cartão
        // mostra o mês — o mesmo recorte do "menor preço dos últimos 30 dias".
        var janela = days is >= 7 and <= 180 ? days : _settings.LowestPriceWindowDays;

        var series = await _repository.GetPriceHistoryAsync(
            meus.Select(m => m.Isbn), janela, cancellationToken);

        return series.ToDictionary(
            par => par.Key,
            par => (IReadOnlyList<PricePointDto>)par.Value
                .Select(ponto => new PricePointDto
                {
                    D = ponto.Day.ToString("yyyy-MM-dd"),
                    P = decimal.Round(ponto.Price, 2),
                })
                .ToList());
    }

    public async Task<WatchedBookBatchResult> AddAsync(
        int userId, WatchedBookBatchRequest request, CancellationToken cancellationToken = default)
    {
        var resultado = new WatchedBookBatchResult();
        var jaTem = await _repository.CountByUserAsync(userId, cancellationToken);

        var tocados = new List<WatchedBook>();
        var criados = 0;

        foreach (var item in request.Items)
        {
            var isbn = SomenteDigitos(item.Isbn);

            if (isbn.Length is not (10 or 13))
            {
                resultado.Rejected.Add(new WatchedBookRejection
                {
                    Isbn = item.Isbn,
                    Reason = "ISBN inválido."
                });
                continue;
            }

            var existente = await _repository.GetByUserAndIsbnAsync(isbn, userId, cancellationToken);

            if (existente != null)
            {
                // Repetir o pedido é editar a regra, não um erro. Reativa também:
                // quem pausou e pediu de novo quer de volta. Não conta para o
                // teto: a linha já existia.
                AplicarRegra(existente, item.Rule, item.TargetPrice, item.DropPercent);
                existente.Active = true;
                tocados.Add(existente);
                continue;
            }

            // O teto conta as linhas que já existem mais as criadas neste lote.
            // Checar só no começo deixaria um lote de 30 passar inteiro numa
            // conta vazia.
            if (jaTem + criados >= _settings.MaxPerUser)
            {
                resultado.Rejected.Add(new WatchedBookRejection
                {
                    Isbn = item.Isbn,
                    Reason = $"Limite de {_settings.MaxPerUser} livros observados atingido."
                });
                continue;
            }

            var livro = new WatchedBook
            {
                UserId = userId,
                Isbn = isbn,
                Title = item.Title?.Trim() ?? string.Empty,
                Author = item.Author?.Trim(),
                ImageUrl = item.ImageUrl?.Trim(),
                CreatedAt = DateTime.UtcNow,
                Active = true,
            };

            AplicarRegra(livro, item.Rule, item.TargetPrice, item.DropPercent);
            await PreencherMetadadosAsync(livro, cancellationToken);

            await _repository.AddAsync(livro);
            tocados.Add(livro);
            criados++;
        }

        if (tocados.Count > 0)
        {
            var menores = await _repository.GetLowestPricesAsync(
                tocados.Select(n => n.Isbn), _settings.LowestPriceWindowDays, cancellationToken: cancellationToken);

            // A referência é o menor preço conhecido na hora da adição. Faltando
            // histórico ela fica nula e a primeira varredura estabelece.
            foreach (var livro in tocados.Where(n => n.ReferencePrice == null))
            {
                if (!menores.TryGetValue(livro.Isbn, out var menor)) continue;
                livro.ReferencePrice = menor;
                livro.ReferencePriceAt = DateTime.UtcNow;
            }

            await _repository.SaveChangesAsync(cancellationToken);
            resultado.Items = tocados.Select(n => ParaDto(n, menores)).ToList();

            EnfileirarVarredura(tocados);
        }

        _logger.LogInformation(
            "[Watch] Usuário {UserId}: {Add} livros observados, {Rej} recusados",
            userId, resultado.Items.Count, resultado.Rejected.Count);

        return resultado;
    }

    public async Task<WatchedBookDto?> UpdateAsync(
        int userId, int id, WatchedBookPatchRequest request, CancellationToken cancellationToken = default)
    {
        var livro = await _repository.GetByIdAsync(id);

        // Id de outra pessoa responde igual a id inexistente: dizer "existe, mas
        // não é seu" já entrega que ele existe.
        if (livro == null || livro.UserId != userId) return null;

        if (request.Rule.HasValue || request.TargetPrice.HasValue || request.DropPercent.HasValue)
        {
            AplicarRegra(
                livro,
                request.Rule ?? livro.Rule,
                request.TargetPrice ?? livro.TargetPrice,
                request.DropPercent ?? livro.DropPercent);
        }

        if (request.Active.HasValue) livro.Active = request.Active.Value;

        await _repository.SaveChangesAsync(cancellationToken);

        var menores = await _repository.GetLowestPricesAsync(
            new[] { livro.Isbn }, _settings.LowestPriceWindowDays, cancellationToken: cancellationToken);

        return ParaDto(livro, menores);
    }

    public async Task<bool> RemoveAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var livro = await _repository.GetByIdAsync(id);
        if (livro == null || livro.UserId != userId) return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    /// <summary>
    /// Mantém regra e parâmetro coerentes. Guardar um alvo pela metade — regra
    /// `TargetPrice` sem preço — faria o avaliador noturno ignorar o livro em
    /// silêncio, que é a pior forma de errar num vigia.
    /// </summary>
    private static void AplicarRegra(WatchedBook livro, WatchRule regra, decimal? alvo, int queda)
    {
        if (regra == WatchRule.TargetPrice && alvo is > 0)
        {
            livro.Rule = WatchRule.TargetPrice;
            livro.TargetPrice = alvo;
        }
        else
        {
            livro.Rule = WatchRule.PercentDrop;
            livro.TargetPrice = null;
        }

        livro.DropPercent = queda is >= 1 and <= 90 ? queda : 15;
    }

    /// <summary>
    /// Pede a primeira varredura de quem ainda não tem o que mostrar.
    ///
    /// Só o que falta: livro adicionado da tela de resultado já chega com título,
    /// capa e referência vindos da busca que acabou de rodar, e varrer de novo
    /// seria repetir 67 requisições por nada. Quem entrou pelo ISBN digitado no
    /// /watched, sem histórico, é quem precisa.
    ///
    /// Não espera. O POST responde na hora e a fila cuida do resto — varrer em
    /// linha custaria ~17s por livro, e um lote no teto de 30 seriam mais de oito
    /// minutos de espera (§11).
    /// </summary>
    private void EnfileirarVarredura(List<WatchedBook> tocados)
    {
        foreach (var livro in tocados)
        {
            var faltaPreco = livro.ReferencePrice == null;
            var faltaCapa = string.IsNullOrWhiteSpace(livro.ImageUrl);
            var faltaTitulo = string.IsNullOrWhiteSpace(livro.Title) || livro.Title == livro.Isbn;

            if (faltaPreco || faltaCapa || faltaTitulo)
                _varreduraImediata.Enfileirar(livro.Isbn);
        }
    }

    /// <summary>
    /// Título, autor e capa quando a tela não mandou — na ordem do §3.2, da fonte
    /// mais barata para a mais cara. As duas são leitura de banco: nenhuma
    /// requisição sai para as lojas por causa de uma capa.
    ///
    /// 1. `queries` — livro que alguém já buscou tem capa guardada. A maioria cai
    ///    aqui, porque observar um livro quase sempre vem logo depois de buscar ele.
    /// 2. catálogo — espelho local das lojas. Tem título e autor, não tem capa.
    /// 3. sem nada: o cartão fica com o ISBN de rótulo e o placeholder de capa.
    /// </summary>
    private async Task PreencherMetadadosAsync(WatchedBook livro, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(livro.Title) && !string.IsNullOrWhiteSpace(livro.ImageUrl))
            return;

        try
        {
            var meta = await _queries.GetMetadataByIsbnAsync(livro.Isbn, cancellationToken);
            if (meta != null)
            {
                if (string.IsNullOrWhiteSpace(livro.Title)) livro.Title = meta.Title ?? string.Empty;
                if (string.IsNullOrWhiteSpace(livro.Author)) livro.Author = meta.Author;
                if (string.IsNullOrWhiteSpace(livro.ImageUrl)) livro.ImageUrl = meta.ImageUrl;
            }

            if (string.IsNullOrWhiteSpace(livro.Title))
            {
                var item = await _catalog.GetByIsbnAsync(livro.Isbn, cancellationToken);
                if (item != null)
                {
                    livro.Title = item.Name;
                    if (string.IsNullOrWhiteSpace(livro.Author)) livro.Author = item.Authors;
                }
            }
        }
        catch (Exception ex)
        {
            // Cartão sem capa é feio; adicionar falhando porque a capa não veio é pior.
            _logger.LogWarning(ex, "[Watch] Metadados do ISBN {Isbn} não vieram do banco", livro.Isbn);
        }

        if (string.IsNullOrWhiteSpace(livro.Title)) livro.Title = livro.Isbn;
    }

    private static WatchedBookDto ParaDto(WatchedBook w, IReadOnlyDictionary<string, decimal> menores)
        => new()
        {
            Id = w.Id,
            Isbn = w.Isbn,
            Title = w.Title,
            Author = w.Author,
            ImageUrl = w.ImageUrl,
            Rule = w.Rule,
            TargetPrice = w.TargetPrice,
            DropPercent = w.DropPercent,
            ReferencePrice = w.ReferencePrice,
            LastPrice = w.LastPrice,
            PreviousPrice = w.PreviousPrice,
            LastCheckedAt = w.LastCheckedAt,
            LastProviderName = w.LastProviderName,
            LastProductUrl = w.LastProductUrl,
            LowestPrice30d = menores.TryGetValue(w.Isbn, out var m) ? m : null,
            LastNotifiedAt = w.LastNotifiedAt,
            Active = w.Active,
            CreatedAt = w.CreatedAt,
        };

    private static string SomenteDigitos(string entrada)
        => new(entrada.Where(char.IsDigit).ToArray());
}
