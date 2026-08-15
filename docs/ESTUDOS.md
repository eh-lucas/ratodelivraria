# Guia de Estudos — ASP.NET Core, EF Core e PostgreSQL no Sherlock

Documento-roteiro para o estudo guiado. Cada módulo segue o mesmo ciclo:

1. **Conceito** — o que é e por que existe (o problema que resolve).
2. **No Sherlock** — onde já está no código (com `arquivo:linha`) ou por que falta.
3. **Prática** — o que vamos implementar/refatorar de verdade no projeto.
4. **Checagem** — perguntas que eu te faço no fim do módulo. Se você responder sem olhar o código, o módulo fechou.

> Regra do estudo: **nada de exemplo genérico de blog**. Todo conceito é aplicado neste projeto ou explicado a partir do que já existe nele.

---

## Estado atual do projeto (diagnóstico)

Legenda: ✅ existe e está razoável · ⚠️ existe mas incompleto/errado · ❌ não existe

### Bloco 1 — ASP.NET Core / C#

| # | Tópico | Status | Onde |
|---|--------|--------|------|
| 1 | Middleware pipeline | ✅ | `Program.cs:130-146` — `CorrelationIdMiddleware` próprio + `GlobalExceptionHandler` (`IExceptionHandler` + `ProblemDetails`), ordem validada empiricamente. **Módulo concluído (sessão 2)** |
| 2 | Dependency Injection | ⚠️ | `Configurator.cs:67-74`, `Sherlock.Data/ServiceCollectionExtensions.cs:15-31`, `Sherlock.Business/ServiceCollectionExtensions.cs:17-40` — **DbContext registrado duas vezes** (bug sutil) |
| 3 | Autenticação JWT | ⚠️ | `Configurator.cs:31-60`, `Sherlock.Api/Services/TokenService.cs` — `ValidateIssuer/Audience = false`, secret em `appsettings.json`, sem refresh token |
| 4 | Authorization policies | ❌ | Só `[Authorize]` cru. `TokenService.cs:31` emite `ClaimTypes.Role` que **ninguém consome** |
| 5 | Filters | ❌ | Duplicação restante: `BookSearch` e `BookSearchPost` são quase idênticas, e o bloco "verifica crédito → executa → consome crédito" se repete 3× (os `try/catch` já saíram no módulo 1) |
| 6 | Versionamento de API | ❌ | Rotas fixas `api/[controller]` |
| 7 | Health checks | ⚠️ | `Program.cs:107-115, 149` — existe, mas endpoint único sem `/ready` vs `/live` e sem response JSON |
| 8 | Caching | ⚠️ | 3 camadas convivendo: `IDistributedCache` (`CacheService.cs`), cache em banco (`QueryRepository.GetCachedQueryAsync`) e nenhum HTTP cache |
| 9 | Background services | ❌ | Nada. Candidato óbvio: refresh de preços e limpeza de `queries` antigas |
| 10 | Rate limiting | ⚠️ | `Program.cs:57-105` — bem feito, mas a policy `authenticated` só é usada no `CartController` (`:33, :102, :162`); busca de livro está sem |
| 11 | Logging estruturado | ✅ | Serilog em `Program.cs:7-19` + `UseSerilogRequestLogging` — falta `CorrelationId` e enrichers |
| 12 | Minimal APIs | ❌ | 100% controllers |
| 13 | OpenAPI/Swagger | ⚠️ | `Program.cs:35, 128-132` — `AddSwaggerGen()` pelado: sem botão de JWT, sem XML docs (os `///` que você já escreveu não aparecem) |

### Bloco 2 — Entity Framework Core

| # | Tópico | Status | Onde |
|---|--------|--------|------|
| 14 | Tracking vs No-tracking | ⚠️ | Só `AuthController.cs:49, 111` usa `AsNoTracking()`. Todos os repositórios de leitura trackeiam sem necessidade |
| 15 | Migrations | ⚠️ | 7 migrations em `Sherlock.Data/Migrations/` + `Migrate()` no startup (`Program.cs:120-126`) — perigoso em produção |
| 16 | Otimização de queries | ⚠️ | `TransactionRepository.cs:19-27` tem `Take()` antes de `Include()` (cartesian explosion) |
| 17 | Loading strategies | ⚠️ | Só eager (`Include`). Sem `Split Query`, sem lazy (o que é bom) |
| 18 | Transactions | ❌ | `CreditService.ConsumeCreditsAsync` (`:51-107`) faz update de saldo + insert de histórico **sem transação** |
| 19 | Índices | ✅ | `SherlockDbContext.cs:40-171` — inclusive índice filtrado em `:106-107` |
| 20 | Performance | ⚠️ | Sem `EnableSensitiveDataLogging` controlado, sem log de query lenta, sem `QuerySplittingBehavior` |
| 21 | Projections | ❌ | Repositórios retornam entidades inteiras; o `.Select()` acontece em memória, no service |
| 22 | Compiled queries | ❌ | Nenhuma. `GetCachedQueryAsync` roda N vezes por busca — candidata perfeita |

### Bloco 3 — PostgreSQL

