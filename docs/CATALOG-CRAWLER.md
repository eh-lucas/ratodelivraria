# Catálogo local + autocomplete por nome (crawler Cedet)

> **Status:** planejado, não implementado. Este documento é o spec para executar numa sessão futura.
> Escrito em 2026-08-16 com base em medição real do catálogo.

## Objetivo

Permitir que o usuário **digite o nome do livro** e receba sugestões (autocomplete) enquanto
digita. Ao escolher um título, o app já tem o **ISBN** e dispara a busca de preço ao vivo
(fluxo atual do `W16Engine`).

## Por que precisa de banco local (hipótese confirmada)

Autocomplete = "nome → sugestões por prefixo/fuzzy", respondendo em **<50ms** por tecla.

- **BrasilAPI / APIs de ISBN não servem:** são `ISBN → dados`, **não têm busca por nome**.
  Além disso agregam upstreams (CBL etc.) → ~300ms–2s por chamada, com rate limit. Chamar
  a cada tecla seria lento **e** abusivo.
- Logo, o catálogo (título + ISBN) precisa estar **no nosso Postgres, indexado**.

## Medição real do catálogo (feita em 2026-08-16)

Crawl das 104 categorias de uma loja, deduplicado por slug de produto:

| Métrica | Valor |
|---|---|
| **Produtos únicos** | **~7.970** |
| Categorias | 104 |
| Páginas buscadas | 438 |
| Tempo do count | 167s (~2,8 min) |
| ~Tamanho/página de produto | ~48 KB, ~1,3s |

**Conclusão de custo:** catálogo pequeno. Crawl completo (~8k páginas de produto) ≈ **~30 min
uma vez**, ~8 MB no banco, **≈ R$0** (roda no PC que já hospeda o app). Não compromete a UX.

## Fatos descobertos sobre as lojas (importantes p/ o crawler)

- **É um catálogo só, não 67.** Todas as ~67 livrarias rodam a mesma plataforma/catálogo Cedet
  (o `robots.txt` de uma loja aponta para os sitemaps de outras). **Crawleia UMA fonte** (uma
  loja, ex. `bibliotecadoluiz.com.br`, ou avaliar o `cedet.com.br` master).
- **Stack:** OpenCart com tema custom e URLs SEO (slug), ex. `/breve-manual-do-cristao-conservador`.
- **Listagem de categoria** (`index.php?route=product/category&path=N&page=P`):
  - Cada produto = bloco `class="item-product"`.
  - Link+título do produto: `class="product-name" href="SLUG"` → **já dá título + slug** na listagem.
- **Página de produto** contém, em texto: `<h1>` título, `ISBN: <13 dígitos>`, `Editora: ...`,
  `Autores: ...`. → precisa abrir a página do produto **só** para pegar ISBN/autor.
- **robots.txt (RESPEITAR):**
  - `Disallow: /*?route=product/search` → **não** use a rota de busca.
  - `Disallow: /*&limit` e `Disallow: /*&sort` → **não** use `&limit`/`&sort`. Pagine com `&page=N`.
  - Categoria e página de produto são liberadas.

## Otimização a validar: endpoint JSON (infiniteScroll)

Há indício de que essas lojas têm um endpoint de **infiniteScroll que devolve JSON** com
preço/autor/catálogo **sem parsear HTML**. **Validar primeiro** numa sessão futura (olhar as
requisições XHR de uma categoria ao rolar). Se existir, o crawler fica mais rápido e robusto
(sem regex em HTML). Caso não, seguir com o parse de HTML descrito acima.

## Arquitetura proposta

**Separe catálogo de preço:**
- **Crawl = só catálogo** (isbn, título, autor, editora, slug). Estável, muda pouco.
- **Preço continua ao vivo** via `W16Engine` (preço muda toda hora; não cachear no índice).

### 1. Modelo de dados
Reusar/estender a tabela `books` (já existe — entidade `Book`). Campos mínimos:
`isbn` (unique), `title`, `author`, `publisher`, `slug`, `source_provider`, `updated_at`.

### 2. Índice para autocomplete (Postgres, sem Elasticsearch)
```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX ix_books_title_trgm ON books USING gin (title gin_trgm_ops);
```
(Adicionar via migration EF Core; a extensão `pg_trgm` precisa ser habilitada no banco.)

### 3. Crawler (Business/Infra)
- Enumerar categorias (BFS a partir do menu da home; `route=product/category&path=[0-9_]+`).
- Para cada categoria, paginar `&page=1..N` até uma página sem produtos novos.
- Extrair `product-name href` (título + slug), dedup por slug.
- Para cada slug novo, abrir a página do produto e parsear `ISBN`, `Autor`, `Editora`.
- **Reaproveitar o parser existente** em `CedetSingleSearchHttpClient` (já lida com o HTML dessas
  lojas: seletores CSS/XPath, preço BR, Polly retry).
- **Boa cidadania:** User-Agent honesto identificando o bot; concorrência modesta (~4–6);
  rodar fora de pico; respeitar `robots.txt`; re-crawl incremental (cron semanal/mensal).
- Persistir em `books` (upsert por ISBN).

### 4. Endpoint de sugestão
```
GET /api/books/suggest?q={termo}
→ SELECT isbn, title, author FROM books
  WHERE title ILIKE '%'||$1||'%'         -- usa o índice trigram
  ORDER BY similarity(title, $1) DESC
  LIMIT 10;
```
Público ou autenticado conforme a política da tela. Responde em <20ms para ~8k linhas.

### 5. Fluxo no front (feature de busca por termo — já em andamento pelo Lucas)
```
digita nome → /api/books/suggest (local, instantâneo) → escolhe título
   → app já tem o ISBN → dispara a busca de preço ao vivo (fluxo atual)
```

## Passos para a próxima sessão (ordem sugerida)

1. **Validar o endpoint JSON (infiniteScroll)** — se existir, ajustar o crawler para consumi-lo.
2. Migration EF: garantir campos em `books` + `CREATE EXTENSION pg_trgm` + índice GIN trigram.
3. Implementar o crawler (serviço + comando/endpoint admin para disparar o crawl inicial).
4. Rodar o crawl inicial (~30 min) e conferir a contagem (~8k) e a qualidade (ISBN/título).
5. Implementar `GET /api/books/suggest`.
6. Integrar no front (autocomplete) — coordenar com a feature `results-page`/`search-state` em curso.
7. Agendar re-crawl incremental (cron).

## Referência: script de medição usado

O count foi feito com um crawler BFS em Python (urllib + ThreadPoolExecutor, 6 workers),
extraindo `class="product-name" href="..."` por página e deduplicando por slug, paginando com
`&page` (sem `&limit`, respeitando robots). Resultado: **7.970 produtos únicos / 104 categorias /
438 páginas / 167s**. Reproduzir apontando para uma loja e somando os slugs únicos.
