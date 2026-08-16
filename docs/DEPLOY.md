# Deploy — Hospedando o Sherlock neste PC

Guia para rodar a aplicação nesta máquina (sempre ligada) e expô-la na internet
com **Cloudflare Tunnel** — sem abrir portas do roteador e sem expor o IP residencial.

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
