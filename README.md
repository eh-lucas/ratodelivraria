# Sherlock - Comparador de Preços de Livros

Aplicação full-stack para busca e comparação de preços de livros em diversas livrarias online brasileiras. O sistema faz scraping em tempo real de 93 lojas e apresenta os melhores preços para o usuário.

## Stack Tecnológica

- **Backend**: .NET 8 (ASP.NET Core Web API)
- **Frontend**: Angular 20
- **Banco de Dados**: PostgreSQL 16
- **Cache**: Redis 7 (opcional)
- **Containerização**: Docker & Docker Compose

## Início Rápido com Docker

### Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) (versão 20.10+)
- [Docker Compose](https://docs.docker.com/compose/install/) (versão 2.0+)

### Executar a Aplicação Completa

```bash
# Clone o repositório
git clone <repository-url>
cd Sherlock

# Inicie todos os serviços
docker-compose up -d

# Acompanhe os logs (opcional)
docker-compose logs -f
```

Após a inicialização (aguarde ~1-2 minutos), acesse:

| Serviço | URL |
|---------|-----|
| **Aplicação** | http://localhost:4200 |
| **API (Swagger)** | http://localhost:5177/swagger |
| **Health Check** | http://localhost:5177/health |

### Parar a Aplicação

```bash
# Parar todos os serviços
docker-compose down

# Parar e remover volumes (limpa dados do banco)
docker-compose down -v
```

### Rebuild após Alterações

```bash
# Rebuild e reinicia
docker-compose up -d --build
```

## Arquitetura Docker

```
┌─────────────────────────────────────────────────────────────┐
│                    docker-compose.yml                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │   Client     │───▶│     API      │───▶│  PostgreSQL  │   │
│  │   (nginx)    │    │   (.NET 8)   │    │              │   │
│  │   :4200      │    │   :5177      │    │   :5433      │   │
│  └──────────────┘    └──────┬───────┘    └──────────────┘   │
│                             │                                │
│                             ▼                                │
│                      ┌──────────────┐                        │
│                      │    Redis     │                        │
│                      │   (cache)    │                        │
│                      │   :6379      │                        │
│                      └──────────────┘                        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Serviços

| Serviço | Container | Porta Externa | Descrição |
|---------|-----------|---------------|-----------|
| `client` | sherlock-client | 4200 | Frontend Angular (nginx) |
| `api` | sherlock-api | 5177 | Backend .NET API |
| `postgres` | sherlock-postgres | 5433 | Banco de dados PostgreSQL |
| `redis` | sherlock-redis | 6379 | Cache Redis |

## Desenvolvimento Local

Para desenvolvimento com hot-reload, use o compose de desenvolvimento:

### 1. Iniciar Banco de Dados e Redis

```bash
docker-compose -f docker-compose.dev.yml up -d
```

### 2. Executar o Backend

```bash
# Navegue até a pasta do projeto
cd Sherlock.Api

# Restaurar dependências (primeira vez)
dotnet restore

# Executar com hot-reload
dotnet watch run
```

A API estará disponível em: http://localhost:5177

### 3. Executar o Frontend

```bash
# Navegue até a pasta do cliente
cd Client

# Instalar dependências (primeira vez)
npm install

# Executar com hot-reload
npm start
```

O frontend estará disponível em: http://localhost:4200

## Estrutura do Projeto

```
Sherlock/
├── Sherlock.Api/                 # ASP.NET Core Web API
│   ├── Controllers/              # Endpoints da API
│   ├── Configurations/           # Configurações (JWT, CORS, etc)
│   └── Dockerfile                # Container da API
├── Sherlock.Business/   # Lógica de negócio
│   ├── Core/                     # Engine de scraping
│   ├── DTOs/                     # Data Transfer Objects
│   └── Services/                 # Serviços de aplicação
├── Sherlock.Data/       # Acesso a dados (EF Core)
├── Sherlock.Domain/     # Entidades e interfaces
├── Sherlock.Infrastructure/ # Cache, resiliência
├── Client/                       # Angular 20 App
│   ├── src/app/                  # Componentes Angular
│   ├── Dockerfile.prod           # Container de produção
│   └── nginx.conf                # Configuração do nginx
├── docker-compose.yml            # Produção (todos os serviços)
├── docker-compose.dev.yml        # Desenvolvimento (só DB + Redis)
└── README.md
```

## API Endpoints

### Autenticação

```http
POST /api/auth/register    # Registrar novo usuário
POST /api/auth/login       # Login (retorna JWT)
```

### Busca de Livros (requer autenticação)

```http
GET  /api/BookSearch?isbn={isbn}       # Buscar preços por ISBN
POST /api/BookSearch                   # Buscar com body JSON
POST /api/BookSearch/single            # Melhor resultado + alternativas
```

### Carrinho (requer autenticação)

```http
POST /api/Cart/optimize                # Otimizar carrinho (múltiplos livros)
POST /api/Cart/best-provider           # Melhor provider único
```

### Providers

```http
GET /api/Providers                     # Listar todos os providers
GET /api/Providers/active              # Listar providers ativos
```

## Variáveis de Ambiente

### API (.NET)

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ConnectionStrings__SherlockDb` | String de conexão PostgreSQL | - |
| `ConnectionStrings__Redis` | String de conexão Redis | localhost:6379 |
| `UseRedis` | Habilitar cache Redis | false |
| `JwtSettings__SecretKey` | Chave secreta para JWT | - |
| `ASPNETCORE_ENVIRONMENT` | Ambiente (Development/Production) | Production |

### Exemplo de Configuração

```bash
# .env (para docker-compose)
POSTGRES_USER=sherlock_admin
POSTGRES_PASSWORD=SuperSecure123!
POSTGRES_DB=sherlock_dev_db
JWT_SECRET=sua-chave-super-secreta-aqui
```

## Troubleshooting

### Container não inicia

```bash
# Verificar logs
docker-compose logs api
docker-compose logs client

# Verificar status
docker-compose ps
```

### Erro de conexão com banco

```bash
# Verificar se o PostgreSQL está rodando
docker-compose ps postgres

# Conectar diretamente ao banco
docker exec -it sherlock-postgres psql -U sherlock_admin -d sherlock_dev_db
```

### Rebuild completo

```bash
# Limpar tudo e reconstruir
docker-compose down -v
docker system prune -f
docker-compose up -d --build
```

### Verificar health checks

```bash
# API health
curl http://localhost:5177/health

# Ou via Docker
docker inspect --format='{{.State.Health.Status}}' sherlock-api
```

## Contribuindo

1. Fork o repositório
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -m 'feat: adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

## Licença

Este projeto é privado e de uso restrito.
