# Deploy & Boas Práticas — Sherlock self-hosted

Guia para rodar o Sherlock como servidor pessoal (notebook Ubuntu) exposto à internet,
com as boas práticas de segurança e operação aplicadas/recomendadas.

---

## 1. Preparar o servidor (Ubuntu Desktop)

Configurações para o notebook se comportar como servidor 24/7:

- **Não suspender ao fechar a tampa** (a opção some da UI em alguns GNOME — edite o systemd):
  ```bash
  sudo nano /etc/systemd/logind.conf
  # descomente e ajuste:
  #   HandleLidSwitch=ignore
  #   HandleLidSwitchExternalPower=ignore
  #   HandleLidSwitchDocked=ignore
  sudo systemctl restart systemd-logind   # pode deslogar a sessão gráfica
  ```
- **Desligar suspensão por ociosidade**: Configurações → Energia → Suspensão automática: Desativada.
- **Login automático** (opcional): Configurações → Usuários → Login automático.
- **Religar após queda de energia**: ativar "Restore on AC Power Loss" na BIOS.
- Preferir **cabo de rede** ao Wi-Fi (mais estável).

## 2. Instalar Docker

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # usar docker sem sudo (relogar depois)
sudo systemctl enable docker    # sobe no boot
```

## 3. Configurar segredos (.env)

O `.env` **não vem do git** (contém segredos). Crie-o no servidor a partir do modelo:

```bash
cd ~/Sherlock
cp .env.example .env
nano .env        # preencher com valores reais
```

Gere segredos fortes:
```bash
openssl rand -base64 24   # senhas (POSTGRES_PASSWORD, APP_DB_PASSWORD)
openssl rand -base64 48   # JWT_SECRET
```

> ⚠️ O Postgres só cria banco/usuários na **primeira** inicialização do volume.
> Se já subiu antes com outros valores, zere o volume (apaga dados): `docker compose down -v`.

## 4. Subir a aplicação

```bash
docker compose up -d --build
docker compose ps           # conferir saúde dos containers
docker compose logs -f api  # acompanhar logs da API
```

URLs locais: Front `:4200` · API `:5177` · Swagger `:5177/swagger`.

## 5. Expor à internet — Cloudflare Tunnel (gratuito)

A Cloudflare **não dá domínios grátis**, mas o Tunnel expõe sem abrir portas no roteador
e sem IP fixo, com HTTPS automático.

### Opção A — teste rápido, sem domínio
```bash
sudo apt install cloudflared
cloudflared tunnel --url http://localhost:4200   # gera URL https://...trycloudflare.com (muda a cada restart)
```

### Opção B — URL fixa (precisa de um domínio próprio, ex.: registro.br ~R$40/ano)
1. Conta grátis em dash.cloudflare.com → **Add a site** (plano Free).
2. Trocar os nameservers do domínio pelos da Cloudflare (no registrador).
3. Criar e rotear o tunnel:
   ```bash
   cloudflared tunnel login
   cloudflared tunnel create sherlock
   cloudflared tunnel route dns sherlock app.seudominio.com.br
   cloudflared tunnel route dns sherlock api.seudominio.com.br
   ```
4. `~/.cloudflared/config.yml`:
   ```yaml
   tunnel: sherlock
   credentials-file: /home/SEU_USUARIO/.cloudflared/<id-do-tunnel>.json
   ingress:
     - hostname: app.seudominio.com.br
       service: http://localhost:4200
     - hostname: api.seudominio.com.br
       service: http://localhost:5177
     - service: http_status:404
   ```
5. Rodar como serviço (sobe no boot):
   ```bash
   sudo cloudflared service install
   sudo systemctl enable --now cloudflared
   ```

> Depois de definir o domínio da API, ajustar a URL da API no frontend Angular.

---

## ✅ Boas práticas — Checklist de hardening

### Já aplicadas
- [x] **Segredos fora do código** → `.env` (gitignored) + `.env.example` versionado.
- [x] **Separação de privilégios no banco**: API conecta como `sherlock_app` (limitado);
      `sherlock_admin` (superusuário) reservado para administração manual.
- [x] **`restart: unless-stopped`** em todos os serviços (resiliência a reboot/crash).
- [x] **`.gitattributes`** força LF em `*.sh` (evita quebra de script no Linux).
- [x] **Imagens com tag fixa** (`postgres:16-alpine`, `redis:7-alpine`).

### Pendentes (recomendadas antes de abrir ao público)
- [ ] **Fechar portas de Postgres/Redis no host**: remover os blocos `ports:` desses serviços
      no `docker-compose.yml`. Eles já se comunicam pela rede interna `sherlock-network`;
      expor no host é superfície de ataque (Redis está sem senha). **Alta prioridade.**
- [ ] **CORS restrito**: a API deve aceitar requisições apenas do domínio do front
      (`https://app.seudominio.com.br`), não de qualquer origem.
- [ ] **Rate limiting no `/api/auth/login`**: usar rate limiting nativo do ASP.NET Core 8
      contra força bruta.
- [ ] **Limpar segredos hardcoded** dos `appsettings.json` / `appsettings.Docker.json`
      (trocar por placeholders — tudo vem do `.env` agora).
- [ ] **Container da API como non-root**: adicionar usuário não-privilegiado no Dockerfile.
- [ ] **Backup automático do banco**: cron diário com `pg_dump` (ver abaixo).

---

## Backup do banco (sugestão)

```bash
# Backup manual
docker exec sherlock-postgres pg_dump -U sherlock_admin sherlock_db | gzip > backup_$(date +%F).sql.gz

# Restore
gunzip -c backup_AAAA-MM-DD.sql.gz | docker exec -i sherlock-postgres psql -U sherlock_admin sherlock_db
```
Automatizar via `crontab -e` (diário às 3h):
```
0 3 * * * docker exec sherlock-postgres pg_dump -U sherlock_admin sherlock_db | gzip > ~/backups/sherlock_$(date +\%F).sql.gz
```

## Operação do dia a dia

```bash
docker compose ps                 # status
docker compose logs -f api        # logs ao vivo
docker compose pull && docker compose up -d --build   # atualizar
docker compose restart api        # reiniciar um serviço
docker compose down               # parar tudo (mantém dados)
```