| # | Tópico | Status | Onde |
|---|--------|--------|------|
| 23 | EXPLAIN ANALYZE | ❌ | Nunca rodado neste banco |
| 24 | Índices compostos | ✅ | `SherlockDbContext.cs:105-107` (`search_isbn, provider_id, queried_at` + filtro parcial) |
| 25 | Normalização | ⚠️ | `queries` guarda `title`/`author` duplicados de `books` (desnormalização não documentada) |
| 26 | Locks | ❌ | Race condition real no saldo de créditos |
| 27 | Isolation levels | ❌ | Tudo em `READ COMMITTED` default, sem consciência disso |
| 28 | CTEs | ❌ | Nenhuma query SQL escrita à mão |
| 29 | Materialized views | ❌ | Candidato: ranking de providers por preço médio |
| 30 | jsonb | ⚠️ | `transactions.input_parameters` e `errors` são `jsonb` (`SherlockDbContext.cs:69-70`) mas **nunca consultados via operadores jsonb** |
| 31 | Tuning básico | ❌ | Postgres em container com config default |

---

## Bloco 1 — ASP.NET Core / C#

### Módulo 1 — Middleware pipeline

**Conceito.** O pipeline é uma cadeia de funções `(HttpContext, next) => Task`. Cada middleware decide se chama o próximo, o que faz antes e o que faz depois. Ordem é tudo: `UseAuthentication` antes de `UseAuthorization`, `UseCors` antes de qualquer coisa que responda, `UseRateLimiter` antes do endpoint.

**No Sherlock.** `Program.cs:128-149`. Ordem atual:
```
Swagger → SerilogRequestLogging → HttpsRedirection → Cors → RateLimiter → Authentication → Authorization → MapControllers
```
Pontos para discutir: por que `UseCors` está *depois* de `UseHttpsRedirection`? O que acontece com o preflight `OPTIONS` quando o rate limiter rejeita? Por que não existe `UseExceptionHandler`?

**Prática.**
- Escrever um `CorrelationIdMiddleware` que gera/propaga `X-Correlation-Id` e injeta no `LogContext` do Serilog (liga direto com o módulo 11).
- Adicionar tratamento global de exceção com `IExceptionHandler` (.NET 8) e **remover** os `try/catch` duplicados do `BookSearchController`.
- Comparar as 3 formas: `Use`, `Run`, `Map`, e classe com `InvokeAsync`.

**Checagem.** O que quebra se `UseAuthorization` vier antes de `UseAuthentication`? Por que `UseExceptionHandler` precisa ser o primeiro? Qual a diferença entre um middleware e um filter (módulo 5)?

---

### Módulo 2 — Dependency Injection

**Conceito.** Três tempos de vida: `Singleton` (uma instância no processo), `Scoped` (uma por request), `Transient` (uma por resolução). O erro clássico é *captive dependency*: um singleton segurando um scoped.

**No Sherlock.** Tudo é `Scoped` — inclusive `W16Engine` e `CartOptimizer`, que são stateless e poderiam ser singleton. E existe um problema real:

```csharp
// Sherlock.Data/ServiceCollectionExtensions.cs:15-22
services.AddDbContext<SherlockDbContext>(...);          // registro 1
services.AddDbContextFactory<SherlockDbContext>(..., ServiceLifetime.Scoped);  // registro 2
```

Duas configurações independentes do mesmo contexto. Vamos entender o que o `AddDbContextFactory` já faz sozinho (ele registra o `DbContext` também) e por que essa duplicação é frágil.

**Prática.**
- Corrigir o registro duplo do DbContext.
- Trocar `Configuration` cru por `IOptions<T>` no `TokenService` (hoje ele lê `IConfiguration` direto — `TokenService.cs:19-23` — e faz `int.Parse` sem validação).
- Criar um teste que quebra de propósito com captive dependency para ver a exceção do validador de escopo.

**Checagem.** Por que `IDbContextFactory` existe se já temos `Scoped`? O que acontece se eu injetar `SherlockDbContext` dentro de um `BackgroundService`?

---

### Módulo 3 — Autenticação JWT

**Conceito.** JWT = header.payload.signature em base64url. O servidor não guarda sessão: valida a assinatura e confia nas claims. Isso implica que **você não consegue revogar um token** sem estado extra.

**No Sherlock.** `Configurator.cs:36-60` e `TokenService.cs`. Problemas concretos:
- `ValidateIssuer = false` e `ValidateAudience = false` (`:44-45`) — mas o `TokenService` **emite** issuer e audience (`:40-41`). Emite e não valida.
- Secret com fallback hardcoded (`Configurator.cs:15`) e o mesmo valor em `appsettings.json`.
- `OnMessageReceived` aceita token via **query string** (`:49-58`) — vaza em log de acesso, histórico do browser, referer.
- Sem refresh token, expiração de 120 min.

**Prática.** Ligar validação de issuer/audience, mover secret pro `.env` (o projeto já tem um), avaliar remoção do token via query string, e implementar refresh token com a tabela `tokens` que **já existe** no domínio (`Sherlock.Domain/Entities/Token.cs`) e nunca foi usada.

**Checagem.** Se o secret vazar, o que um atacante consegue fazer? Por que HMAC (HS256) e não RSA (RS256)? Como você revoga um JWT?

---

### Módulo 4 — Authorization policies

**Conceito.** Autenticação = quem você é. Autorização = o que você pode. Policies são regras nomeadas compostas por *requirements*, avaliadas por *handlers*. Melhor que `[Authorize(Roles = "Admin")]` espalhado porque a regra fica em um lugar só.

**No Sherlock.** Não existe. O token carrega `ClaimTypes.Role` (`TokenService.cs:31`) e nenhum endpoint lê. Todo controller usa `[Authorize]` puro.

