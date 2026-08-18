# Desempenho da busca — medição e caminhos

> Medido em 2026-08-18 contra as lojas reais, com o mesmo padrão de requisição do
> `CedetSingleSearchHttpClient`.

## A pergunta

Uma busca leva ~45s para o usuário. É lentidão das livrarias, ou nossa?

**As duas coisas.** O teto é das livrarias, mas hoje não chegamos nem perto do teto, e a
espera que o usuário sente é muito maior do que precisaria ser.

## O que foi medido

Uma requisição **isolada** a uma loja: **2,8s**.

As mesmas 67 lojas, variando a concorrência (ISBN diferente por rodada, para não pegar cache
da loja):

| Concorrência | Tempo total | p50 por loja | p95 | Throughput |
|---|---|---|---|---|
| 10 | 67,3s | 9,4s | 15,1s | 1,00 req/s |
| **30** (produção) | **51,8s** | **21,3s** | 27,2s | 1,29 req/s |
| 67 | 40,8s | 34,1s | 39,7s | 1,64 req/s |

A medição a 30 reproduz a produção quase exatamente (transações reais: 44–52s, p50 17–22s),
então o banco de ensaio é confiável.

## Por que existe um teto

**67 das 83 lojas moram em 2 IPs vizinhos** (`170.82.173.30` e `170.82.174.30`). Não são 67
servidores — são dois. Toda concorrência que abrimos cai na mesma fila.

Isso aparece na curva: dobrar a concorrência não dobra a vazão. Entre 10 e 67 threads, o tempo
total cai só 39% (67s → 41s) enquanto a latência individual quase quadruplica (9,4s → 34,1s).
Clássico de fila saturada: estamos convertendo espera em espera. O servidor entrega
**~1,6 req/s** e é isso.

Nesse sentido a resposta é sim, é das livrarias. **Mas** uma requisição sozinha custa 2,8s, e a
nossa mediana em produção é 21s — 7× pior. A diferença inteira é fila que **nós mesmos**
criamos.

## O problema mais caro: nosso timeout está dentro da faixa de operação

`CedetSingleSearchHttpClient` usa **timeout de 30s com 2 retries** e backoff exponencial:

```
30s + 0,4s + 30s + 0,8s + 30s = 91,2s
```

Esse número aparece cru no banco: `max(response_time_ms) = 91205`, **idêntico em todas as
lojas**. Não é uma loja lenta, é o nosso próprio teto de tentativas.

E a 30 de concorrência o p95 medido é **27,2s** — a 3 segundos do timeout. Qualquer variação
normal empurra um punhado de lojas para além dos 30s, e aí:

> requisição estoura → retry → **mais carga no servidor já saturado** → mais requisições
> estouram → retry storm.

Está registrado no banco: transações #34 e #35 levaram **151s e 161s**, com 24 e 25 falhas.
Não foi a loja que caiu; fomos nós que insistimos.

**Retry contra um servidor saturado é contraproducente** — é exatamente a carga que ele não
tem como absorver.

## Caminhos, em ordem de impacto

### 1. Entregar resultado conforme chega (maior ganho percebido)
Hoje o usuário espera os 67 terminarem para ver qualquer coisa. A primeira loja responde em
**3–9s**. Transmitir cada resultado assim que chega leva o tempo até a **primeira informação
útil** de 45s para menos de 10s, sem tocar em uma única requisição.

A infraestrutura já existe: `SearchProgressStore` e o polling de `progress/{jobId}` já
percorrem esse caminho — hoje carregam só um contador. Passar a carregar também os resultados
parciais é incremental.

### 2. Cache com prazo realista
TTL atual: **30 minutos**. Preço de livro não muda a cada meia hora. A taxa de acerto hoje é
de 16% (401 de 2.507 consultas), e o acerto custa **0ms**.

Subir para 12–24h transforma buscas repetidas — que são as mais comuns, porque títulos
populares se repetem entre usuários — em resposta instantânea. Risco baixo e reversível.

Quanto subir deixou de ser palpite: cruzando as observações repetidas da tabela `queries`
(383 pares de ISBN+loja, ignorando cache), **nenhum preço mudou em intervalos abaixo de 6h**
(215 pares) e 2,4% mudaram acima de 24h — raros, mas grandes (−24% a +23%). A amostra é
pequena (24 ISBNs, 2 dias), então `tools/price-volatility/` foi montado para medir isso
continuamente e fechar o número.

