# Domínio próprio via Registro.br + Cloudflare Tunnel

Caminho completo para sair da URL temporária (`*.trycloudflare.com`) e colocar o
Sherlock em um `.com.br` fixo, mantendo o Cloudflare Tunnel.

Complementa a **Opção B (Named Tunnel)** do [`DEPLOY.md`](./DEPLOY.md) — aqui o
foco é a parte de domínio/DNS, que é onde o `.com.br` tem particularidades.

## Divisão de papéis

| Papel | Quem faz | Custo |
|---|---|---|
| **Registrar** (dono do domínio) | Registro.br | R$ 40/ano, fixo no registro e na renovação |
| **DNS autoritativo** (zona) | Cloudflare (plano Free) | R$ 0 |
| **Túnel + TLS** | `cloudflared` nesta máquina | R$ 0 |

O Registro.br é o único caminho oficial para `.com.br`, e a Cloudflare **não vende**
TLDs `.br`. Mas isso não é problema: o túnel só exige que a **zona DNS** esteja na
Cloudflare, e o Registro.br permite delegar os nameservers para qualquer provedor.

```
Registro.br (registrar)  ──delega NS──►  Cloudflare (DNS + edge)
                                              │  túnel criptografado
                                              ▼
                                       cloudflared (este PC)
                                              ▼
                                   client :4200 (nginx) ──► api :8080
```

---

## ⚠️ A pegadinha do `.com.br` — leia antes de começar

O Registro.br faz **pré-validação de DNS**: ao trocar os nameservers, ele consulta os
servidores informados e só aceita a alteração se eles já responderem autoritativamente
pela zona (SOA). Se você trocar os NS antes de criar a zona na Cloudflare, o painel
recusa com erro do tipo *"Pesquisa recusada"* / *"DNS não configurado"*.

**Portanto a ordem importa:** criar a zona na Cloudflare **primeiro**, trocar os NS no
Registro.br **depois**. Os passos abaixo já seguem essa ordem.

---

## 1. Registrar o domínio no Registro.br

1. Acesse https://registro.br e faça login com CPF ou CNPJ (obrigatório — o
   `.com.br` exige titular com documento brasileiro).
2. Pesquise o domínio desejado e registre.
3. **Pague o boleto/Pix.** O domínio só entra em produção após a confirmação do
   pagamento — antes disso ele fica como "aguardando pagamento" e não resolve.
4. Privacidade WHOIS: para pessoa física já vem ocultada por padrão.

> Dica: registrar por 2–3 anos de uma vez sai um pouco mais barato e evita
> esquecer a renovação (perder o domínio significa perder a URL pública).

## 2. Criar a zona na Cloudflare

1. Em https://dash.cloudflare.com → **Add a site** → digite `SEUDOMINIO.com.br`.
2. Escolha o plano **Free**.
3. A Cloudflare vai varrer registros existentes (não vai achar nada — normal) e
   exibir **dois nameservers**, algo como:

   ```
   lucy.ns.cloudflare.com
   mark.ns.cloudflare.com
   ```

   Anote os seus — cada conta recebe um par diferente.

4. **Crie um registro placeholder** na aba *DNS* para a zona ter conteúdo e
   responder às consultas do Registro.br:

   | Type | Name | Content | Proxy |
   |---|---|---|---|
   | `A` | `@` | `192.0.2.1` | DNS only (nuvem cinza) |

   `192.0.2.1` é um IP reservado para documentação (RFC 5737) — não aponta para
   lugar nenhum. Ele será substituído no passo 5.

## 3. Delegar os nameservers no Registro.br

1. https://registro.br → login → clique no domínio.
2. Em **Servidores DNS**, clique em **Alterar Servidores DNS**.
3. Selecione **"Informar servidores DNS"** (e não "Usar os servidores do Registro.br").
4. Preencha os dois nameservers da Cloudflare, sem IPs (deixe os campos de IP vazios —
   `ns.cloudflare.com` não é glue record).
5. Salve. Se der erro de pré-validação, confirme que o passo 2.4 (registro placeholder)
   está salvo e tente de novo em alguns minutos.

Propagação: normalmente minutos, oficialmente até 24h. Acompanhe:

```bash
dig +short NS SEUDOMINIO.com.br @a.dns.br
# deve retornar os dois *.ns.cloudflare.com
```

No painel da Cloudflare a zona sai de **Pending** para **Active** quando a delegação
é reconhecida (pode levar mais alguns minutos após o `dig` já responder).

## 4. Configurar TLS na Cloudflare

Com a zona ativa, em **SSL/TLS**:

- **Encryption mode:** `Full (strict)`
- **Edge Certificates → Always Use HTTPS:** `On`
- **Minimum TLS Version:** `1.2`
- (Opcional) **HSTS:** ligar só depois de confirmar que tudo funciona em HTTPS —
  é difícil de reverter.

O certificado do edge é emitido automaticamente e cobre `SEUDOMINIO.com.br` e
`*.SEUDOMINIO.com.br`.

## 5. Criar o Named Tunnel

