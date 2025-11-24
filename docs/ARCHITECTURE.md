# Arquitetura do Sistema BookPricesWatcher

## 1. Visão Geral

O BookPricesWatcher é um sistema de comparação de preços de livros que permite ao usuário inserir uma lista de livros e receber a **melhor combinação de sites para compra com o menor custo total**.

### Objetivo Principal
Otimizar a compra de múltiplos livros considerando:
- Preço individual de cada livro
- Promoções e descontos por loja
- Custo de frete (por loja e combinado)
- Divisão inteligente entre múltiplos sites

---

## 2. Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                                   FRONTEND                                       │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        Angular 20 (SPA)                                  │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │   │
│  │  │  Login   │  │ Register │  │   Home   │  │  Search  │  │ Results  │  │   │
│  │  │   Page   │  │   Page   │  │   Page   │  │   Page   │  │   Page   │  │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘  │   │
│  │                              │                                          │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐   │   │
│  │  │ Services: AuthService | BookSearchService | CartOptimizerService │   │   │
│  │  └─────────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        │ HTTP/REST + JWT
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                                  API GATEWAY                                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                      ASP.NET Core 8.0 Web API                            │   │
│  │                                                                          │   │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────────────┐ │   │
│  │  │    Auth    │  │   Books    │  │  Search    │  │   CartOptimizer    │ │   │
│  │  │ Controller │  │ Controller │  │ Controller │  │    Controller      │ │   │
│  │  └────────────┘  └────────────┘  └────────────┘  └────────────────────┘ │   │
│  │                                                                          │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │   │
│  │  │ Middlewares: Auth | RateLimiting | Logging | ErrorHandling      │    │   │
│  │  └─────────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              BUSINESS LAYER                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                          │   │
│  │  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐   │   │
│  │  │   W16Engine      │    │  CartOptimizer   │    │   CacheManager   │   │   │
│  │  │  (Orquestrador)  │    │    (Algoritmo)   │    │     (Redis)      │   │   │
│  │  └────────┬─────────┘    └────────┬─────────┘    └────────┬─────────┘   │   │
│  │           │                       │                       │              │   │
│  │  ┌────────▼─────────┐    ┌────────▼─────────┐    ┌────────▼─────────┐   │   │
│  │  │  ScraperFactory  │    │  FreightCalc     │    │   CostCalc       │   │   │
│  │  │                  │    │                  │    │                  │   │   │
│  │  └────────┬─────────┘    └──────────────────┘    └──────────────────┘   │   │
│  │           │                                                              │   │
│  │  ┌────────▼───────────────────────────────────────────────────────────┐ │   │
│  │  │                        SCRAPERS (IScraper)                          │ │   │
│  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  │ │   │
│  │  │  │  Cedet  │  │ Amazon  │  │ Estante │  │   ML    │  │ Generic │  │ │   │
│  │  │  │ Scraper │  │ Scraper │  │ Virtual │  │ Scraper │  │ Scraper │  │ │   │
│  │  │  └─────────┘  └─────────┘  └─────────┘  └─────────┘  └─────────┘  │ │   │
│  │  └────────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               DATA LAYER                                         │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                          │   │
│  │  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐   │   │
│  │  │   PostgreSQL     │    │      Redis       │    │   Elasticsearch  │   │   │
│  │  │   (Principal)    │    │     (Cache)      │    │     (Search)     │   │   │
│  │  │                  │    │                  │    │                  │   │   │
│  │  │  • Users         │    │  • Price Cache   │    │  • Book Search   │   │   │
│  │  │  • Books         │    │  • Session       │    │  • Full-text     │   │   │
│  │  │  • BookPrices    │    │  • Rate Limit    │    │  • Autocomplete  │   │   │
│  │  │  • Providers     │    │                  │    │                  │   │   │
│  │  │  • Queries       │    │                  │    │                  │   │   │
│  │  │  • Transactions  │    │                  │    │                  │   │   │
│  │  └──────────────────┘    └──────────────────┘    └──────────────────┘   │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            INFRAESTRUTURA                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                                                                          │   │
│  │  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐   │   │
│  │  │    Serilog       │    │    Prometheus    │    │     Jaeger       │   │   │
│  │  │    (Logging)     │    │    (Metrics)     │    │    (Tracing)     │   │   │
│  │  └──────────────────┘    └──────────────────┘    └──────────────────┘   │   │
│  │                                                                          │   │
│  │  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐   │   │
│  │  │    Hangfire      │    │      Polly       │    │   HealthChecks   │   │   │
│  │  │  (Background)    │    │    (Resilience)  │    │                  │   │   │
│  │  └──────────────────┘    └──────────────────┘    └──────────────────┘   │   │
│  │                                                                          │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Fluxo de Consulta Passo a Passo

