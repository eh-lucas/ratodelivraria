# Plano: preço fresco e busca rápida

> Reescrito em 2026-08-18 depois de medir o que faltava. **A versão anterior deste
> documento estava errada no essencial:** dimensionava tudo em torno de espelhar o
> catálogo no banco, porque assumia que a única forma de ser rápido era não falar com as
> lojas. A medição mostrou que dá para falar com as 67 lojas em 11 segundos. Os números
> estão em [PLATAFORMA-CEDET.md](PLATAFORMA-CEDET.md).

## O que mudou

Três medições, em ordem de impacto:

1. **A busca por ISBN no endpoint JSON custa 1,8s e 0,7 KB.** A página de catálogo custa
   30s e 295 KB. São a **mesma rota** com pesos 30× diferentes.
2. **O caminho ao vivo de hoje usa a operação mais cara que existe**: a página HTML de
   busca, 29,8 KB, 43× maior que o JSON equivalente.
3. **67 lojas em 11,2s**, zero falhas, 166 KB — medido, com 10 conexões por servidor.

E duas descobertas negativas que fecham portas:

- **`date_modified` não se move com o preço** (refutado com dois produtos cuja mudança
  está registrada no nosso banco). Não existe canal de "o que mudou".
- **A varredura completa não pode ser frequente.** 2.479 requisições a 594 req/h = 4,2h
  **monopolizando** os dois servidores. Freshness de 1h para o catálogo inteiro é
  fisicamente impossível — e forçar concorrência não resolve: 5× mais conexões na página
  de catálogo comprou 24% de vazão e dobrou a latência.
- **98,8% dos produtos estão com `special`** (promoção). Não existe subconjunto volátil
  pequeno para vigiar.

## A arquitetura

O erro era tratar "rápido" e "fresco" como opostos que só um espelho no banco resolveria.
Com a requisição certa, o caminho ao vivo é rápido **e** fresco por construção.

```
1. usuário busca
2. cache de 1h?  -> devolve na hora, freshness garantida <= 1h
3. senão: fan-out por ISBN nas 67 lojas no endpoint JSON (11s, streaming)
   \-> primeiro resultado na tela em ~2,6s
4. grava no cache por 1h e no catalog_items (preço + price_checked_at)
```

### Por que isso atende os dois requisitos

| Requisito | Como é atendido |
|---|---|
| Não pagar 67 requisições por usuário | O cache de 1h faz o **primeiro** usuário da hora pagar; os seguintes, zero. Título popular custa ~0. |
| Freshness ≤ 1h | É o TTL do cache. Não vem de manter o catálogo fresco — vem de nunca servir nada com mais de 1h. |
| Não entregar dado frio | Nada com mais de 1h chega ao usuário. Abaixo de 6h a volatilidade medida é **zero** em 215 observações, então 1h é folgado. |

### Por que 1h de cache é seguro e não um chute

| Intervalo entre leituras | Pares observados | Mudaram |
|---|---|---|
| < 1h | 77 | 0 |
| 1–6h | 138 | 0 |
| > 24h | 168 | 4 (2,4%) |

A sonda em `tools/price-volatility/` está acumulando amostra para fechar esse número com
mais confiança, mas nenhuma leitura abaixo de 6 horas mostrou mudança.

## Implementação, em ordem de retorno

### 1. Trocar o scraper de HTML para o endpoint JSON — o único item obrigatório

`CedetSingleSearchHttpClient` passa a chamar:

```
GET {loja}/index.php?route=product/search/infiniteScroll&search={isbn}&page=1&limit=20
    X-Requested-With: XMLHttpRequest
    Accept: application/json, text/javascript, */*; q=0.01
    Accept-Encoding: gzip
```

E lê `products[0]`: `name`, `price`, `special`, `quantity`, `href`.

Ganhos medidos **ponta a ponta pela API**, com o motor, o banco e os créditos no
caminho (não só o fan-out cru):

| | Hoje (HTML) | JSON, paralelismo 10 | JSON, paralelismo 20 |
|---|---|---|---|
| Busca completa (67 lojas) | ~45s | 17,0s | **14,3s** |
| Lojas que responderam | com falhas | 67/67 | **67/67** |
| Erros | 24–25 em transações ruins | 0 | **0** |
| Tempo por loja (p50) | 17–22s | 2,5s | 3,7s |

O fan-out cru, sem motor nem banco, fez as mesmas 67 lojas em **11,2s** com 166 KB de
tráfego (contra ~2 MB do HTML) — a diferença para os 14,3s é o custo da nossa própria
camada, não das lojas.

Ganhos que não aparecem no relógio: parsing por campo nomeado em vez de seletor
CSS/XPath frágil, e `quantity` (estoque) vindo de graça no payload.

**Também corrigir timeout e retry.** Hoje são 30s com 2 retries (teto de 91,2s, que
aparece cru no banco como `max(response_time_ms) = 91205`). Com p95 de 3,23s, o timeout
certo é **10s com 1 retry**. Retry contra servidor saturado é o que produziu as
transações de 151s e 161s.

