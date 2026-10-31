using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sherlock.Business.Configuration;
using Sherlock.Business.Interfaces;
using Sherlock.Domain.Entities;
using Sherlock.Domain.Interfaces;

namespace Sherlock.Api.Controllers;

/// <summary>
/// O que as pessoas mais procuram aqui. Leitura do nosso próprio histórico —
/// não consulta loja nenhuma, não custa crédito.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RankingController : ControllerBase
{
    private readonly IQueryRepository _queryRepository;
    private readonly ICacheService _cache;
    private readonly RankingSettings _settings;

    /// <summary>
    /// Mesma janela do ResponseCache do endpoint: navegador e servidor expiram
    /// juntos. Com TTL menor aqui, um F5 pagaria a consulta inteira de novo;
    /// com TTL maior, o ranking demoraria mais a reagir do que a tela promete.
    /// </summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A consulta varre a tabela de queries inteira e custa cerca de um segundo.
    /// Sem esta porta, todo restart do servidor — e toda expiração de TTL em
    /// hora movimentada — põe as requisições em voo recalculando o mesmo
    /// ranking ao mesmo tempo. Quem perde a corrida espera aqui e encontra o
    /// resultado já pronto no cache.
    /// </summary>
    private static readonly SemaphoreSlim Porta = new(1, 1);

    public RankingController(
        IQueryRepository queryRepository,
        ICacheService cache,
        IOptions<RankingSettings> settings)
    {
        _queryRepository = queryRepository;
        _cache = cache;
        _settings = settings.Value;
    }

    /// <summary>Os livros mais consultados, do mais procurado para o menos.</summary>
    [HttpGet("most-searched")]
    [AllowAnonymous]
    // 60s e nao 300: quem acabou de buscar um livro quer ver o ranking reagir.
    // O cabecalho governa o navegador; o cache abaixo governa o servidor.
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> MostSearched(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var teto = Math.Clamp(limit, 1, 50);

        // O marco entra na chave: mexer no ResetAt muda o ranking inteiro, e o
        // resultado antigo nao pode sobreviver a troca.
        var chave = $"ranking:most-searched:{teto}:{_settings.ResetAt?.Ticks ?? 0}";

        var cacheado = await _cache.GetAsync<List<PopularBook>>(chave);
        if (cacheado is not null)
            return Ok(cacheado);

        await Porta.WaitAsync(cancellationToken);
        try
        {
            // Segunda olhada: quem esperou na porta ja costuma ter resposta.
            cacheado = await _cache.GetAsync<List<PopularBook>>(chave);
            if (cacheado is not null)
                return Ok(cacheado);

            var livros = await _queryRepository.GetMostSearchedAsync(
                teto, _settings.ResetAt, cancellationToken);

            var lista = livros.ToList();
            await _cache.SetAsync(chave, lista, Ttl);

            return Ok(lista);
        }
        finally
        {
            Porta.Release();
        }
    }
}