**Prática.** Casos reais deste projeto:
- Policy `"HasCredits"` — requirement que checa saldo antes mesmo de entrar no controller (hoje isso é `if` repetido em `BookSearchController.cs:81-94, 171-184, 240-253`).
- Policy `"AdminOnly"` para os endpoints de crédito/bônus.
- Resource-based authorization: usuário só vê a própria `transaction` no histórico.

**Checagem.** Diferença entre claim, role e policy? Onde uma policy roda em relação ao pipeline e aos filters?

---

### Módulo 5 — Filters

**Conceito.** Filters rodam **dentro** do MVC, com acesso ao contexto da action (model, parâmetros, resultado) — coisa que middleware não tem. Ordem: Authorization → Resource → Action → Exception → Result.

**No Sherlock.** Zero filters. O sintoma é visível: `BookSearchController.BookSearch` e `BookSearchPost` são ~90% o mesmo código, e o bloco de "verifica crédito → executa → consome crédito" se repete 3 vezes.

**Prática.**
- `ActionFilter` de validação de ISBN (hoje é `if (string.IsNullOrEmpty(isbn))` em toda action).
- `ActionFilter` de consumo de créditos, eliminando a duplicação pós-busca.
- `ExceptionFilter` vs `IExceptionHandler` do módulo 1 — quando usar cada um.

**Checagem.** Quando escolher middleware, quando escolher filter? Por que um `ExceptionFilter` não pega erro que acontece no middleware?

---

### Módulo 6 — Versionamento de API

**Conceito.** Contrato público muda; clientes não mudam junto. Estratégias: URL (`/api/v1/...`), query string, header, media type.

**No Sherlock.** Não existe. E já existe uma quebra latente: o `BookSearchController` mudou de `title OU isbn` para `isbn obrigatório` — o `CLAUDE.md` ainda documenta a versão antiga. Isso é exatamente o tipo de quebra que versionamento resolve.

**Prática.** Instalar `Asp.Versioning.Mvc`, versionar como `v1`, criar um `v2` do `BookSearch` com contrato diferente, e integrar com o Swagger (múltiplos documentos).

**Checagem.** Qual estratégia você escolheria para o Sherlock e por quê? O que é uma breaking change de verdade?

---

### Módulo 7 — Health checks

**Conceito.** Endpoint que o orquestrador consulta. `/live` = o processo está vivo (reinicia se falhar). `/ready` = está pronto para receber tráfego (tira do load balancer se falhar). Confundir os dois causa reinício em loop.

**No Sherlock.** `Program.cs:107-115` registra Postgres e Redis; `:149` expõe um `/health` único. O `docker-compose.yml` tem healthcheck de container.

**Prática.** Separar `/health/live` e `/health/ready` por tags, escrever um health check **customizado** que testa se os providers de scraping estão respondendo (usa `Degraded`, não `Unhealthy`), e formatar a resposta em JSON.

**Checagem.** Por que um health check de banco não deve estar no `/live`? O que significa `Degraded`?

---

### Módulo 8 — Caching

**Conceito.** Camadas: HTTP (`Cache-Control`/ETag), output cache, in-memory, distribuído. Cada uma resolve um problema diferente. O difícil nunca é cachear — é **invalidar** e escolher a chave.

**No Sherlock.** Aqui tem um assunto bom: existem **duas** estratégias competindo.
- `CacheService` (`Sherlock.Business/Services/CacheService.cs`) — `IDistributedCache`, chave `book:price:{sha256}:provider:{id}`, TTL 2h.
- Cache em banco — `QueryRepository.GetCachedQueryAsync` (`:79-93`) consulta a tabela `queries` dentro da janela de `QueryCache:DefaultCacheTimeMinutes` (30 min).

TTLs diferentes, fontes de verdade diferentes. A regra do `CLAUDE.md` ("transaction não cacheia, query cacheia") está certa, mas a implementação está duplicada.

**Prática.** Decidir uma estratégia, medir hit rate com log estruturado, e adicionar `OutputCache` no `GET /api/Providers` (dado estático que hoje bate no banco a cada request).

**Checagem.** Por que o cache é por `livro+provider` e não por transação? O que é cache stampede e como o Sherlock está exposto a ele?

---

### Módulo 9 — Background services

**Conceito.** `BackgroundService` / `IHostedService` rodam fora do request. Cuidados: são **singleton**, então nada de injetar `Scoped` direto (precisa de `IServiceScopeFactory`); e precisam respeitar `CancellationToken` no shutdown.

**No Sherlock.** Não existe, e há três candidatos reais:
1. Refresh periódico de preços dos livros mais buscados (aquece o cache).
2. Limpeza de `queries` antigas (a tabela só cresce).
3. Health probe dos 93 providers, alimentando a flag `IsActive`.

**Prática.** Implementar o #2 com `PeriodicTimer`, escopo correto, log estruturado e shutdown limpo. Depois discutir por que Hangfire/Quartz existem se `BackgroundService` já resolve.

**Checagem.** Por que não posso injetar `SherlockDbContext` no construtor de um `BackgroundService`? O que acontece se o `ExecuteAsync` lançar exceção?

---

### Módulo 10 — Rate limiting

**Conceito.** Quatro algoritmos nativos no .NET 8: fixed window, sliding window, token bucket, concurrency. Diferença prática: fixed window sofre com o problema da borda (2× o limite na virada da janela); token bucket permite rajada controlada.