### 3. Corrigir timeout e retry
Subir o timeout para ~45–60s e **reduzir para 1 retry**. Isso não acelera o caso bom; elimina
o caso ruim (as transações de 151s) e para de alimentar a saturação.

### 4. Servir preço do catálogo (estrutural)
O crawler **já grava preço de todo item** — 53.988 itens, 100% com preço. Com o catálogo
completo, a comparação sai do Postgres em milissegundos e a consulta ao vivo passa a
**confirmar** o preço das poucas lojas candidatas em vez de descobrir 67 do zero.

É o único caminho que rompe o teto de 1,6 req/s, porque deixa de depender dele no caminho
crítico. Depende de concluir o crawl (5 de 67 lojas).

## O que *não* vale a pena

- **Subir a concorrência acima de 30 sem antes mexer no timeout.** A 67 threads o p95 é 39,7s,
  ou seja, *acima* do timeout de 30s: metade das lojas falharia e entraria em retry. O ganho
  bruto de 11s viraria prejuízo.
- **Consultar menos lojas para ir mais rápido.** Os preços variam de verdade: no ISBN
  9788594090782 os valores vão de **R$34,11 a R$101,17**. Cortar lojas é cortar economia do
  usuário, que é o produto. E o vencedor não se concentra: nenhuma loja ganha mais que 11 vezes
  na amostra.

## O que fizemos depois de aceitar o teto (2026-08-18)

Medição de uma busca real em produção, com o caminho JSON já no ar:

| | |
|---|---|
| Lojas | 68 |
| Mais rápida | 1,5s |
| p50 | 4,0s |
| p90 | 6,7s |
| Mais lenta | 9,1s |
| Soma do tempo das lojas | 294,7s |
| Tempo de parede | 17,4s |

294,7s de trabalho entregues em 17,4s dá concorrência efetiva de ~17, contra 20
configurados. **Não é cauda longa** — nenhuma loja travada segurando as outras.
É trabalho uniforme contra um servidor saturado, e o total é simplesmente
`trabalho ÷ concorrência`. Como a concorrência já está no joelho da curva, o
número não desce por força bruta.

Então atacamos por fora do teto.

### Mostrar as ofertas conforme chegam

O `SearchProgress` passou a acumular as ofertas já respondidas, e o endpoint
`progress/{jobId}` as devolve junto do contador. A tela renderiza incremental.

Medido numa busca fria de ISBN popular:

```
   3,0s   1/68 lojas |  1 oferta  | menor R$ 26,76
   4,1s  21/68 lojas | 19 ofertas | menor R$ 26,76
   9,1s  40/68 lojas | 35 ofertas | menor R$ 26,76
  16,1s  68/68 lojas | fim        | menor R$ 26,76
```

O tempo total não mudou. O que mudou é que **o menor preço já estava na primeira
oferta, aos 3s** — os outros 13s só confirmaram. Custo em carga no servidor das
livrarias: zero, é o mesmo trabalho, só reportado antes.

### Manter os populares quentes

`PopularBooksPrefetcher` lê o ranking de mais consultados e revisita os primeiros
a cada 50 minutos — abaixo dos 60 do cache, senão esfria antes da próxima
passada.

Duas escolhas para não pesar no servidor deles:

- **Um livro por vez, espaçado de 60s.** Dez de enfiada seriam 680 requisições
  na mesma fila em que estão as buscas de gente de verdade.
- **Sem forçar.** O motor já pula a loja cujo resultado está fresco, então uma
  passada logo depois da outra custa quase nada. Medido: `68 lojas, 68 ainda no
  cache, 298ms` — nenhuma requisição saiu.

O aquecedor marca as transações com `isPrefetch: true` e o ranking as exclui.
Sem isso ele votaria nos próprios livros a cada rodada e o ranking congelaria
nos dez primeiros para sempre.

### Capa do livro

O JSON que já baixamos traz `thumb`, e a gente jogava fora. Nenhuma requisição
nova: as imagens moram em `static.cedet.com.br` e o `product_id` é global entre
as lojas, então a mesma capa serve para todas. Vem junto `quantity` e
`ondemand`, ainda não usados.

A capa é persistida em `queries.image_url` porque o resultado servido do cache
precisa dela tanto quanto o recém-consultado.