O `cloudflared` já está instalado em `~/.local/bin/cloudflared`. Ainda **não** existe
`~/.cloudflared/` nem serviço systemd — este passo cria os dois.

```bash
# 1. Autentica (abre o navegador; selecione a zona SEUDOMINIO.com.br)
cloudflared tunnel login
# → grava ~/.cloudflared/cert.pem

# 2. Cria o túnel (gera ~/.cloudflared/<UUID>.json com as credenciais)
cloudflared tunnel create sherlock
cloudflared tunnel list          # anote o UUID

# 3. Cria os registros DNS apontando para o túnel
cloudflared tunnel route dns sherlock SEUDOMINIO.com.br
cloudflared tunnel route dns sherlock www.SEUDOMINIO.com.br
```

O `route dns` cria CNAMEs proxiados (`<UUID>.cfargotunnel.com`) — inclusive no apex,
graças ao CNAME flattening da Cloudflare. **Apague o registro placeholder `A @ 192.0.2.1`**
no painel de DNS depois disso (ou o `route dns` do apex vai reclamar de conflito —
nesse caso apague primeiro e rode o comando de novo).

## 6. Arquivo de configuração do túnel

Crie `~/.cloudflared/config.yml` (substitua `<UUID>` pelo valor real):

```yaml
tunnel: sherlock
credentials-file: /home/lucas/.cloudflared/<UUID>.json

ingress:
  - hostname: SEUDOMINIO.com.br
    service: http://localhost:4200
  - hostname: www.SEUDOMINIO.com.br
    service: http://localhost:4200
  - service: http_status:404
```

O container **client (nginx)** já serve o Angular e faz proxy de `/api` → `api:8080`,
então um único destino (`localhost:4200`) entrega a aplicação inteira.

Teste em foreground antes de virar serviço:

```bash
cloudflared --config ~/.cloudflared/config.yml tunnel run sherlock
# em outro terminal:
curl -o /dev/null -w "%{http_code}\n" https://SEUDOMINIO.com.br/
curl -s https://SEUDOMINIO.com.br/api/Providers/active | head -c 200
```

## 7. Instalar como serviço (sobe no boot)

```bash
sudo cloudflared --config /home/lucas/.cloudflared/config.yml service install
sudo systemctl enable --now cloudflared
systemctl status cloudflared
```

Junto com o `restart: unless-stopped` do `docker-compose.yml`, a stack inteira
volta sozinha após reboot ou queda de energia.

---

## O que **não** precisa mudar no código

Nada. Vale registrar o porquê:

- **`environment.prod.ts`** usa `apiUrl: '/api'` (relativo) → o front chama o mesmo
  host que o serviu, qualquer que seja o domínio.
- **CORS** (`Configurator.cs:26` libera só `http://localhost:4200`) → irrelevante,
  porque as chamadas passam pelo proxy do nginx e são **same-origin**. Não há preflight.
- **`nginx.conf`** tem `server_name localhost`, mas é o único `server` block do arquivo
  → vira o default server e atende qualquer `Host`.

## Checklist de verificação

```bash
dig +short NS SEUDOMINIO.com.br                    # nameservers da Cloudflare
dig +short SEUDOMINIO.com.br                       # IPs da Cloudflare (104.x / 172.6x)
curl -sI https://SEUDOMINIO.com.br | head -5       # 200 + server: cloudflare
curl -s https://SEUDOMINIO.com.br/api/Providers/active | head -c 200   # JSON
curl -sI http://SEUDOMINIO.com.br | grep -i location                   # 301 → https
systemctl is-active cloudflared docker             # active
docker compose ps                                  # todos healthy
```

## Segurança

Herda o checklist do [`DEPLOY.md`](./DEPLOY.md), com um ponto novo agora que a URL
é pública e indexável:

- [ ] O túnel aponta **só** para `localhost:4200`. Postgres (`5433`) e Redis (`6379`)
      nunca entram no `ingress`.
- [ ] `DemoMode__Enabled: "true"` no `docker-compose.yml` deixa a API **sem
      autenticação** (toda requisição roda como usuário master). Numa URL pública e
      estável isso vira exposição real — avaliar **Cloudflare Access** na frente da
      app, ou rate limiting via WAF (regras básicas são grátis no plano Free).
- [ ] Renovação do domínio em dia — o Registro.br avisa por e-mail, mas o domínio
      cai se vencer.
- [ ] (Opcional) `set_real_ip_from` + `CF-Connecting-IP` no `nginx.conf` para os logs
      registrarem o IP real do visitante em vez do IP do `cloudflared`.

## Custo total

| Item | Valor |
|---|---|
| Domínio `.com.br` (Registro.br) | R$ 40/ano |
| Cloudflare DNS + Tunnel + TLS (Free) | R$ 0 |
| **Total** | **R$ 40/ano** |

## Rollback

Se algo der errado, o caminho de volta é curto:

```bash
sudo systemctl stop cloudflared
cloudflared tunnel --url http://localhost:4200   # volta pro quick tunnel temporário
```

O domínio e a zona continuam intactos; nada no `docker-compose` foi alterado.