**No Sherlock.** `Program.cs:57-105`. Já está bem feito — token bucket por usuário + fixed window global por IP, com `OnRejected` customizado. Dois problemas:
- A policy `authenticated` só é aplicada no `CartController` (`:33, :102, :162`). `BookSearchController`, que dispara **93 requisições HTTP externas** por chamada, está apenas no limite global.
- O `partitionKey` cai para IP quando a claim `sub` não é encontrada — e o `BookSearchController.GetUserId()` (`:39`) lê `ClaimTypes.NameIdentifier`, enquanto o rate limiter procura `"sub"`. Vale conferir se estão de fato lendo a mesma coisa.

**Prática.** Aplicar a policy nos endpoints de busca, escrever um teste de integração que estoura o limite, e discutir rate limit por *custo* (93 providers deveria custar mais que 3).

**Checagem.** Por que token bucket para usuário e fixed window para IP? O que o `QueueLimit` faz?

---

### Módulo 11 — Logging estruturado

**Conceito.** Log estruturado = evento com propriedades tipadas, não string concatenada. `_logger.LogInformation("Busca {Isbn} levou {Ms}ms", isbn, ms)` permite filtrar por `Isbn` depois. Concatenar com `$""` destrói isso.

**No Sherlock.** ✅ Bem feito. Serilog configurado em `Program.cs:7-19`, console + arquivo rotativo com 30 dias, overrides por namespace, `UseSerilogRequestLogging` com template customizado (`:134-137`). O código usa template properties corretamente (ex: `CreditService.cs:96-98`).

O que falta: `Enrich.WithMachineName()`/`WithThreadId()` (os pacotes **já estão instalados** no `.csproj` e não são usados), `CorrelationId`, e nenhum sink estruturado (Seq/Elastic) — o arquivo é texto plano.

**Prática.** Ligar os enrichers, plugar o `CorrelationIdMiddleware` do módulo 1 via `LogContext.PushProperty`, e subir um Seq no compose para ver a diferença entre ler log em texto e consultar log estruturado.

**Checagem.** Por que `LogInformation($"user {id}")` é um bug e não só um estilo ruim? O que é log scope?

---

### Módulo 12 — Minimal APIs

**Conceito.** Endpoints como delegates, sem classe de controller. Menos cerimônia, menos overhead de model binding, e um modelo diferente de filters (`AddEndpointFilter`).

**No Sherlock.** Tudo é controller. Bom candidato: `ProvidersController` (`:17, :39`) — duas actions que só projetam uma lista estática, sem estado nenhum.

**Prática.** Reescrever `ProvidersController` como minimal API com `MapGroup`, `TypedResults` e endpoint filter. Comparar lado a lado: legibilidade, testabilidade, suporte a OpenAPI.

**Checagem.** Quando controller ainda ganha de minimal API? Como validação funciona sem `[ApiController]`?

---

### Módulo 13 — OpenAPI/Swagger

**Conceito.** OpenAPI é a especificação; Swagger UI é uma das ferramentas que a consome. Um spec bom é contrato executável: gera cliente, valida request, documenta erro.

**No Sherlock.** `AddSwaggerGen()` sem configuração (`Program.cs:35`). Consequências:
- Os comentários `///` que você já escreveu (`BookSearchController.cs:50-55`, etc.) **não aparecem** — falta `<GenerateDocumentationFile>` e `IncludeXmlComments`.
- Não há botão "Authorize", então nenhum endpoint protegido é testável pela UI.
- Os `[ProducesResponseType]` (`:57-60`) estão lá e funcionam — bom ponto de partida.

**Prática.** Configurar XML docs, security definition JWT, exemplos de request, e integrar com o versionamento do módulo 6.

**Checagem.** Por que `[ProducesResponseType]` importa se o endpoint já retorna o status certo?

---

## Bloco 2 — Entity Framework Core

### Módulo 14 — Tracking vs No-tracking

**Conceito.** Por padrão o `DbContext` guarda um snapshot de toda entidade lida para detectar mudanças no `SaveChanges`. Em leitura pura isso é custo puro: memória + tempo de fixup do change tracker. `AsNoTracking()` desliga; `AsNoTrackingWithIdentityResolution()` desliga mas mantém identidade de referência.

**No Sherlock.** Só o `AuthController` usa (`:49, :111`). Todo o resto trackeia — inclusive `TransactionRepository.GetByUserIdAsync`, que carrega N transactions × M queries só para mapear pra DTO.

**Prática.** Medir com `dotnet-counters` ou stopwatch o histórico de transações antes/depois. Discutir por que **não** basta setar `QueryTrackingBehavior.NoTracking` global.

---

### Módulo 15 — Migrations

**Conceito.** Migration é diff versionado do modelo. O `ModelSnapshot` é a fonte de verdade do "estado anterior". Migration gerada não é migration correta: rename vira drop+create, e você perde dados.

**No Sherlock.** 7 migrations em `Sherlock.Data/Migrations/`. Duas coisas para discutir:
- `Program.cs:120-126` roda `db.Database.Migrate()` no startup. Conveniente em dev, **perigoso** com múltiplas réplicas (duas instâncias migrando ao mesmo tempo).
- `UseSnakeCaseNaming` (migration `20251125005946`) — como uma convenção de nomes vira DDL.

**Prática.** Gerar um script idempotente (`dotnet ef migrations script --idempotent`), escrever uma migration manual de rename preservando dados, e mover a migração do startup para o pipeline de deploy.

---

### Módulo 16 — Otimização de queries

**Conceito.** Os pecados: N+1, cartesian explosion, filtro em memória, `Count()` desnecessário, e client-side evaluation silencioso.

