# Catálogo local + autocomplete por nome (crawler Cedet)

> **Status:** implementado e em uso. Este documento descreve o que existe e o que foi medido.
> Reescrito em 2026-08-18 substituindo o spec original de 2026-08-16 — várias premissas
> daquele texto se mostraram erradas quando confrontadas com medição. As correções estão
> marcadas com **[Corrigido]**.

## Objetivo

O usuário digita o **nome do livro** e recebe sugestões enquanto digita. Ao escolher um
título, o app já tem o **ISBN** e dispara a busca de preço ao vivo (`W16Engine`).

Por que o catálogo precisa estar no nosso Postgres: autocomplete é "nome → sugestões",
respondendo em milissegundos por tecla. As APIs públicas de ISBN (BrasilAPI, Open Library,
Google Books) são `ISBN → dados` — **não têm busca por nome** — e ainda cobram rate limit.
Chamá-las a cada tecla seria lento e abusivo.

## O que está implementado

| Peça | Onde |
|---|---|
| Entidade | `Sherlock.Domain/Entities/CatalogItem.cs` |
| Repositório | `Sherlock.Domain/Interfaces/ICatalogRepository.cs` → `Sherlock.Data/Repositories/CatalogRepository.cs` |
| Crawler | `Sherlock.Business/Core/Crawling/CatalogCrawler.cs` |
| Limites | `Sherlock.Business/Core/Crawling/CatalogCrawlSettings.cs` |
| Orquestração | `Sherlock.Business/Services/CatalogService.cs` |
| API | `Sherlock.Api/Controllers/CatalogController.cs` |
| Migration | `20260818110755_AddCatalogItems` (inclui `pg_trgm` + índice GIN) |

Endpoints:
- `GET /api/catalog/suggest?q={termo}` — autocomplete (índice trigram)
- `POST /api/catalog/{id}/resolve-isbn` — abre a página do produto e extrai o ISBN sob demanda
- `POST /api/catalog/crawl` — dispara o crawl (`ProviderIds`, `MaxProviders`, `Force`, `Full`)

## Como a coleta funciona

Usamos o endpoint JSON `product/search/infiniteScroll` com `search=` **vazio**, que pagina a
loja inteira. **[Corrigido]** O spec original planejava BFS por 104 categorias + uma
requisição por página de produto (~8.000 requisições por loja). O endpoint JSON reduz isso
para **~13 requisições por loja** com `limit=500`.

O JSON traz `product_id`, nome, autores, preço e href — **mas não o ISBN**. O ISBN mora na
página do produto e por isso é resolvido **sob demanda**, quando o usuário escolhe um título
que ainda não tem ISBN gravado (é a origem da mensagem "buscando ISBN...").

Detalhes que custaram tempo para descobrir:
- `pagination_total` é o número de **páginas**, não de produtos.
- `product_id` é **global na plataforma Cedet**, não por loja — serve para deduplicar entre lojas.
- Ordenar por `&sort=p.date_added&order=DESC` coloca os produtos novos primeiro.

## Números medidos

| Métrica | Valor |
|---|---|
| Itens coletados | 53.988 |
| Títulos únicos | 20.094 |
| Lojas completas | 5 |
| Itens com preço | 53.988 (100%) |
| Tempo por loja | ~16 min |
| Requisições por loja | ~13 páginas de 500 |

### Tamanho de página (medido em produção)
- **200** — mesma latência de 500 (o custo é fixo por requisição) e 2,5× mais páginas.
- **1000** — a loja devolve **504** a partir da página ~8: o `OFFSET` fica alto demais.
- **500** — varredura completa sem um único erro. **É o valor comprovado.**

## Fatos sobre a hospedagem — o que realmente limita

**[Corrigido] Não é "um catálogo só".** O spec original concluiu, a partir do `robots.txt` de
uma loja apontar para sitemaps de outras, que bastava varrer **uma** fonte. A medição desmente:
a 3ª loja varrida (Araceli) acrescentou **2.290 produtos novos** que não existiam nas duas
anteriores — mais do que a 2ª loja acrescentara (851). Cada livraria escolhe seu sortimento.
**Varrer uma loja não substitui varrer as outras.**

