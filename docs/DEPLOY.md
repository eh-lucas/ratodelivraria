# Deploy — Hospedando o Sherlock

Guia para rodar a aplicação numa máquina sempre ligada e expô-la na internet com
**Cloudflare Tunnel** — sem abrir portas do roteador e sem expor o IP residencial.
As seções 1 a 3 descrevem o deploy neste PC; a seção 4, como levá-lo para outra máquina.

## Arquitetura do deploy

```
Navegador (internet)
   │  HTTPS
   ▼
Cloudflare Edge ──(túnel criptografado)──► cloudflared (neste PC)
                                              │
                                              ▼
                                      client :4200 (nginx)
                                       ├── / .............. Angular (SPA)
                                       └── /api ─proxy─► api :8080 ──► postgres / redis
```

O container **client (nginx)** já serve o front e faz proxy de `/api` para a API,
então basta apontar o túnel para `http://localhost:4200` — ele entrega o app inteiro.

---

## 1. Rodar a stack localmente

Pré-requisitos: Docker + Docker Compose, e o arquivo `.env` preenchido
(veja `.env.example`; gere segredos com `openssl rand -base64 24`).

```bash
docker compose up -d --build     # sobe postgres, redis, api, client
docker compose ps                # todos devem ficar "healthy"
```

Verificação local:

```bash
curl -o /dev/null -w "%{http_code}\n" http://localhost:4200/                    # 200 (front)
curl -o /dev/null -w "%{http_code}\n" http://localhost:4200/api/Providers/active # 200 (API via proxy)
```

`restart: unless-stopped` em todos os serviços → sobem sozinhos após reboot/crash.

Na rede local a app já fica acessível em `http://<IP-DA-MAQUINA>:4200`.

---

## 2. Opção A — Quick Tunnel (grátis, imediato, URL temporária)

Bom para testar/compartilhar rápido. **A URL muda a cada reinício do túnel.**

```bash
# instala o cloudflared (binário no ~/.local/bin, sem sudo)
mkdir -p ~/.local/bin
curl -fsSL -o ~/.local/bin/cloudflared \
  https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64
chmod +x ~/.local/bin/cloudflared

# sobe o túnel apontando para o client
cloudflared tunnel --url http://localhost:4200
```

O cloudflared imprime a URL pública (`https://<aleatorio>.trycloudflare.com`).

---

## 3. Opção B — Named Tunnel (permanente, URL fixa, sobe no boot) ✅ recomendado

Requer um **domínio na sua conta Cloudflare** (a Cloudflare não dá domínio grátis;
registre um barato, ~US$1–10/ano, e adicione-o à sua conta — plano Free serve).

> Para registrar um `.com.br` (R$ 40/ano) e delegar o DNS para a Cloudflare, siga o
> passo a passo em [`DOMINIO-REGISTRO-BR.md`](./DOMINIO-REGISTRO-BR.md) — o `.com.br`
> tem uma pré-validação de DNS que exige uma ordem específica dos passos.

```bash
# 1. Login (abre o navegador; escolha o domínio/zone da conta)
cloudflared tunnel login

# 2. Cria o túnel (gera credenciais em ~/.cloudflared/<UUID>.json)
cloudflared tunnel create sherlock

# 3. Aponta um subdomínio para o túnel (cria o registro DNS automaticamente)
cloudflared tunnel route dns sherlock app.SEUDOMINIO.com
```

Crie `~/.cloudflared/config.yml`:

```yaml
tunnel: sherlock
credentials-file: /home/lucas/.cloudflared/<UUID>.json

ingress:
  - hostname: app.SEUDOMINIO.com
    service: http://localhost:4200
  - service: http_status:404
```

Instale como **serviço do sistema** (sobe no boot, reinicia sozinho) — precisa de sudo:

```bash
sudo cloudflared --config /home/lucas/.cloudflared/config.yml service install
sudo systemctl enable --now cloudflared
sudo systemctl status cloudflared
```

Pronto: `https://app.SEUDOMINIO.com` fica permanente, com HTTPS automático.

---

## 4. Subir em outra máquina

O deploy é todo em container, então trocar de máquina é `git clone` + `docker compose up`.
Três coisas **não** vêm no git e precisam ser levadas à mão:

1. o `.env` (segredos);
2. o volume `sherlock_postgres_data` (dados), se você quiser o histórico;
3. as credenciais do túnel Cloudflare.

### 4.1 Preparar a máquina

Docker + plugin Compose, git, e **~2 GB de RAM livre para o build** — o pico é o
`npm run build` do Angular, não o runtime. Máquina menor que isso: veja o 4.4.

```bash
git clone https://github.com/eh-lucas/Sherlock.git && cd Sherlock
cp .env.example .env
```

O que vai no `.env` depende do destino dos dados:

- **Banco novo, vazio** — gere segredos novos (`openssl rand -base64 24` / `48`).
  O `scripts/db-init/01-create-app-user.sh` cria o `sherlock_app` na primeira
  inicialização, as migrations rodam no startup e os providers vêm do código.
- **Migrando os dados atuais** — o que precisa casar depende do método (4.2):
  no dump lógico, só os **nomes** `POSTGRES_DB` e `APP_DB_USER` (o dump traz
  `OWNER TO sherlock_app`, mas nenhuma senha) — as senhas podem ser novas. Na
  cópia do volume, o `pg_authid` vem junto e o init script não roda, então o
  `.env` tem de ser o antigo inteiro, senhas incluídas. Copie-o por `scp`,
  nunca por git.

