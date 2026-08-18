# A plataforma Cedet — servidor, endpoint e banco de dados deles

> Tudo aqui foi **medido**, não inferido, entre 2026-08-16 e 2026-08-18. Onde ainda é
> hipótese, está marcado como **[A validar]**. Este é o documento de referência sobre a
> infraestrutura e os dados das livrarias; decisões de arquitetura nossa ficam em
> [DESEMPENHO-BUSCA.md](DESEMPENHO-BUSCA.md) e [CATALOG-CRAWLER.md](CATALOG-CRAWLER.md).

## 1. Não são 67 servidores. São 2.

Dos 83 domínios cadastrados, **67 resolvem para 2 endereços IP vizinhos**:

| IP | Domínios |
|---|---|
| `170.82.173.30` | 34 |
| `170.82.174.30` | 33 |

**Consequência que manda em todo o resto:** toda concorrência que abrimos cai na mesma
fila. Limitar por domínio não protege nada — 4 "lojas diferentes" em paralelo são 4
acessos ao mesmo host. Qualquer coleta precisa de semáforo **por IP resolvido**.

### O teto de vazão

Medido com o mesmo padrão de requisição do `CedetSingleSearchHttpClient`, variando a
concorrência sobre as 67 lojas (ISBN diferente por rodada, para não pegar cache da loja):

| Concorrência | Tempo total | p50 por loja | p95 | Throughput |
|---|---|---|---|---|
| 10 | 67,3s | 9,4s | 15,1s | 1,00 req/s |
| 30 | 51,8s | 21,3s | 27,2s | 1,29 req/s |
| 67 | 40,8s | 34,1s | 39,7s | 1,64 req/s |

Uma requisição **isolada** custa 2,8s. O servidor entrega **~1,6 req/s** e é isso —
dobrar a concorrência não dobra a vazão, só converte espera em espera.

### Como o servidor avisa que está sofrendo

Em ordem de gravidade:

1. **A latência sobe.** É o primeiro sinal e o mais confiável. De 2,8s isolado para
   34s a 67 threads.
2. **504 Gateway Timeout.** Aparece com `limit=1000` a partir da página ~8 (OFFSET
   alto), e sob concorrência alta.
3. Sob retry agressivo, o efeito vira bola de neve: transações #34 e #35 do nosso banco
   levaram 151s e 161s com 24 e 25 falhas. Não foi a loja que caiu — fomos nós que
   insistimos.

**Retry contra servidor saturado é contraproducente.** É exatamente a carga que ele não
tem como absorver. Ver `tools/price-volatility/volatility.py` (classe `Server`) para a
política que usa a latência como pedido de trégua.

### robots.txt não nos proíbe

As regras `Disallow` (`/*?route=product/search`, `/*&limit`, `/*&sort`) vêm abaixo de um
bloco que nomeia 6 agentes — `Googlebot`, `Bingbot`, `Facebot`, `Pinterestbot`,
`Twitterbot`, `UptimeRobot` — e valem só para eles. No fim do arquivo há um bloco
separado:

```
User-agent: *

Disallow:
```

`Disallow:` vazio significa liberado. Mantemos a postura conservadora por educação e
porque o servidor mede 504 quando pressionado, não por proibição.

## 2. O endpoint JSON — a peça mais valiosa que descobrimos

```
GET /index.php?route=product/search/infiniteScroll
    &search={termo}      # vazio = catálogo inteiro paginado; aceita ISBN
    &page={n}
    &limit={n}           # 500 comprovado; 1000 devolve 504
    &sort=p.date_added   # opcional
    &order=DESC
```

Cabeçalhos que a loja espera (o `X-Requested-With` é o que importa):

```
X-Requested-With: XMLHttpRequest
Accept: application/json, text/javascript, */*; q=0.01
User-Agent: <navegador honesto>
```

### O que cada produto traz

```
product_id  name  authors[]  price  special  special_percent
quantity  href  thumb  date_published  ondemand  first_variant_type  variants
```

- **`price`** é o preço de tabela, formato BR com `R$`: `'R$ 5.324,00'`.
- **`special`** é o promocional, às vezes sem o `R$`: `' 4.898,08'`. Preço efetivo =
  `special` quando existe, senão `price`.
- **`quantity`** vem no payload — ou seja, o endpoint entrega **estoque** de graça.
  Preço baixo em livro esgotado é recomendação ruim; vale usar. **[A validar]** se o
  número é confiável ou decorativo.
- **Não traz ISBN.** O ISBN mora na página do produto (`ISBN: <13 dígitos>` no HTML),
  daí o `resolve-isbn` sob demanda.

### Armadilhas que custaram tempo

- **`pagination_total` é número de PÁGINAS, não de produtos** — e depende do `limit`.
  Com `limit=20` a mesma loja reporta 925; com `limit=500`, 37.