**As lojas compartilham servidor.** Dos 83 domínios cadastrados, **67 resolvem para apenas 2
endereços IP vizinhos** (`170.82.173.30` com 34 e `170.82.174.30` com 33). É por isso que o
crawler usa um **semáforo por IP de destino**, e não por domínio: limitar por domínio não
protege nada — 4 "lojas diferentes" em paralelo viram 4 varreduras no mesmo servidor, que
responde 504.

**[Corrigido] O `robots.txt` não nos proíbe.** O spec original leu as regras `Disallow` como
se valessem para nós. Elas vêm logo abaixo de um bloco que nomeia 6 agentes
(`Googlebot`, `Bingbot`, `Facebot`, `Pinterestbot`, `Twitterbot`, `UptimeRobot`) e valem só
para eles. No fim do arquivo há um bloco separado:

```
User-agent: *

Disallow:
```

`Disallow:` vazio significa **liberado**. Ainda assim mantemos a postura conservadora
(pausa entre páginas, semáforo por IP, sem rajada) — por educação, e porque o servidor
mede 504 quando pressionado.

**[Corrigido] Não existe atalho por sitemap.** Os sitemaps anunciados no `robots.txt` estão
em `index.php?route=feed/google_sitemap` (não em `/sitemap.xml`) e **todos respondem 403**.
O endpoint `infiniteScroll` também não aceita filtro por ID nem por data. **Não há como
descobrir o catálogo de uma loja nova sem paginá-la inteira.**

## Crawl incremental: o que funciona e o que não funciona

`StopAfterKnownPages` encerra a loja depois de N páginas seguidas sem nenhum `product_id`
novo, com a listagem ordenada do mais recente para o mais antigo.

**Isso vale para re-crawl de loja já conhecida, não para loja nova.** Medido na Araceli: das
37 páginas, 36 foram buscadas — os 2.290 produtos inéditos estavam **espalhados por todo o
catálogo**, não concentrados no início, e a parada antecipada nunca disparou. O sortimento de
uma loja nova é diferente o suficiente para que a ordenação por data não ajude.

## Limites configurados

| Parâmetro | Valor | Motivo |
|---|---|---|
| `MaxParallelProviders` | 4 | O limite que protege de verdade é o semáforo por IP |
| `DelayBetweenPagesMs` | 3000 | ~3 req/min por servidor, abaixo de um punhado de visitantes |
| `PageSize` | 500 | Único valor sem 504 (ver acima) |
| `RequestTimeoutSeconds` | 120 | O custo é a loja montar a página, não a rede |
| `MaxConsecutiveErrors` | 3 | Desiste da loja |
| `MaxPagesPerProvider` | 200 | Trava contra paginação infinita |
| `StopAfterKnownPages` | 3 | Só eficaz em re-crawl |
| `SkipIfCrawledWithinDays` | 6 | Evita refazer tudo num refresh semanal |

## Lições operacionais (erros que já cometemos)

1. **Subir o paralelismo de 4 para 8** derrubou 91 das 93 lojas em timeout de 30s, 0 itens
   coletados, 13 minutos perdidos — porque tudo era mantido em memória até o fim. Hoje cada
   loja é **gravada assim que termina** (`onProviderCompleted`), então uma falha tardia não
   descarta o que já foi coletado.
2. **Reduzir a página de 500 para 200** presumindo "menor = mais rápido": a latência é a mesma
   e o número de páginas foi 2,5× maior. Revertido.
3. **Subir para 1000**: 504 a partir da página ~8. Revertido.
4. A causa raiz das três foi a mesma: **contar limites por domínio quando o gargalo é o servidor.**

## Ligação com o desempenho da busca

O crawler já grava **preço** de todo item (100% de cobertura). Isso abre um caminho que ainda
não exploramos: hoje uma busca dispara 67 requisições ao vivo e leva ~45s, limitada pelos
~1,6 req/s que o servidor compartilhado entrega. Com o catálogo completo no banco, boa parte
dessa resposta poderia sair do Postgres em milissegundos, com o preço ao vivo servindo para
**confirmar** o resultado em vez de descobri-lo. Ver `docs/DESEMPENHO-BUSCA.md`.

## Próximos passos

1. Concluir o crawl das lojas restantes (5 de 67 feitas), uma por vez, ~16 min cada.
2. Agendar re-crawl incremental (aí sim `StopAfterKnownPages` compensa).
3. Preencher ISBN em lote em segundo plano, para eliminar o "buscando ISBN..." da tela.