### 4.2 Levar os dados (opcional)

```bash
# maquina antiga
docker compose exec -T postgres pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > sherlock.dump
scp sherlock.dump usuario@maquina-nova:~/

# maquina nova, com o postgres de pe
docker compose up -d postgres
docker compose exec -T postgres pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists < sherlock.dump
```

A ordem importa: o dump não cria o banco nem os roles, só referencia os nomes. Quem
os cria é a primeira subida do container (`POSTGRES_DB` + `01-create-app-user.sh`),
por isso `up -d postgres` **antes** do restore e a API só depois. Restaure como
`POSTGRES_USER` (admin), não como o usuário da app, senão os `OWNER TO` falham.

Dos 88 MB, o que dói perder é `catalog_items` (~54 mil linhas, 29 MB): é a colheita
do catalog crawler nas 67 lojas, e refazer significa recrawlear tudo. `queries`
(130 mil linhas, 50 MB) é cache de preço com TTL de 60 min — dado que já nasceu
velho. Para deixar o peso morto para trás sem mexer no schema:

```bash
docker compose exec -T postgres pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc \
  --exclude-table-data=queries --exclude-table-data=transactions > sherlock.dump
```

O preço disso é o ranking e o prefetch dos mais buscados, que leem dessas duas
tabelas: sem elas, ambos começam do zero.

Prefira o dump à cópia do volume mesmo quando quiser tudo: o dump atravessa
versão maior do Postgres (16 → 17), o volume não.

Depois, suba tudo e valide com os comandos da seção 1, mais:

```bash
docker compose exec -T postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
  -c '\dt' -c 'SELECT count(*) FROM catalog_items;'
```

### 4.3 Apontar o túnel

`cloudflared tunnel login` abre um navegador, que máquina headless não tem. Duas saídas:

**Túnel novo, por token** (recomendado para VPS): no dashboard Zero Trust →
Networks → Tunnels → Create tunnel, copie o `cloudflared service install <TOKEN>`
que ele gera e rode na máquina nova. O public hostname (`http://localhost:4200`)
se configura pelo próprio dashboard — nenhum arquivo para copiar.

**Reaproveitar o túnel `sherlock`**: copie `~/.cloudflared/cert.pem`, o
`<UUID>.json` e o `config.yml`, corrigindo o path de `credentials-file`.
Atenção: com as duas máquinas rodando o mesmo túnel, a Cloudflare as trata como
réplicas e divide o tráfego entre elas. Pare o serviço na antiga
(`sudo systemctl stop cloudflared`) antes de subir a nova — ou use isso de
propósito, validando a stack nova por um hostname alternativo
(`novo.ratodelivraria.com.br`) e só então movendo o hostname principal.

### 4.4 Quando o build não cabe na máquina

Builde onde tem folga, publique no GHCR, e a máquina nova só baixa — é para isso
que existe o `docker-compose.registry.yml`:

```bash
# maquina com folga (precisa de `docker login ghcr.io`)
docker build -t ghcr.io/eh-lucas/sherlock-api:latest -f Sherlock.Api/Dockerfile .
docker build -t ghcr.io/eh-lucas/sherlock-client:latest ./Client
docker push ghcr.io/eh-lucas/sherlock-api:latest
docker push ghcr.io/eh-lucas/sherlock-client:latest

# maquina nova
docker compose -f docker-compose.yml -f docker-compose.registry.yml pull
docker compose -f docker-compose.yml -f docker-compose.registry.yml up -d
```

As arquiteturas têm de casar: imagem buildada em x86 não roda em ARM. Para
Graviton ou Raspberry Pi, builde na própria máquina alvo ou use
`docker buildx build --platform linux/arm64`.

### 4.5 Ajustes que a máquina nova pede

- `Search__MaxDegreeOfParallelism: 20` foi medido aqui, com dois IPs. Em host de
  IP único — e de datacenter, que as lojas bloqueiam primeiro — comece em 5–8 e
  suba medindo.
- **Remova os `ports` de `postgres` (5433) e `redis` (6379)** se o host tiver IP
  público: ali eles ficam expostos à internet, e os containers já se falam pela
  `sherlock-network`.
- O container da API instala chromium (scraper da Amazon): conte ~400 MB de RAM
  a mais enquanto `Amazon:Enabled` estiver ligado.

---

## Operação do dia a dia

```bash
docker compose logs -f api          # logs da API
docker compose ps                   # status/health
docker compose pull && docker compose up -d --build   # atualizar após git pull
docker compose down                 # parar tudo (dados persistem nos volumes)

sudo systemctl restart cloudflared  # reiniciar o túnel (named)
journalctl -u cloudflared -f        # logs do túnel (named)
```

## Segurança — checklist para host exposto na internet

- [ ] `.env` com segredos fortes e **fora do git** (já está no `.gitignore`).
- [ ] Não expor as portas de infra publicamente: o túnel aponta só para `:4200`.
      Postgres (`5433`) e Redis (`6379`) ficam só no host — **não** os coloque no túnel.
- [ ] Manter o SO e o Docker atualizados (`sudo apt update && sudo apt upgrade`).
- [ ] Backup periódico do volume `postgres_data`
      (`docker compose exec postgres pg_dump ...`).
- [ ] (Opcional) Cloudflare Access na frente da app para exigir login antes de chegar na origem.
```