- **`limit=1000` quebra** a partir da página ~8: OFFSET alto demais → 504.
- **`limit=200` não é mais rápido** que 500: o custo é fixo por requisição, e dá 2,5×
  mais páginas.
- **Loja que ignora `page`** devolveria sempre o mesmo bloco — vale checar se a página
  trouxe algum `product_id` novo antes de continuar paginando.

### Ordenações

| `sort` | Comportamento medido |
|---|---|
| `p.date_added` | Funciona. Ordem espalhada por id — `date_added` não acompanha o id. |
| `p.date_modified` | Devolve ordem diferente de `date_added`, mas **estritamente decrescente por `product_id`** — indistinguível de "mais novos primeiro". |

### `date_modified` NÃO serve para achar preço alterado — refutado em 2026-08-18

Era a hipótese mais promissora: se `date_modified` mexesse a cada alteração de preço,
o refresh leria só o topo dessa ordenação e pararia — 37 páginas por loja virariam 2 ou 3,
e o refresh de hora em hora ficaria viável. **Não é o caso.**

Teste decisivo, com dois produtos cuja mudança de preço está registrada no nosso banco
(loja 57, Católicos de Verdade):

| Produto | Mudança | Quando | Está no head de 500 por `date_modified`? |
|---|---|---|---|
| `35057` "Como ler livros" | R$99,92 → R$86,18 | ~4h antes do teste | **Não** |
| `21546` "Os objetivos da educação" | R$40,23 → R$30,36 | 14–41h antes | **Não** |

O head dessa loja são os ids **36860–38087**, estritamente decrescentes, com **zero** ids
abaixo de 30000 — o retrato de "recém-criados", não de "recém-editados". Um livro antigo
que mudou de preço hoje deveria ter pulado para a posição 1; nenhum dos dois apareceu.

Duas leituras possíveis — `sort=p.date_modified` não está na whitelist do OpenCart e cai
num default por id, **ou** está e o campo simplesmente não é tocado quando o preço muda.
Para a decisão dá no mesmo: o preço é atualizado por um processo em lote que não deixa
rastro nesse campo.

**Corolário:** não existe canal barato de detecção de mudança. `route=product/special` e
`route=product/special/infiniteScroll` também retornam **404** neste tema, então nem a
lista de promoções serve de atalho. **A única forma de saber um preço é ler o preço.**
É isso que dimensiona a coleta — ver [PLANO-PRECO-FRESCO.md](PLANO-PRECO-FRESCO.md).

### As duas operações têm pesos completamente diferentes — o fato mais importante

O mesmo endpoint atende duas coisas que **não** custam o mesmo ao servidor. Medido em
2026-08-18:

| Operação | Latência p50 | Payload | Vazão sustentada |
|---|---|---|---|
| Página de catálogo (`search=` vazio, 500 produtos) | **30s** | 295 KB | **594 req/h** |
| Busca por termo (`search={isbn}`, 1 produto) | **1,8s** | **0,7 KB** (gzip) | **~21.500 req/h** |

**30× de diferença.** A página paginada obriga o servidor a um `OFFSET` sobre 18 mil
linhas e a montar 295 KB de JSON; a busca por ISBN é um índice e uma linha.

E o caminho ao vivo de hoje usa a operação **errada**: o `CedetSingleSearchHttpClient`
baixa a **página HTML** de busca — 29,8 KB contra 0,7 KB do JSON, **43× maior**.

### Escala de concorrência: a consulta leve não satura

| Conexões por servidor | Latência p50 | Vazão |
|---|---|---|
| 1 | 1,40s | 0,7 req/s |
| 5 | 1,82s | 4,93 req/s |
| 10 | 2,65s (p95 3,23s) | 5,98 req/s |

A latência sobe devagar enquanto a vazão sobe rápido — assinatura de caminho **não
saturado**, o oposto exato da página de catálogo (onde 5× de concorrência comprou 24% de
vazão e dobrou a latência).

### O resultado que decide a arquitetura

Fan-out por ISBN nas **67 lojas**, 10 conexões por servidor:

```
TEMPO TOTAL: 11,2s | 67 responderam, 0 falharam, 67 têm o livro
latência p50 2,65s | p95 3,23s | max 4,28s | tráfego total 166 KB
mais barato R$76,19 | mais caro R$124,90 | economia 39%
```

**11,2s contra os ~45s de hoje**, com zero falha e 166 KB no lugar de ~2 MB. Sem cache,
sem banco, sem espelho — só trocando a requisição. Ver
[PLANO-PRECO-FRESCO.md](PLANO-PRECO-FRESCO.md).

## 3. O banco de dados deles

### `product_id` é global da plataforma