### 3.1 Fluxo: Busca de Lista de Livros com Otimização de Carrinho

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  Usuário │     │ Frontend │     │   API    │     │ Business │     │   Data   │
└────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘
     │                │                │                │                │
     │ 1. Insere lista│                │                │                │
     │    de livros   │                │                │                │
     │───────────────>│                │                │                │
     │                │                │                │                │
     │                │ 2. POST        │                │                │
     │                │ /api/cart/     │                │                │
     │                │ optimize       │                │                │
     │                │───────────────>│                │                │
     │                │                │                │                │
     │                │                │ 3. Valida JWT  │                │
     │                │                │ Rate Limit     │                │
     │                │                │────────────────│                │
     │                │                │                │                │
     │                │                │ 4. Chama       │                │
     │                │                │ CartOptimizer  │                │
     │                │                │───────────────>│                │
     │                │                │                │                │
     │                │                │                │ 5. Verifica    │
     │                │                │                │    Cache       │
     │                │                │                │───────────────>│
     │                │                │                │                │
     │                │                │                │ 6. Cache Miss: │
     │                │                │                │    W16Engine   │
     │                │                │                │    executa     │
     │                │                │                │────────────────│
     │                │                │                │                │
     │                │                │                │ 7. Para cada   │
     │                │                │                │    livro:      │
     │                │                │                │    ScraperFactory
     │                │                │                │    cria scrapers
     │                │                │                │────────────────│
     │                │                │                │                │
     │                │                │                │ 8. Scrapers    │
     │                │                │                │    buscam em   │
     │                │                │                │    paralelo    │
     │                │                │                │────────────────│
     │                │                │                │                │
     │                │                │                │ 9. Resultados  │
     │                │                │                │    agregados   │
     │                │                │                │<───────────────│
     │                │                │                │                │
     │                │                │                │ 10. Algoritmo  │
     │                │                │                │     otimização │
     │                │                │                │     (frete +   │
     │                │                │                │     preço)     │
     │                │                │                │────────────────│
     │                │                │                │                │
     │                │                │                │ 11. Salva      │
     │                │                │                │     cache +    │
     │                │                │                │     histórico  │
     │                │                │                │───────────────>│
     │                │                │                │                │
     │                │                │ 12. Retorna    │                │
     │                │                │     resultado  │                │
     │                │                │<───────────────│                │
     │                │                │                │                │
     │                │ 13. JSON       │                │                │
     │                │     Response   │                │                │
     │                │<───────────────│                │                │
     │                │                │                │                │
     │ 14. Exibe      │                │                │                │
     │     melhor     │                │                │                │
     │     combinação │                │                │                │
     │<───────────────│                │                │                │
     │                │                │                │                │
```

### 3.2 Detalhamento do Fluxo

**Passo 1-2: Entrada do Usuário**
```json
POST /api/cart/optimize
{
  "books": [
    { "title": "Clean Code", "isbn": "978-0132350884" },
    { "title": "Domain-Driven Design", "isbn": "978-0321125217" },
    { "title": "The Pragmatic Programmer" }
  ],
  "options": {
    "maxSitesPerPurchase": 3,
    "preferSingleSite": false,
    "maxTotalPrice": 500.00,
    "includeUsedBooks": false
  }
}
```

**Passo 3: Validação e Rate Limiting**
- Verifica JWT válido
- Verifica créditos do usuário
- Aplica rate limit (10 req/min por usuário)

**Passo 4-5: Verificação de Cache**
```
Cache Key: "cart:{hash_of_books}:{options_hash}"
TTL: Configurável por loja (padrão 1 hora)
```

**Passo 6-8: Execução dos Scrapers**
- W16Engine orquestra scrapers em paralelo
- Cada scraper tem timeout de 30s
- Retry com backoff exponencial (Polly)
- Circuit breaker por provider

**Passo 9-10: Algoritmo de Otimização**
```
1. Agrupa preços por livro
2. Para cada livro, ordena por (preço + frete_proporcional)
3. Aplica algoritmo de otimização combinatória:
   - Se preferSingleSite: encontra loja com todos os livros
   - Se não: calcula combinação ótima (branch and bound)