**No Sherlock.** Bug concreto:
```csharp
// TransactionRepository.cs:19-27
.Where(t => t.UserId == userId)
.OrderByDescending(t => t.StartedAt)
.Take(limit)          // ← Take ANTES do Include
.Include(t => t.Queries)
```
Com `Include` de coleção, o EF gera um JOIN: 20 transactions × ~93 queries cada = ~1860 linhas trafegadas, com todas as colunas da transaction repetidas. Vamos ver o SQL gerado e medir.

**Prática.** Capturar SQL via `LogTo`, corrigir com `AsSplitQuery()` ou projeção, medir antes/depois.

---

### Módulo 17 — Loading strategies

**Conceito.** Eager (`Include`), explicit (`.Collection().LoadAsync()`), lazy (proxies), split query. Cada uma troca "número de roundtrips" por "volume de dados".

**No Sherlock.** Só eager. `TransactionRepository.GetWithQueriesAsync` (`:38-45`) faz `Include(Queries).ThenInclude(Provider).Include(BestQuery)` — três níveis num único JOIN.

**Prática.** Comparar os três modos na mesma query, com SQL e tempo. Entender por que lazy loading é armadilha em API (serialização dispara queries).

---

### Módulo 18 — Transactions

**Conceito.** `SaveChanges` já é transacional. O problema é quando **duas** operações precisam ser atômicas. Aí entra `BeginTransactionAsync`, savepoints e `TransactionScope`.

**No Sherlock.** Bug real em `CreditService.ConsumeCreditsAsync` (`:76-94`):
```
1. lê saldo
2. UpdateUserCreditsAsync(novo saldo)   ← SaveChanges #1
3. AddCreditTransactionAsync(histórico) ← SaveChanges #2
```
Se o passo 3 falhar, o saldo foi debitado sem registro. E o passo 1→2 é read-modify-write sem lock — duas buscas simultâneas do mesmo usuário debitam em cima do mesmo saldo lido. Este é o gancho direto para os módulos 26 e 27.

**Prática.** Envolver em transação explícita; depois resolver a corrida de três formas (UPDATE atômico, `SELECT FOR UPDATE`, concorrência otimista com `xmin`) e comparar.

---

### Módulo 19 — Índices (EF)

**Conceito.** Como declarar índice no modelo, quando o EF cria sozinho (FK), e por que índice único no modelo ≠ constraint no banco.

**No Sherlock.** ✅ Melhor parte do projeto. `SherlockDbContext.cs` — índice composto com filtro parcial em `:106-107`, único em `:59` e `:129`, e cobertura de FK. Vamos validar se os índices declarados são os que as queries realmente usam (liga com o módulo 23).

---

### Módulo 20 — Performance (EF)

**Conceito.** Ferramentas: `LogTo` com `LogLevel.Information`, interceptors, `EnableDetailedErrors`, connection pooling (`AddDbContextPool`), e batching de `SaveChanges`.

**No Sherlock.** Nada disso. `AddQueriesAsync` (`QueryRepository.cs:60-73`) já usa `AddRangeAsync` + um `SaveChanges` — batching correto, vale entender o SQL que sai.

**Prática.** Escrever um `DbCommandInterceptor` que loga toda query acima de 100ms como Warning estruturado.

---

### Módulo 21 — Projections

**Conceito.** `Select` para DTO faz o SQL trazer só as colunas necessárias, dispensa tracking e elimina o `Include`. É a otimização com melhor relação esforço/ganho no EF.

**No Sherlock.** Padrão invertido: os repositórios trazem entidades inteiras e o `Select` acontece **em memória**, no service (`QueryHistoryService.cs:81, 136`; `CreditService.cs:219`). O `CreditRepository.cs:21` é o contraexemplo correto — projeta `AvailableCredits` direto no SQL.

**Prática.** Reescrever o histórico de transações com projeção, comparar o SQL e o payload.

---

### Módulo 22 — Compiled queries

**Conceito.** `EF.CompileAsyncQuery` cacheia a árvore de expressão traduzida. Ganho real só em query executada milhares de vezes, porque elimina o custo de tradução — não o custo do banco.

**No Sherlock.** Candidata perfeita: `GetCachedQueryAsync` (`QueryRepository.cs:79-93`) roda **uma vez por provider por busca** — até 93 execuções da mesma query por request.

**Prática.** Compilar, benchmarkar com BenchmarkDotNet, e entender quando o ganho não compensa a rigidez.

---

## Bloco 3 — PostgreSQL

### Módulo 23 — EXPLAIN ANALYZE

**Conceito.** `EXPLAIN` mostra o plano estimado; `EXPLAIN ANALYZE` executa e mostra o real. O que importa: nós (Seq Scan, Index Scan, Bitmap Heap Scan, Nested Loop, Hash Join), `rows` estimado vs real (divergência grande = estatística ruim), e `Buffers` (`EXPLAIN (ANALYZE, BUFFERS)`).

**No Sherlock.** Vamos rodar direto no container:
```bash
docker exec sherlock-postgres psql -U sherlock_admin -d sherlock_dev_db -c "EXPLAIN (ANALYZE, BUFFERS) <query>"
```
Alvos: a query de cache por ISBN, o histórico com JOIN de queries, e o agrupamento por provider.

**Prática.** Popular a tabela `queries` com volume (100k+ linhas), rodar EXPLAIN antes e depois de índice, e ler o plano juntos linha a linha.

---

### Módulo 24 — Índices compostos

**Conceito.** Regra da esquerda para a direita: um índice em `(a, b, c)` serve para `a`, `a,b`, `a,b,c` — nunca só para `b`. Ordem das colunas manda: igualdade antes de range. Índice parcial (`WHERE`) reduz tamanho. Covering index (`INCLUDE`) permite Index Only Scan.