O mesmo livro tem o **mesmo `product_id` em todas as lojas**. Serve para deduplicar
entre lojas e — mais importante — **dispensa o ISBN para comparar preço**: dado um
`product_id`, os preços de todas as lojas saem de uma consulta.

### Cada loja escolhe seu sortimento

**Não é um catálogo só.** A hipótese inicial (o `robots.txt` de uma loja aponta para os
sitemaps de outras, logo bastaria varrer uma) foi desmentida pela medição: a 3ª loja
varrida acrescentou **2.290 produtos** que não existiam nas duas anteriores — mais do
que a 2ª acrescentara (851). Varrer uma loja não substitui varrer as outras.

### Tamanhos medidos (3 lojas completas)

| Métrica | Valor |
|---|---|
| Itens por loja completa | 17.270 – 17.989 |
| Páginas de 500 por loja | **37** |
| Produtos únicos (3 lojas) | 20.945 |
| Produtos em mais de uma loja | 18.200 (87%) |
| Lojas por produto (de 3) | 2,58 |
| Spread médio de preço do mesmo produto | 4,0% |
| Spread máximo observado | **120%** (R$32,25 → R$70,95) |

O spread médio baixo com máximo altíssimo é o desenho do produto: na média as lojas
cobram parecido, mas a cauda é onde mora a economia do usuário. Cortar lojas para ir
mais rápido é cortar exatamente essa cauda.

### Preço do crawl == preço da busca ao vivo

95 pares casados por (loja, título exato) entre `catalog_items` e `queries`:
**95 iguais, 0 diferentes, diferença média R$ 0,00.**

É a validação que autoriza servir preço do banco: o número guardado é o mesmo número
que o usuário veria consultando ao vivo.

### Volatilidade de preço

Das observações repetidas em `queries` (383 pares de ISBN+loja, ignorando cache):

| Intervalo entre leituras | Pares | Mudaram |
|---|---|---|
| < 1h | 77 | 0 |
| 1–6h | 138 | 0 |
| > 24h | 168 | 4 (2,4%) |

Abaixo de 6h, nada mudou. Acima de 24h, mudanças raras mas **grandes** (−24,5%, +23,4%,
−13,8%, +13,3%). Amostra pequena (24 ISBNs, 2 dias) — `tools/price-volatility/` mede
isso continuamente para fechar o número.

## 4. A aritmética que decide a estratégia de coleta

O ponto não é qual abordagem é mais útil, é quantas requisições cada resposta custa:

| Abordagem | Requisições | Produtos cobertos | Custo por produto |
|---|---|---|---|
| Um produto em todas as lojas (ao vivo, hoje) | 67 | 1 | **67 req** |
| Varredura completa das 67 lojas | 67 × 37 = 2.479 | ~21 mil | **0,12 req** |

Uma varredura completa custa o equivalente a **37 buscas ao vivo** e traz o preço de
todos os títulos de todas as lojas. É uma diferença de ~560× por produto.

Por isso a coleta é por loja e a **entrega** é por produto: varre-se loja por loja
porque é assim que o endpoint é barato, e o usuário recebe "todos os preços deste livro"
de uma consulta SQL, sem HTTP nenhum no caminho crítico.

### Quanto tempo leva uma varredura completa

2.479 requisições ÷ 2 servidores = ~1.240 por servidor. No ritmo que o servidor tolera:

| Cenário | Segundos por requisição | Tempo total |
|---|---|---|
| Servidor descansado (madrugada) | ~8s | **~2,8h** |
| Realista (resposta ~15s, ritmo adaptativo ~25s) | ~25s | **~8,6h** |
| Servidor pressionado (ritmo em 40s) | ~40s | **~13,8h** |

**Conclusão prática:** refresh completo cabe numa janela noturna — intervalo de 12h ou
24h é viável, intervalo em minutos não é, enquanto depender da varredura completa. E
está alinhado com a volatilidade medida, que não mostra mudança abaixo de 6h. Para
descer a intervalos curtos, o caminho é o `date_modified` da seção 2.

## 5. Perguntas abertas

- ~~`p.date_modified` reflete mudança de preço?~~ **Refutado** — ver seção 2.
- **[A validar]** `quantity` é estoque real? Se sim, dá para não recomendar esgotado.
- **[A validar] — decide se a varredura cabe numa madrugada:** o tempo de resposta cai
  de madrugada? A 8s por requisição a varredura completa cabe em ~2,8h; a 25s, não cabe
  numa janela de 6h e precisa de duas noites. A sonda de volatilidade pode medir isso
  gravando a latência mediana por rodada.
- **[A validar]** O tempo de resposta varia por hora do dia? Se a madrugada for
  significativamente mais rápida, a janela de refresh se paga sozinha.
- Quantos produtos únicos existem no total das 67 lojas? Só 3 estão varridas; a curva
  de novidade por loja (2.290 na 3ª) sugere que o total passa bem de 21 mil.