4. Considera:
   - Frete grátis acima de X
   - Cupons de desconto conhecidos
   - Disponibilidade em estoque
```

**Passo 11: Persistência**
- Salva no Redis (cache de preços)
- Salva no PostgreSQL (histórico de consultas)
- Atualiza métricas (Prometheus)

**Passo 12-14: Resposta**
```json
{
  "success": true,
  "optimization": {
    "totalPrice": 287.50,
    "totalFreight": 15.00,
    "totalSavings": 45.30,
    "purchasePlan": [
      {
        "provider": "Amazon",
        "providerUrl": "https://amazon.com.br",
        "books": [
          { "title": "Clean Code", "price": 89.90 },
          { "title": "Domain-Driven Design", "price": 120.00 }
        ],
        "subtotal": 209.90,
        "freight": 0.00,
        "note": "Frete grátis acima de R$ 150"
      },
      {
        "provider": "Estante Virtual",
        "providerUrl": "https://estantevirtual.com.br",
        "books": [
          { "title": "The Pragmatic Programmer", "price": 77.60 }
        ],
        "subtotal": 77.60,
        "freight": 15.00
      }
    ]
  },
  "alternatives": [...],
  "metadata": {
    "queryId": "uuid",
    "executionTimeMs": 4523,
    "providersQueried": 12,
    "cacheHits": 3,
    "cost": 5
  }
}
```

---

## 4. Estratégia de Cache

### 4.1 Arquitetura de Cache em Camadas

```
┌─────────────────────────────────────────────────────────────────┐
│                      CACHE STRATEGY                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Layer 1: In-Memory (IMemoryCache)                              │
│  ├── TTL: 5 minutos                                             │
│  ├── Uso: Hot data, resultados recentes                         │
│  └── Tamanho: 100MB máximo                                      │
│                                                                  │
│  Layer 2: Distributed (Redis)                                   │
│  ├── TTL: Configurável por provider                             │
│  ├── Uso: Preços, sessões, rate limiting                        │
│  └── Estrutura: Hash por provider + ISBN                        │
│                                                                  │
│  Layer 3: Database (PostgreSQL)                                 │
│  ├── TTL: Permanente (histórico)                                │
│  ├── Uso: Análise, trends, auditoria                            │
│  └── Índices: provider_id + book_id + query_date                │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 TTL por Provider

```csharp
public static class CacheTTLConfiguration
{
    public static readonly Dictionary<ProviderCategoryEnum, TimeSpan> TTLByProvider = new()
    {
        // Lojas com preços estáveis
        { ProviderCategoryEnum.Cedet, TimeSpan.FromHours(6) },

        // Amazon muda frequentemente
        { ProviderCategoryEnum.Amazon, TimeSpan.FromMinutes(30) },

        // Marketplace - preços voláteis
        { ProviderCategoryEnum.MercadoLivre, TimeSpan.FromMinutes(15) },

        // Estante Virtual - usados, mais estável
        { ProviderCategoryEnum.EstanteVirtual, TimeSpan.FromHours(12) },

        // Default
        { ProviderCategoryEnum.Generic, TimeSpan.FromHours(1) }
    };
}
```

### 4.3 Estrutura de Cache no Redis

```
# Preço individual por livro/loja
price:{provider_id}:{isbn} = {
  "price": 89.90,
  "discount": 15,
  "available": true,
  "freight": 12.50,
  "updated_at": "2024-01-15T10:30:00Z"
}
TTL: Conforme provider

# Resultado de otimização de carrinho
cart:{user_id}:{books_hash} = {
  "optimization": {...},
  "created_at": "2024-01-15T10:30:00Z"
}
TTL: 15 minutos

# Rate limiting
rate:{user_id}:{endpoint} = contador
TTL: 1 minuto

# Circuit breaker state
circuit:{provider_id} = {
  "state": "open|closed|half-open",
  "failures": 5,
  "last_failure": "2024-01-15T10:30:00Z"
}
TTL: 5 minutos
```

### 4.4 Cache Bypass (Debug Mode)

```csharp
public class CacheOptions
{
    public bool BypassCache { get; set; } = false;
    public bool ForceRefresh { get; set; } = false;
    public List<string> BypassProviders { get; set; } = new();
}

// Header para bypass
// X-Cache-Control: no-cache
// X-Force-Refresh: true
```

