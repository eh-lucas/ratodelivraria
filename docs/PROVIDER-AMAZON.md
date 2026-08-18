# Amazon como provider

Medições de 2026-08-18, no IP residencial do notebook que serve o site.

## Por que navegador e não HttpClient

A Amazon serve **duas versões da mesma página**, decididas por quem pede.

Na mesma URL, no mesmo minuto:

| Cliente | O que veio |
|---|---|
| Chrome | `R$ 49,74 com 40% de desconto · De: R$ 82,90 · vendido por Academia do Saber` |
| HttpClient / curl | bloco `priceNotAvailable`: "adicione este item ao seu carrinho" |

Isso **não é MAP** (preço mínimo anunciado), que foi a primeira leitura e estava
errada. É a Amazon distinguindo navegador de script.

A assinatura é clara: as respostas sem preço voltam **byte a byte idênticas —
828.367 B em 0,27s** — enquanto uma renderização de verdade leva ~2,6s. É
resposta enlatada de cache para cliente suspeito.

Tentativas que **não** resolvem:

- Cabeçalhos completos de Chrome (`sec-ch-ua`, `sec-fetch-*`, `Accept-Language`)
- Cookies de sessão colhidos de um Chrome real (`session-id`, `ubid-acbbr`,
  `session-token`): resolveu **1 ASIN em 5**
- Parâmetros de cache-busting (`?th=1&psc=1`): resposta idêntica, mesmo tamanho

O discriminador mais provável é impressão digital de TLS, que cabeçalho nenhum
disfarça.

## Onde a Amazon deixa passar e onde não

| Caminho | Script | Navegador |
|---|---|---|
| `/` (home) | **202** — desafio do AWS WAF (`awsWafCookieDomainList`, `gokuProps`) | ok |
| `/s?k={isbn}` (busca) | **503** | ok |
| `/gp/product/{asin}` | 200, mas sem preço | ok |
| `/gp/aod/ajax?asin=` (ofertas) | **503** | — |

O robots.txt libera `/gp/product/` para `User-agent: *` (só `e-mail-friend`,
`product-availability` e `rate-this-item` são bloqueados). `ClaudeBot` tem
`Disallow: /`.

## O que usamos

Busca por ISBN (`/s?k={isbn}&i=stripbooks`) num Chrome headless, e não
`/gp/product/{isbn10}`, por dois motivos:

1. Funciona para livro cujo ASIN **não** é o ISBN-10
2. A busca por ISBN devolve **um card só**, então a edição certa vem resolvida

Medido com o navegador quente e imagem/fonte/CSS bloqueados:

| ISBN | tempo | resultado |
|---|---|---|
| 9788528617986 | 1,03s | O velho e o mar — R$ 35,11 |
| 9786585033121 | 0,82s | O mínimo sobre Platão — R$ 21,93 |
| 9788595084766 | 1,10s | As duas torres — R$ 42,70 |
| 9786556923642 | 0,99s | Os ratos — R$ 54,32 |
| 9788535914849 | 1,16s | 1984 — R$ 28,92 |

**5 de 5.** Mais rápido que as nossas livrarias (p50 4,7s), então a Amazon nunca
é o gargalo — ela responde e espera o resto.

## Desenho

- `AmazonBrowser` (singleton): um Chrome de pé no processo. Subir custa ~1s; uma
  aba nova por consulta, para não carregar estado da busca anterior.
- `AmazonBrowserScraper`: traduz para o mesmo `QueryResult` das livrarias, então
  motor, cache e comparador não sabem que este é diferente.
- `W16Engine` roda as categorias **em paralelo**: serializá-las deixaria a Amazon
  (~1s) esperando 15s de livraria à toa.
- Se o Chrome não subir, a Amazon fica fora daquela busca e as 67 livrarias
  respondem normalmente.

Configuração em `Amazon:` (`Enabled`, `ChromePath`, `MaxConcurrentPages`,
`TimeoutSeconds`). No container o chromium vem do apt — o binário que o
PuppeteerSharp baixa exigiria instalar ~30 bibliotecas de sistema na mão.

## Risco conhecido

Testei dezenas de requisições, não milhares. Como a Amazon reage a esse padrão
ao longo de dias é o que ainda não sei medir.

A alternativa oficial é a PA-API 5.0, que exige conta de Associados aprovada —
descartada por decisão do dono do projeto em 2026-08-18.