**No Sherlock.** ✅ `SherlockDbContext.cs:105-107` já faz certo:
```sql
CREATE INDEX ... ON queries (search_isbn, provider_id, queried_at) WHERE search_isbn IS NOT NULL
```
Igualdade (`search_isbn`, `provider_id`) antes do range (`queried_at`), com filtro parcial. Vamos confirmar no EXPLAIN que a query de cache realmente usa esse índice — e testar se um `INCLUDE (price, title)` transforma em Index Only Scan.

---

### Módulo 25 — Normalização

**Conceito.** 1FN/2FN/3FN, e quando desnormalizar de propósito. A pergunta certa não é "está normalizado?", é "qual anomalia de atualização isso cria e eu aceito?".

**No Sherlock.** `queries` guarda `title`, `author`, `price` — dados que também vivem em `books`/`book_prices`. Isso é **correto**: a query é um snapshot histórico do que o provider retornou naquele instante. Mas não está documentado como decisão. Vamos formalizar isso e olhar `transactions.errors` (jsonb) sob a mesma lente.

---

### Módulo 26 — Locks

**Conceito.** Row locks (`FOR UPDATE`, `FOR NO KEY UPDATE`, `FOR SHARE`), table locks, advisory locks. Deadlock acontece quando duas transações pegam os mesmos locks em ordem diferente — a solução quase sempre é ordenar os acessos.

**No Sherlock.** O saldo de créditos (módulo 18). Vamos reproduzir a corrida de verdade: duas requisições paralelas de busca do mesmo usuário, e observar o saldo ficar errado. Depois comparar:
```sql
-- opção A: UPDATE atômico (sem lock explícito)
UPDATE users SET available_credits = available_credits - $1 WHERE id = $2 AND available_credits >= $1;

-- opção B: lock pessimista
SELECT available_credits FROM users WHERE id = $1 FOR UPDATE;
```

**Prática.** Reproduzir com duas sessões `psql` lado a lado, inspecionar `pg_locks`, provocar um deadlock de propósito.

---

### Módulo 27 — Isolation levels

**Conceito.** Os quatro níveis do padrão e as anomalias que cada um permite: dirty read, non-repeatable read, phantom read. Particularidade do Postgres: `READ UNCOMMITTED` não existe (vira `READ COMMITTED`), e `REPEATABLE READ` já impede phantom (é snapshot isolation). `SERIALIZABLE` usa SSI e pode **abortar** transação com erro 40001 — sua aplicação precisa saber fazer retry.

**No Sherlock.** Tudo em `READ COMMITTED` por default, sem consciência disso. O consumo de créditos é o caso onde isso importa.

**Prática.** Duas sessões `psql`, reproduzir cada anomalia manualmente, subir o nível e ver o comportamento mudar. Depois configurar isolation level no EF Core e implementar retry de erro de serialização.

---

### Módulo 28 — CTEs

**Conceito.** `WITH ... AS (...)` para decompor query complexa. `WITH RECURSIVE` para hierarquia. Detalhe importante: desde o PG 12 a CTE não é mais optimization fence por padrão (usa `MATERIALIZED` para forçar).

**No Sherlock.** Nenhuma. Alvo natural: a lógica do `CartOptimizer.cs` — hoje ela busca tudo e agrupa **em C#** (`:29, :71, :180`). A mesma coisa em SQL com CTE + window function seria drasticamente menos dados trafegados.

**Prática.** Reescrever "melhor provider para um carrinho" como uma query só, com CTE e `ROW_NUMBER() OVER (PARTITION BY ...)`. Comparar com a versão em C#.

---

### Módulo 29 — Materialized views

**Conceito.** View comum é macro (executa sempre); materialized view guarda o resultado em disco. `REFRESH MATERIALIZED VIEW CONCURRENTLY` não bloqueia leitura, mas exige índice único.

**No Sherlock.** Candidatos: ranking de providers (preço médio, taxa de sucesso, tempo de resposta) e livros mais buscados. Hoje isso exigiria varrer `queries` inteira a cada dashboard.

**Prática.** Criar a MV, indexar, agendar o refresh no `BackgroundService` do módulo 9 — os dois conceitos se encontram aqui.

---

### Módulo 30 — jsonb

**Conceito.** `json` guarda texto literal; `jsonb` guarda binário parseado — mais lento para gravar, muito mais rápido para consultar, e indexável com GIN. Operadores: `->`, `->>`, `@>`, `?`, `#>`. Índice GIN com `jsonb_path_ops` é menor e mais rápido para `@>`.

**No Sherlock.** ⚠️ `transactions.input_parameters` e `transactions.errors` são `jsonb` (`SherlockDbContext.cs:69-70`) e são gravados como string serializada (`TransactionPersistenceService.cs:21`) — nunca consultados por operador jsonb. É jsonb usado como se fosse `text`.

**Prática.** Escrever queries de verdade: "todas as transações que tiveram erro de timeout", "buscas por um ISBN específico" via `input_parameters->>'isbn'`. Criar índice GIN, medir. Depois mapear isso no EF Core com `EF.Functions.JsonContains`.

---

### Módulo 31 — Tuning básico

**Conceito.** Os parâmetros que importam de verdade: `shared_buffers` (~25% da RAM), `work_mem` (por operação de sort/hash, cuidado com multiplicação), `effective_cache_size` (dica pro planner), `max_connections` vs pooler, `random_page_cost` (4 é para HD; SSD quer ~1.1). E `autovacuum` — a causa mais comum de degradação lenta.