---

## 5. Estratégia de Cálculo de Custo

### 5.1 Métricas Consideradas

O custo de uma consulta é calculado com base em múltiplos fatores:

```
┌─────────────────────────────────────────────────────────────────┐
│                    COST CALCULATION                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Custo Total = Base + (Scrapers × W1) + (Tempo × W2) + Complexidade
│                                                                  │
│  Onde:                                                           │
│  ├── Base: 1 crédito (custo fixo por transação)                 │
│  ├── Scrapers: Quantidade de providers consultados              │
│  ├── W1: 0.5 créditos por provider                              │
│  ├── Tempo: Tempo de execução em segundos                       │
│  ├── W2: 0.1 créditos por segundo                               │
│  └── Complexidade: Número de livros × 0.2                       │
│                                                                  │
│  Descontos:                                                      │
│  ├── Cache Hit: -50% do custo                                   │
│  ├── Usuário Premium: -30%                                      │
│  └── Horário off-peak: -20%                                     │
│                                                                  │
│  Limites:                                                        │
│  ├── Mínimo: 1 crédito                                          │
│  └── Máximo: 50 créditos                                        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Justificativa da Métrica

**Por que essa combinação de fatores?**

1. **Custo Base (1 crédito)**
   - Garante receita mínima por transação
   - Cobre overhead fixo (infraestrutura, logging)

2. **Por Provider (0.5 créditos)**
   - Scrapers consomem recursos (CPU, memória, rede)
   - Risco de bloqueio por antibot
   - Custo proporcional ao uso real

3. **Por Tempo (0.1 créditos/segundo)**
   - Consultas lentas ocupam threads
   - Incentiva otimização pelo sistema
   - Penaliza providers problemáticos

4. **Por Livro (0.2 créditos)**
   - Mais livros = mais processamento
   - Algoritmo de otimização é O(n²) no pior caso
   - Incentiva consultas em lote (mais eficiente)

### 5.3 Implementação

```csharp
public class TransactionCostCalculator
{
    private const decimal BaseCost = 1.0m;
    private const decimal CostPerProvider = 0.5m;
    private const decimal CostPerSecond = 0.1m;
    private const decimal CostPerBook = 0.2m;
    private const decimal CacheHitDiscount = 0.5m;
    private const decimal PremiumDiscount = 0.3m;
    private const decimal OffPeakDiscount = 0.2m;
    private const decimal MinCost = 1.0m;
    private const decimal MaxCost = 50.0m;

    public TransactionCost Calculate(TransactionMetrics metrics, UserContext user)
    {
        // Custo bruto
        var rawCost = BaseCost
            + (metrics.ProvidersQueried * CostPerProvider)
            + (metrics.ExecutionTimeSeconds * CostPerSecond)
            + (metrics.BooksRequested * CostPerBook);

        // Aplica descontos
        var discounts = 0m;

        if (metrics.CacheHitRatio > 0.5m)
            discounts += CacheHitDiscount;

        if (user.IsPremium)
            discounts += PremiumDiscount;

        if (IsOffPeakHour())
            discounts += OffPeakDiscount;

        var finalCost = rawCost * (1 - Math.Min(discounts, 0.7m));

        return new TransactionCost
        {
            RawCost = rawCost,
            Discounts = discounts,
            FinalCost = Math.Clamp(finalCost, MinCost, MaxCost),
            Breakdown = new CostBreakdown
            {
                Base = BaseCost,
                Providers = metrics.ProvidersQueried * CostPerProvider,
                Time = metrics.ExecutionTimeSeconds * CostPerSecond,
                Books = metrics.BooksRequested * CostPerBook
            }
        };
    }

