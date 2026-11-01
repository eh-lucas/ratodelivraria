# SPF, DKIM e DMARC do ratodelivraria.com.br

> Fatia 3 do [PLANO-OBSERVADOS.md](PLANO-OBSERVADOS.md) §7. **Isto não é código** — são
> registros no painel de DNS do domínio, e só você pode criar. O que este documento faz é
> dizer exatamente quais, em que ordem, e como conferir que ficaram certos.

## Por que isso importa agora

Enquanto o login era por e-mail, um código que não chegava era o produto inteiro parado.
O login saiu do e-mail (entra-se pelo Google ou por senha), então **isto deixou de ser o
maior risco do plano** — mas o aviso diário de preço continua dependendo de chegar na
caixa de entrada, e não na pasta de lixo eletrônico.

Os três registros respondem perguntas diferentes do servidor que recebe:

| | Pergunta que responde | Sem ele |
|---|---|---|
| **SPF** | Quais servidores podem mandar e-mail dizendo ser deste domínio? | Qualquer um pode se passar por você |
| **DKIM** | Esta mensagem foi mesmo assinada por quem diz? | A mensagem pode ter sido alterada no caminho |
| **DMARC** | O que fazer quando SPF ou DKIM falham? | Cada servidor decide sozinho — e o mais comum é a pasta de spam |

## O estado de hoje da zona (conferido em 2026-11-01)

O domínio **não está em branco** — está trancado. Ele é seu no Registro.br e a zona é da
Cloudflare (`rohin` / `yolanda.ns.cloudflare.com`), e já tem publicado:

```
TXT  ratodelivraria.com.br     v=spf1 -all
TXT  _dmarc.ratodelivraria...  v=DMARC1; p=reject;
MX   ratodelivraria.com.br     0 .            (null MX)
```

É a trava "este domínio não manda e não recebe e-mail" que a Cloudflare publica sozinha.
Lida junto: nenhum servidor pode enviar como `@ratodelivraria.com.br`, e quem receber deve
**rejeitar** a mensagem — não mandar para o spam.

Isso muda o trabalho deste documento. Não é criar registros do zero: é **desfazer uma
trava**, e na ordem certa. Ligar o envio pelo domínio sem mexer nisso não degrada a
entrega, ela é recusada na porta.

**O caminho do Gmail passa por cima disto.** Com `EMAIL_FROM_ADDRESS` apontando para um
endereço `@gmail.com`, o alinhamento DMARC é com o `gmail.com` e estes três registros não
são consultados. Eles só entram em jogo quando o remetente for do domínio.

Para conferir sem instalar nada (o `dig` não está nesta máquina):

```bash
curl -s 'https://dns.google/resolve?name=ratodelivraria.com.br&type=TXT' | python3 -m json.tool
curl -s 'https://dns.google/resolve?name=_dmarc.ratodelivraria.com.br&type=TXT' | python3 -m json.tool
```

## Ordem

**O DMARC é o primeiro a mexer, para afrouxar — e o último a mexer, para apertar.** Como a
zona já está em `p=reject` (seção acima), o primeiro passo é baixar para `p=none`: com
`p=reject` no ar, qualquer erro nos passos seguintes vira mensagem recusada em vez de
relatório. `p=none` só pede relatório; depois de duas semanas lendo os relatórios é que se
aperta de volta.

### 1. Escolher o provedor de envio

Hoje o `appsettings` nasce com `Email:Enabled=false`, e em desenvolvimento quem responde é
o Mailpit. Para produção, a decisão do §8.5 do plano:

| Opção | Esforço | O que você abre mão |
|---|---|---|
| **Gmail + App Password** | ~15 min, **zero DNS** | O remetente vira o endereço `@gmail.com`, e não há alinhamento DMARC com o domínio |
| **Brevo / Resend** | Criar conta e publicar os registros abaixo | Nada; é o caminho completo |