**No Sherlock.** Postgres em container com config default e sem tuning nenhum.

**Prática.** Ler a config atual, habilitar `pg_stat_statements`, encontrar as queries mais caras do projeto por tempo total, e ajustar dois ou três parâmetros medindo o efeito. Discutir bloat e `VACUUM` na tabela `queries`, que só cresce.

---

## Registro de progresso

- [x] 01 — Middleware pipeline · **concluído** (sessões 1 e 2)
- [ ] 02 — Dependency Injection
- [ ] 03 — Autenticação JWT
- [ ] 04 — Authorization policies
- [ ] 05 — Filters
- [ ] 06 — Versionamento de API
- [ ] 07 — Health checks
- [ ] 08 — Caching
- [ ] 09 — Background services
- [ ] 10 — Rate limiting
- [ ] 11 — Logging estruturado
- [ ] 12 — Minimal APIs
- [ ] 13 — OpenAPI/Swagger
- [ ] 14 — Tracking vs No-tracking
- [ ] 15 — Migrations
- [ ] 16 — Otimização de queries
- [ ] 17 — Loading strategies
- [ ] 18 — Transactions
- [ ] 19 — Índices (EF)
- [ ] 20 — Performance (EF)
- [ ] 21 — Projections
- [ ] 22 — Compiled queries
- [ ] 23 — EXPLAIN ANALYZE
- [ ] 24 — Índices compostos
- [ ] 25 — Normalização
- [ ] 26 — Locks
- [ ] 27 — Isolation levels
- [ ] 28 — CTEs
- [ ] 29 — Materialized views
- [ ] 30 — jsonb
- [ ] 31 — Tuning básico

---

## Diário de sessões

### Sessão 1 — 2026-08-12 · Módulo 1 (middleware pipeline)

**Formato acordado.** Panorama do módulo → OK do Lucas → condução passo a passo (Claude explica o que e o porquê, Lucas escreve o código) → resumo no fim.

**Conceitos cobertos.**
- Pipeline como cadeia de funções aninhadas; a ordem de registro define quem é o mais externo.
- `Use` / `Run` / `Map`; curto-circuito; o `await next()` dividindo o método em ida e volta.
- Middleware por convenção (duck typing: ctor com `RequestDelegate` + `InvokeAsync`) vs. middleware inline.
- Middleware é **singleton**: nada de service `Scoped` no construtor (vai como parâmetro do `InvokeAsync`) e nada de estado em campo.
- `AsyncLocal` por trás do `LogContext` — é o que faz o escopo alcançar as Tasks paralelas dos scrapers.
- Auditoria da ordem atual do `Program.cs`: `UseCors` antes de `UseRateLimiter` é o que faz o 429 chegar legível no Angular; `UseHttpsRedirection` antes de `UseCors` funciona por acidente (a API só escuta HTTP).

**Feito.**
- `Sherlock.Api/Middleware/CorrelationIdMiddleware.cs` — reaproveita ou gera `X-Correlation-Id`, valida o valor recebido (log forging via `\r\n`, limite de 64 chars), seta `TraceIdentifier`, escreve o header via `Response.OnStarting` e empurra a propriedade no `LogContext`.
- Registrado como primeiro do pipeline em `Program.cs:130`.
- `dotnet build` passando.

**Parado em.** Validação manual com `curl.exe -i http://localhost:5177/health` (o `curl` do PowerShell é alias de `Invoke-WebRequest` e não aceita `-i`). Falta confirmar o header na resposta e o `CorrelationId` no arquivo de log.

**Próximos passos do módulo.**
1. Rodar a validação manual acima.
2. `GlobalExceptionHandler` com `IExceptionHandler` + `AddProblemDetails` (.NET 8).
3. Decidir a ordem dos dois middlewares — **o `CorrelationIdMiddleware` precisa vir antes do `UseExceptionHandler`**, contrariando a recomendação genérica da documentação: o `using` do `LogContext` é descartado quando a exceção sobe por ele, então o handler só enxerga o `CorrelationId` se estiver dentro daquele escopo.
4. Remover os `try/catch` duplicados do `BookSearchController` (`:123-128`, `:213-218`) — hoje vazam `ex.Message` e transformam `UnauthorizedAccessException` em 500.

**Notas soltas.**
- O template de console do Serilog (`Program.cs:15`) não tem `{Properties:j}`; só o de arquivo (`:19`) tem. Propriedades estruturadas só aparecem em `logs/sherlock-AAAAMMDD.log`.
- Shell é PowerShell: `curl.exe` (não `curl`), `Get-Content -Tail N` (não `tail`).

---

### Sessão 2 — 2026-08-15 · Módulo 1 (encerramento)

**Validação do `CorrelationIdMiddleware`.** Quatro checagens com `curl.exe -i` contra `/health`: gera id quando o cliente não manda, reaproveita o id recebido, rejeita valor acima de `MaxIdLength`, e a propriedade aparece no arquivo de log.

Dois aprendizados laterais:
- O log **não** fica em `logs/` na raiz da solução, e sim em `Sherlock.Api/logs/` — caminho relativo resolve contra o *working directory do processo*, que o `dotnet run --project` fixa na pasta do projeto. Mesma armadilha em container (`WORKDIR`) e em serviço do Windows (`System32`).
- O `[WRN] Failed to determine the https port` do `HttpsRedirectionMiddleware` — código da Microsoft, não nosso — sai **com** `CorrelationId`. É a prova prática do `AsyncLocal`: tudo que loga dentro do `await _next(context)` herda o escopo.
- A checagem de tamanho passou por engano na primeira tentativa: `$('a' * 100)` chegou literal ao header. Falso positivo — refeita com a variável em separado.