    private bool IsOffPeakHour()
    {
        var hour = DateTime.Now.Hour;
        return hour >= 0 && hour < 8; // 00:00 - 08:00
    }
}
```

---

## 6. Componentes Detalhados

### 6.1 Domain Layer (Existente + Novos)

```
BookPricesWatcher.Domain/
├── Entities/
│   ├── User.cs ✓ (existente)
│   ├── Book.cs ✓ (existente)
│   ├── BookPrice.cs ✓ (existente)
│   ├── Provider.cs ✓ (existente)
│   ├── Query.cs ✓ (existente)
│   ├── ResultType.cs ✓ (existente - melhorado)
│   ├── Token.cs ✓ (existente)
│   ├── Client.cs ✓ (existente)
│   ├── Scraper.cs ✓ (existente)
│   ├── Cart.cs ⭐ (NOVO)
│   ├── CartItem.cs ⭐ (NOVO)
│   ├── OptimizationResult.cs ⭐ (NOVO)
│   ├── FreightRule.cs ⭐ (NOVO)
│   └── UserCredits.cs ⭐ (NOVO)
├── Enums/
│   ├── ProviderCategoryEnum.cs ✓ (existente)
│   ├── ScraperTypeEnum.cs ✓ (existente)
│   ├── OptimizationStrategyEnum.cs ⭐ (NOVO)
│   └── TransactionStatusEnum.cs ⭐ (NOVO)
├── Interfaces/
│   ├── IUserRepository.cs ✓ (existente)
│   ├── IBookRepository.cs ⭐ (NOVO)
│   ├── IBookPriceRepository.cs ⭐ (NOVO)
│   ├── IProviderRepository.cs ⭐ (NOVO)
│   ├── IQueryRepository.cs ⭐ (NOVO)
│   └── ICartRepository.cs ⭐ (NOVO)
└── ValueObjects/
    ├── ISBN.cs ⭐ (NOVO)
    ├── Money.cs ⭐ (NOVO)
    └── BookIdentifier.cs ⭐ (NOVO)
```

### 6.2 Business Layer (Existente + Novos)

```
BookPricesWatcher.Business/
├── Core/
│   ├── Base/
│   │   ├── W16Engine.cs ✓ (existente - melhorado)
│   │   ├── Requestor.cs ✓ (existente)
│   │   └── Comparator.cs ✓ (existente)
│   ├── Scrapers/
│   │   ├── ScraperFactory.cs ✓ (existente)
│   │   ├── SearchParameter.cs ✓ (existente)
│   │   ├── SearchResult.cs ✓ (existente - melhorado)
│   │   ├── BookPriceResult.cs ✓ (existente)
│   │   └── Cedet/
│   │       └── HttpClient/
│   │           └── CedetSingleSearchHttpClient.cs ✓ (existente - melhorado)
│   ├── Optimization/ ⭐ (NOVO)
│   │   ├── CartOptimizer.cs
│   │   ├── FreightCalculator.cs
│   │   ├── CombinationGenerator.cs
│   │   └── Strategies/
│   │       ├── IOptimizationStrategy.cs
│   │       ├── LowestPriceStrategy.cs
│   │       ├── SingleSiteStrategy.cs
│   │       └── BalancedStrategy.cs
│   └── Cache/ ⭐ (NOVO)
│       ├── ICacheManager.cs
│       ├── RedisCacheManager.cs
│       ├── CacheKeyGenerator.cs
│       └── CacheTTLConfiguration.cs
├── Services/
│   ├── UserService.cs ✓ (existente - incompleto)
│   ├── BookService.cs ⭐ (NOVO)
│   ├── BookPriceService.cs ⭐ (NOVO)
│   ├── CartOptimizerService.cs ⭐ (NOVO)
│   ├── ProviderService.cs ⭐ (NOVO)
│   ├── QueryHistoryService.cs ⭐ (NOVO)
│   └── CreditService.cs ⭐ (NOVO)
├── Interfaces/
│   ├── IScraper.cs ✓ (existente)
│   ├── IUserService.cs ✓ (existente)
│   ├── IBookService.cs ⭐ (NOVO)
│   ├── ICartOptimizerService.cs ⭐ (NOVO)
│   ├── ICacheManager.cs ⭐ (NOVO)
│   └── ICostCalculator.cs ⭐ (NOVO)
└── DTOs/
    ├── UserDto.cs ✓ (existente)
    ├── CartOptimizationRequestDto.cs ⭐ (NOVO)
    ├── CartOptimizationResultDto.cs ⭐ (NOVO)
    ├── BookSearchRequestDto.cs ⭐ (NOVO)
    └── PriceComparisonDto.cs ⭐ (NOVO)
