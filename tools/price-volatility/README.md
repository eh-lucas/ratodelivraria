# Sonda de volatilidade de preço

Descobre **de quanto em quanto tempo as lojas realmente mudam preço**, para escolher
com número o intervalo entre crawls e a validade do preço servido do banco.

Utilitário de medição, não parte do produto: Python 3 sem dependências, banco SQLite
próprio em `data/volatility.db`, nenhum acesso ao Postgres do app e nenhuma migration.
Pode rodar, parar e apagar sem consequência para o sistema.

## A pergunta que ele responde

Hoje o TTL de cache é 30 minutos, escolhido por palpite. Servir preço do banco só é
honesto se soubermos por quanto tempo o preço guardado continua igual ao da loja.
Isso não se deduz — se mede.

## Como funciona

A cada execução lê as primeiras páginas do endpoint JSON
`product/search/infiniteScroll` de cada loja — o mesmo que o `CatalogCrawler` usa,
com os mesmos cabeçalhos — e guarda `product_id -> preço`. Na execução seguinte
compara com o que já tinha:

- preço igual → só atualiza `last_seen` (vira denominador da taxa);
- preço diferente → grava uma linha em `changes` com o intervalo desde a leitura
  anterior, o valor antigo, o novo e a natureza da mudança.

O arquivo cresce com a informação, não com a repetição.

**Preço efetivo** = `special` quando existe, senão `price` — a mesma regra do crawler.
A tabela `changes` separa promoção entrando/saindo de mudança de preço de tabela,
porque a conclusão é diferente: se quase tudo é promoção, o intervalo de crawl tem
que seguir o ritmo das campanhas.

## Uso

```bash
cd tools/price-volatility

./volatility.py stores                          # lojas e em que servidor cada uma mora
./volatility.py snapshot                        # uma leitura (6 lojas, 1 página)
./volatility.py snapshot --pace 15              # mais devagar ainda
./volatility.py snapshot --limit 0              # todas as 67 lojas
./volatility.py snapshot --stores 1,3,17        # lojas específicas, por id
./volatility.py snapshot --every 20             # repete a cada 20 min até Ctrl-C
./volatility.py report                          # o que foi medido até agora
```

Para sobreviver a reboot, use cron em vez de `--every`:

```cron
*/20 * * * * cd /home/lucas/Desktop/Projects/Sherlock/tools/price-volatility && ./volatility.py snapshot --quiet >> data/snapshot.log 2>&1
```

## Boa vizinhança — a restrição que manda no desenho

As lojas **não são 67 servidores**: 67 domínios resolvem para 2 IPs. Toda requisição
que abrimos cai na mesma fila, e esse servidor devolve 504 quando pressionado. Então:

- o agrupamento é **por IP resolvido**, nunca por domínio — limitar por domínio não
  protegeria nada, porque 4 "lojas diferentes" em paralelo são 4 acessos ao mesmo host;
- **uma requisição por vez** em cada servidor, com pausa mínima de 8s (`--pace`);
- a pausa **cresce sozinha**: resposta acima de 12s multiplica por 1,5; erro, 429 ou
  503 põe o servidor de molho por 2 minutos. A latência dele é o pedido de trégua —
  é o único sinal honesto que temos de fora sobre quanta carga ele aguenta;
- resposta rápida devolve o ritmo ao valor base aos poucos, nunca de uma vez.

Toda execução imprime a carga **antes** de gerar qualquer uma:

```
carga: 6 lojas / 2 servidores, 6 requisições por rodada
       (3 no servidor mais carregado, >= 8s entre elas = 0.4min)
       a cada 20min = 9 req/hora no servidor mais carregado (1 a cada 6.7min)
```

No ajuste padrão (6 lojas, 1 página, a cada 20 min) o servidor mais carregado recebe
**9 requisições por hora** — uma a cada ~7 minutos, menos que uma pessoa navegando o
catálogo. E rende 3.000 produtos por leitura, ~216 mil observações pareadas por dia:
sobra estatística de folga. Ampliar a amostra sai caro para as lojas e acrescenta
pouco; prefira esperar mais tempo a apertar o ritmo.

`limit=500` é o valor comprovado: 1000 devolve 504 a partir da página ~8 porque o
OFFSET fica alto demais.

## Como ler o relatório

- **TAXA DE MUDANÇA POR INTERVALO** — a curva que define o TTL. O maior intervalo com
  zero mudanças e amostra suficiente (≥200 pareados) é o tempo que dá para servir
  preço do banco sem mudar o resultado para o usuário.
- **MUDANÇAS POR HORA DO DIA** — se as mudanças se concentram numa faixa de horário,
  a loja mexe em preço em lote, e aí o refresh deve rodar *depois* dessa janela em vez
  de de X em X minutos o dia inteiro. Essa é a descoberta que pode economizar quase
  todo o crawl.
- **NATUREZA DA MUDANÇA** — promoção vs. preço de tabela.
- **Tamanho da mudança** — quanto custa errar. Mudança de 1% não muda quem ganha a
  comparação; de 25% muda.

Precisão temporal: a mudança é detectada, não testemunhada. Com leitura a cada 20
minutos, sabe-se que ela ocorreu nos últimos 20 minutos — o histograma por hora tem
essa granularidade.

## Amostragem

A listagem é ordenada por `p.date_added DESC`, então as primeiras páginas são os
produtos mais recentes. Como o pareamento é por `product_id`, produto que sai da
janela amostrada simplesmente acumula intervalo maior até reaparecer — não polui a
medição. Para cobrir a cauda longa do catálogo, aumente `--pages`.

## Contexto: o que já se sabia antes desta sonda

Cruzando as observações repetidas da tabela `queries` do app (383 pares de
(ISBN, loja), ignorando cache):

| Intervalo | Pares | Mudaram |
|---|---|---|
| < 1h | 77 | 0 |
| 1–6h | 138 | 0 |
| > 24h | 168 | 4 (2,4%) |

Indício de que abaixo de 6h nada muda, com amostra pequena (24 ISBNs, 2 dias). Esta
sonda é o que transforma esse indício em número confiável — e as 4 mudanças observadas
foram de −24% a +23%, ou seja, raras mas grandes.