**Conceitos cobertos.**
- `IExceptionHandler` **não é middleware**: roda *dentro* do `ExceptionHandlerMiddleware`, não recebe `RequestDelegate`, e o retorno `bool` implementa cadeia de responsabilidade (`true` = tratei, `false` = passa ao próximo).
- Primary constructor, switch *expression* (vs statement), type pattern, tupla + desconstrução.
- `AddExceptionHandler<T>` registra o handler como **singleton** — injetar `Scoped` ali quebra no startup (captive dependency, gancho para o módulo 2).
- `ProblemDetails` / RFC 7807, `application/problem+json`, `Extensions` como campo livre, `IProblemDetailsService.TryWriteAsync`.
- Vazamento de informação: `ex.Message` cru na resposta expõe `NpgsqlException` (nome de tabela) e `HttpRequestException` (URL interna de provider). Resolvido com `Detail` condicional a `IsDevelopment()`.

**Descoberta empírica sobre ordem.** Com o `UseExceptionHandler` *por fora* do `UseSerilogRequestLogging`, o log de acesso registrava **500 em requests que respondiam 400** — o Serilog fecha o log quando a exceção passa por ele, antes de alguém decidir o status. Invertido. Regra que ficou: **logo por dentro de quem precisa observar a resposta, logo por fora de quem pode falhar** — vale mais que o "exception handler primeiro" da documentação.

**Feito.**
- `Sherlock.Api/Middleware/GlobalExceptionHandler.cs` — mapeia `UnauthorizedAccessException`→401, `ArgumentException`→400, `OperationCanceledException`→408, resto→500; nível de log proporcional ao status (`Error` só para 5xx); `traceId` nas extensions vindo do `TraceIdentifier`.
- Pipeline final: `CorrelationId` → Swagger → `SerilogRequestLogging` → `ExceptionHandler` → HttpsRedirection → Cors → RateLimiter → Auth.
- **12 `try/catch` genéricos removidos** de 5 controllers (−143 linhas líquidas). Critério: `catch` que só re-embrulha erro genérico sai; `catch` com semântica de domínio fica (`InvalidOperationException`→404 no `UserController`). O `OperationCanceledException`→408 do `CartController` subiu para o handler por ser genérico o bastante.
- Endpoints `/boom/*` usados na validação foram removidos.

**Pendências que este módulo gerou.**
1. O `catch` do `BookSearchController` logava `"Erro ao buscar preços para ISBN {Isbn}"`. O handler é genérico e não conhece ISBN — no POST o valor sumiu do log. Solução no **módulo 11**: `LogContext.PushProperty`/`BeginScope` na action (é a pergunta de checagem "o que é log scope").
2. `GetSelectedProviders` (`BookSearchController.cs:291`) devolve `null` como código de erro. Candidato a `ArgumentException` explícita + filter de validação no **módulo 5**.
3. O `ExceptionHandlerMiddleware` da Microsoft loga `An unhandled exception has occurred` antes de chamar o handler — stack trace duplicada no arquivo. Mantido de propósito: é a rede de segurança caso o handler retorne `false` ou falhe.

**Checagem do módulo (respondida).** Diferença entre middleware e `IExceptionHandler`; por que `UseAuthorization` depois de `UseAuthentication`; por que a ordem do exception handler depende de quem precisa ler a resposta.

---

## Bugs achados no diagnóstico

Encontrados ao mapear os conceitos. Cada um vira exercício no módulo indicado — mas são problemas reais, não didáticos.

| Severidade | Problema | Onde | Módulo |
|---|---|---|---|
| 🔴 | Consumo de créditos sem transação nem lock — saldo pode ficar inconsistente | `CreditService.cs:76-94` | 18, 26, 27 |
| 🔴 | Secret JWT hardcoded como fallback e commitado em `appsettings.json` | `Configurator.cs:15`, `appsettings.json` | 3 |
| 🟡 | Token JWT aceito via query string | `Configurator.cs:49-58` | 3 |
| 🟡 | Emite issuer/audience mas não valida | `Configurator.cs:44-45` vs `TokenService.cs:40-41` | 3 |
| 🟡 | DbContext registrado duas vezes com configs independentes | `Sherlock.Data/ServiceCollectionExtensions.cs:15-22` | 2 |
| 🟡 | `Take()` antes de `Include()` — carrega ~1860 linhas para retornar 20 | `TransactionRepository.cs:19-27` | 16 |
| 🟡 | `Migrate()` no startup — race entre réplicas | `Program.cs:120-126` | 15 |
| 🟡 | Busca de livro (93 req externas) sem policy de rate limit | `BookSearchController.cs` | 10 |
| 🟢 | Duas estratégias de cache com TTLs diferentes | `CacheService.cs` vs `QueryRepository.cs:79` | 8 |
| 🟢 | Swagger sem XML docs e sem auth — `///` já escritos não aparecem | `Program.cs:35` | 13 |
| 🟢 | `int.Parse` sem validação em config de expiração do token | `TokenService.cs:23` | 2 |
| 🟢 | Duas connection strings idênticas com nomes diferentes | `appsettings.json` | 2 |
| 🟢 | jsonb usado como text — nunca consultado por operador | `SherlockDbContext.cs:69-70` | 30 |