```

### 6.3 API Layer (Existente + Novos)

```
API/
├── Controllers/
│   ├── AuthController.cs ✓ (existente)
│   ├── BookSearchController.cs ✓ (existente)
│   ├── CartOptimizerController.cs ⭐ (NOVO)
│   ├── BooksController.cs ⭐ (NOVO)
│   ├── ProvidersController.cs ⭐ (NOVO)
│   ├── HistoryController.cs ⭐ (NOVO)
│   └── CreditsController.cs ⭐ (NOVO)
├── Middlewares/ ⭐ (NOVO)
│   ├── RateLimitingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── ErrorHandlingMiddleware.cs
├── Filters/ ⭐ (NOVO)
│   ├── ValidateCreditsFilter.cs
│   └── CacheControlFilter.cs
├── Services/
│   └── TokenService.cs ✓ (existente)
├── DTOs/
│   ├── LoginDTO.cs ✓ (existente)
│   ├── BookDTO.cs ✓ (existente)
│   ├── CartRequestDTO.cs ⭐ (NOVO)
│   └── ApiResponseDTO.cs ⭐ (NOVO)
└── Configurations/
    └── Configurator.cs ✓ (existente)
```

---

## 7. Pontos de Falha e Mitigação

### 7.1 Mapa de Riscos

```
┌─────────────────────────────────────────────────────────────────┐
│                    FAILURE POINTS & MITIGATION                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. SCRAPER FAILURES                                            │
│  ├── Risco: Site muda HTML, antibot, timeout                    │
│  ├── Impacto: Alto (core business)                              │
│  └── Mitigação:                                                 │
│      ├── Circuit breaker por provider (Polly)                   │
│      ├── Retry com backoff exponencial                          │
│      ├── Fallback para cache stale                              │
│      ├── Alertas quando >30% falha                              │
│      └── Scrapers versionados com fallback                      │
│                                                                  │
│  2. DATABASE FAILURES                                           │
│  ├── Risco: PostgreSQL indisponível                             │
│  ├── Impacto: Crítico                                           │
│  └── Mitigação:                                                 │
│      ├── Connection pooling (Npgsql)                            │
│      ├── Read replicas para queries                             │
│      ├── Graceful degradation (read from cache)                 │
│      └── Health checks + auto-restart                           │
│                                                                  │
│  3. REDIS FAILURES                                              │
│  ├── Risco: Cache indisponível                                  │
│  ├── Impacto: Médio (performance)                               │
│  └── Mitigação:                                                 │
│      ├── Fallback para IMemoryCache                             │
│      ├── Redis Sentinel/Cluster                                 │
│      └── Bypass automático se Redis down                        │
│                                                                  │
│  4. RATE LIMITING / ANTIBOT                                     │
│  ├── Risco: IP bloqueado por provedores                         │
│  ├── Impacto: Alto                                              │
│  └── Mitigação:                                                 │
│      ├── Delay randômico entre requests (1-3s)                  │
│      ├── Rotate User-Agents                                     │
│      ├── Proxy pool (futuro)                                    │
│      ├── Respeitar robots.txt                                   │
│      └── Rate limit interno por provider                        │
│                                                                  │
│  5. OPTIMIZATION ALGORITHM                                      │
│  ├── Risco: Timeout em listas grandes (>20 livros)              │
│  ├── Impacto: Médio                                             │
│  └── Mitigação:                                                 │
│      ├── Limite de 20 livros por request                        │
│      ├── Algoritmo aproximado para n > 10                       │
│      ├── Timeout de 30s com resultado parcial                   │
│      └── Queue para processamento async                         │
│                                                                  │
│  6. AUTHENTICATION                                              │
│  ├── Risco: Token leak, brute force                             │
│  ├── Impacto: Alto (segurança)                                  │
│  └── Mitigação:                                                 │
│      ├── JWT com expiration curto (2h)                          │
│      ├── Refresh tokens                                         │
│      ├── Rate limit em /login                                   │
│      └── Secrets em environment variables                       │
│                                                                  │
│  7. MEMORY LEAKS                                                │
│  ├── Risco: HttpClient não reutilizado                          │
│  ├── Impacto: Médio (já mitigado)                               │
│  └── Mitigação:                                                 │
│      ├── HttpClient estático ✓ (implementado)                   │
│      ├── IHttpClientFactory (recomendado)                       │
│      └── Memory profiling periódico                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 Circuit Breaker Configuration

```csharp
public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetScraperPolicy()
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));

        var circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (ex, duration) =>
                    Log.Warning("Circuit OPEN for {duration}", duration),
                onReset: () =>
                    Log.Information("Circuit CLOSED"));

        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(30);

        return Policy.WrapAsync(retryPolicy, circuitBreaker, timeout);
    }
}
```