A 20 usuários, o Gmail resolve e este documento inteiro pode esperar. **Ele só passa a ser
necessário quando o remetente for `@ratodelivraria.com.br`.**

### 2. SPF

Um único registro `TXT` na raiz do domínio. **Já existe um** (`v=spf1 -all`, a trava):
edite esse, não crie um segundo — dois registros SPF no mesmo domínio invalidam os dois.

```
Tipo:  TXT
Nome:  @              (ou ratodelivraria.com.br, conforme o painel)
Valor: v=spf1 include:<o include do seu provedor> -all
```

O `include` é o que o provedor manda usar (`include:spf.brevo.com`, `include:_spf.resend.com`,
e assim por diante). O `-all` no fim diz "quem não está nesta lista não é meu" — comece com
`~all` (falha branda) se quiser observar antes de endurecer.

### 3. DKIM

O provedor gera o par de chaves e mostra um registro pronto, quase sempre `CNAME`:

```
Tipo:  CNAME
Nome:  <seletor>._domainkey
Valor: <o host que o provedor indicar>
```

Copie exatamente como o painel do provedor mostra, inclusive o seletor. Depois de publicar,
o provedor tem um botão de verificar — espere ele ficar verde antes de seguir.

### 4. DMARC, de volta ao aperto

Lembrando que afrouxar para `p=none` foi o **primeiro** passo, antes de mexer no SPF — o
registro abaixo é o que deve estar no ar desde o começo desta lista:

```
Tipo:  TXT
Nome:  _dmarc
Valor: v=DMARC1; p=none; rua=mailto:<seu e-mail>; fo=1; adkim=r; aspf=r
```

Duas semanas depois, com os relatórios mostrando que o seu tráfego legítimo passa:

```
v=DMARC1; p=quarantine; pct=100; rua=mailto:<seu e-mail>
```

E, mais adiante, `p=reject`.

### 5. Separar por subdomínio (quando crescer)

Do §8.5 do plano, e vale repetir porque a razão é concreta: **a reputação queimada pelo
aviso diário não pode derrubar a entrega de mensagem crítica.** Quando o volume justificar:

```
auth.ratodelivraria.com.br   mensagem transacional (recuperação de conta)
mail.ratodelivraria.com.br   o aviso diário de preço
```

Cada um com DKIM próprio. Aí um aviso recorrente que irrite muita gente não contamina o
outro canal.

## Conferir

O `dig` não está instalado nesta máquina (nem `drill`, nem `host`). Ou instale o pacote
`bind`, ou use DNS sobre HTTPS, que não precisa de nada:

```bash
d() { curl -s "https://dns.google/resolve?name=$1&type=${2:-TXT}" | python3 -m json.tool; }

d ratodelivraria.com.br                           # deve mostrar UM v=spf1
d _dmarc.ratodelivraria.com.br                    # v=DMARC1
d '<seletor>._domainkey.ratodelivraria.com.br' CNAME
```

DNS leva de minutos a algumas horas para propagar. Se a consulta não mostra, ainda não
chegou — não republique o registro.

Depois de ligar o envio de verdade, o teste que vale é mandar um para uma conta no Gmail e
abrir **"Mostrar original"**: ali aparecem `SPF: PASS`, `DKIM: PASS` e `DMARC: PASS`. Os
três verdes é o que este documento inteiro quer.

## O que o código já faz

Nada aqui exige mudança de C#. O `SmtpEmailSender` fala SMTP puro, e trocar de provedor é
mudar variável de ambiente:

```
Email__Enabled=true
Email__Host=<host do provedor>
Email__Port=587
Email__UseStartTls=true
Email__Username=<usuário>
Email__Password=<senha>
Email__FromAddress=nao-responda@ratodelivraria.com.br
Email__FromName=ratodelivraria
```

E o vigia de preço só começa a mandar e-mail quando `Watch__NotifyEnabled=true` — ele nasce
desligado justamente para que publicar o DNS e ligar o envio sejam duas decisões separadas.