### 2. Cache de 1h nas queries

`QueryCacheSettings.DefaultCacheTimeMinutes`: **30 → 60**. Uma linha, reversível.

A regra do cache não muda (ver CLAUDE.md): transação não se cacheia, query por
(livro, loja) se cacheia.

### 3. Streaming dos resultados parciais

A primeira loja responde em ~1,4s e a mediana em 2,65s. Entregar conforme chega leva o
tempo até a **primeira informação útil** para ~2,6s. `SearchProgressStore` e o polling de
`progress/{jobId}` já percorrem esse caminho — hoje carregam só um contador.

### 4. Prefetch do hot set (opcional)

Um `BackgroundService` que, de hora em hora, refaz a busca dos títulos mais procurados
para que o cache nunca esteja frio para eles. Custo: 20 títulos × 67 lojas = 1.340
requisições leves por hora, ~7% da capacidade medida. Efeito: título popular responde em
milissegundos com freshness ≤ 1h.

### 5. Concorrência

A seção de configuração chama-se `Search` (e não `SearchSettings`, apesar do nome da
classe — errar isso faz o valor ser silenciosamente ignorado). Não havia nenhuma no
appsettings, então valia o **default 10**, que obriga 7 rodadas de 67 lojas.

`Search:MaxDegreeOfParallelism`: **10 → 20** (cerca de 10 por servidor, o valor medido).
Não subir mais sem medir: entre 10 e 20 o tempo caiu de 17,0s para 14,3s enquanto a
latência por loja subiu de 2,5s para 3,7s — a curva já está achatando.

## O que o catálogo no banco continua fazendo

`catalog_items` **não** é mais a fonte do preço — é o que faz a busca por nome existir:

- **autocomplete** (nome → sugestões, índice trigram, milissegundos);
- **cobertura** para saber quais lojas têm o produto antes de perguntar;
- **primeira pintura**: mostrar a comparação de imediato com preço datado, enquanto o
  fan-out ao vivo confirma em 11s.

Por isso a varredura completa continua valendo — mas como tarefa de **catálogo**, rodando
em ritmo de madrugada, sem pressa e sem prazo de freshness. Não é mais o caminho crítico.

### Colunas que ainda vale acrescentar

| Coluna | Por quê |
|---|---|
| `price_checked_at` | Quando confirmamos o preço, mudou ou não. `updated_at` muda com qualquer campo. |
| `list_price` / `special_price` | Permite mostrar "de R$124,90 por R$86,18"; 98,8% têm promoção. |
| `quantity` | Estoque, que o JSON entrega de graça. Não recomendar esgotado. |

Sem histórico de preço — só o valor atual, como pedido. A curva de volatilidade vive no
SQLite de `tools/price-volatility/`, que é instrumento de medida, não parte do produto.

Índices que faltam:

```sql
-- "todos os preços deste livro": hoje varre a tabela inteira
CREATE INDEX ix_catalog_items_product_id ON catalog_items (product_id);
CREATE INDEX ix_catalog_items_price_checked_at ON catalog_items (price_checked_at);
```

E em `providers`, `server_ip varchar(45)` — propriedade da loja, não do livro; serve para
o limitador agrupar por servidor sem resolver DNS a cada ciclo.

## O que ficou para trás, e por quê

| Ideia | Veredito |
|---|---|
| Worker olhando `date_modified` de X em X minutos | **Impossível** — o campo não se move com o preço. |
| Espelho completo com freshness de 1h | **Impossível** — 4,2h por passada, no melhor caso, monopolizando os servidores. |
| Backfill de ISBN antes de tudo | **Desnecessário** para preço — o fan-out busca por ISBN direto na loja, e quem busca por nome resolve pelo `product_id`. |
| Derivar o preço das outras lojas de uma só | **Não dá** — 20,5% dos produtos têm preço idêntico entre lojas, mas o resto varia por produto, não por um fator fixo da loja. |
| Vigiar só as promoções | **Não dá** — 98,8% dos produtos estão em promoção. |
| Feed `feed/google_base` | **Bloqueado** por token, mas é a via mais eficiente que existe: traria todo o catálogo com preço em **1 requisição por loja**. Pedir o token à Cedet é a ação de maior alavancagem fora do código. |

## A ação fora do código

`index.php?route=feed/google_base` existe em todas as lojas e responde **"Token
Inválido"**. É o feed do Google Merchant Center: catálogo inteiro com preço, uma
requisição. Com ele, freshness de 1h para **tudo** custaria 67 requisições por hora — e
seria mais leve para o servidor deles do que qualquer coisa que fazemos hoje.

Vale pedir. O interesse é dos dois lados: nós paramos de paginar 730 MB por passada, e
eles ganham tráfego qualificado de quem já decidiu comprar.