---

## 8. Tecnologias Recomendadas

### 8.1 Stack Atual (Manter)

| Camada | Tecnologia | Versão | Status |
|--------|------------|--------|--------|
| Backend | ASP.NET Core | 8.0 | ✓ |
| ORM | Entity Framework Core | 8.0 | ✓ |
| Database | PostgreSQL | 15+ | ✓ |
| Frontend | Angular | 20 | ✓ |
| Auth | JWT Bearer | - | ✓ |
| Scraping | HtmlAgilityPack | 1.11 | ✓ |

### 8.2 Adicionar

| Camada | Tecnologia | Propósito |
|--------|------------|-----------|
| Cache | Redis | Cache distribuído |
| Cache Client | StackExchange.Redis | Client .NET |
| Resilience | Polly | Retry, Circuit Breaker |
| Logging | Serilog | Logs estruturados |
| Metrics | Prometheus | Métricas |
| Tracing | OpenTelemetry | Distributed tracing |
| Background Jobs | Hangfire | Scheduled tasks |
| Validation | FluentValidation | Input validation |
| Mapping | Mapster | Object mapping (leve) |
| API Docs | Swashbuckle | OpenAPI/Swagger |
| Health Checks | AspNetCore.HealthChecks | Monitoring |
| Rate Limiting | AspNetCoreRateLimit | Throttling |

### 8.3 Pacotes NuGet Recomendados

```xml
<!-- Caching -->
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />

<!-- Resilience -->
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />

<!-- Logging & Observability -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.7.0" />

<!-- Background Jobs -->
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.6" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.4" />

<!-- Validation -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />

<!-- Mapping -->
<PackageReference Include="Mapster" Version="7.4.0" />

<!-- Health Checks -->
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.0.0" />

<!-- Rate Limiting -->
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
```

---

## 9. Roadmap de Implementação

### Fase 1: Fundação (2-3 semanas)
- [ ] Implementar repositórios faltantes (Book, BookPrice, Provider, Query)
- [ ] Adicionar DbSets faltantes ao SherlockDbContext
- [ ] Criar migrations para relacionamentos
- [ ] Implementar BookService e BookPriceService
- [ ] Adicionar Serilog para logging estruturado
- [ ] Configurar health checks básicos

### Fase 2: Cache & Resilience (2 semanas)
- [ ] Configurar Redis
- [ ] Implementar CacheManager com TTL por provider
- [ ] Adicionar Polly (retry, circuit breaker)
- [ ] Implementar rate limiting por usuário
- [ ] Cache bypass para debug mode

### Fase 3: Otimização de Carrinho (3 semanas)
- [ ] Criar entidades Cart, CartItem, OptimizationResult
- [ ] Implementar CartOptimizerService
- [ ] Criar algoritmo de otimização (branch and bound)
- [ ] Implementar FreightCalculator
- [ ] Criar endpoint /api/cart/optimize
- [ ] Testes unitários do algoritmo

### Fase 4: Frontend (2-3 semanas)
- [ ] Criar BookSearchService
- [ ] Implementar página de busca com lista de livros
- [ ] Criar componentes de resultado
- [ ] Implementar visualização de otimização
- [ ] Adicionar loading states e error handling

### Fase 5: Observabilidade (1-2 semanas)
- [ ] Configurar Prometheus metrics
- [ ] Adicionar dashboards básicos
- [ ] Configurar alertas
- [ ] Implementar request tracing

### Fase 6: Polimento (1-2 semanas)
- [ ] Testes de integração
- [ ] Performance tuning
- [ ] Documentação da API
- [ ] Security review
- [ ] Deploy pipeline (CI/CD)

---

## 10. Métricas de Sucesso

| Métrica | Target | Como Medir |
|---------|--------|------------|
| Tempo de resposta (P95) | < 5s | Prometheus |
| Taxa de sucesso de scraping | > 85% | Logs agregados |
| Cache hit ratio | > 60% | Redis stats |
| Uptime | > 99.5% | Health checks |
| Economia média para usuário | > 15% | Cálculo no resultado |
| Custo médio por consulta | 3-5 créditos | Logs de transação |

---

*Documento gerado para o projeto BookPricesWatcher*
*Versão: 1.0*
*Data: 2024*
