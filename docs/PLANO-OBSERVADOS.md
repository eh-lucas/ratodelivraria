# Plano: observar livros (login + vigia de preço)

> Escrito em 2026-10-31. Dimensionado em cima dos números já medidos em
> [PLATAFORMA-CEDET.md](PLATAFORMA-CEDET.md) — nada aqui é estimativa nova.

## O que já existe e muda o plano

Antes de desenhar qualquer coisa, o que o repo já entrega:

| Peça | Onde | O que significa para esta feature |
|---|---|---|
| Tela de observados (mock) | `Client/src/app/pages/watched-page/`, `services/watched-service.ts` | A UI já existe em `localStorage`. É trocar o serviço por HTTP, não construir a tela. |
| Sugestão por nome | `GET /api/Catalog/suggest` → `catalog_items` | O "box de pesquisa" do observados é o mesmo box. Custa uma consulta ao banco, zero requisição às lojas. |
| Resolver ISBN | `POST /api/Catalog/{id}/resolve-isbn` | Já abre a página do produto. É aqui que a capa entra de carona. |
| Capa do livro | `queries.image_url` | Toda busca já guarda a capa. Para livro já consultado, a capa sai do banco — nenhuma requisição. |
| Cache por (ISBN, loja) | `QueryRepository.GetCachedQueryAsync`, 60 min | **A peça que faz a feature escalar.** Ver §4. |
| Teto global de requisições | `StoreRequestGate`, 20 simultâneas | O vigia tem que caber dentro desse teto, não ao lado dele. |
| Worker periódico de exemplo | `PopularBooksPrefetcher` (`BackgroundService`) | Molde pronto: delay de startup, espaçamento entre livros, falha sem derrubar nada. |
| Créditos | `users.available_credits`, `CreditService` | Decidir se a observação consome crédito (§3.4). |
| **Nada de e-mail** | — | `Sherlock.Infrastructure` está vazio (só o `.csproj`). É o lugar do `EmailSender`. |

---

## 1. Login: Google, com e-mail e senha para quem não tem

> **Revisto em 2026-10-31.** A versão anterior desta seção defendia código de 6 dígitos
> por e-mail e descartava o link mágico. **O código por e-mail saiu do plano** — não
> porque os argumentos contra o link mágico estivessem errados, mas porque havia uma
> terceira opção melhor que os dois, e ela resolve justamente o risco que a própria
> seção apontava como o maior de todos.

### 1.1 Por que Google

O código por e-mail custava mais e entregava menos:

| | Entrar com Google | Código por e-mail (descartado) |
|---|---|---|
| Backend | validar o ID token, achar ou criar o usuário | `LoginCode` + migration + 3 endpoints + hash + expiração + limite de tentativa + limite de envio |
| Infraestrutura | um OAuth client no Google Cloud | provedor SMTP **e SPF/DKIM/DMARC**, com espera de DNS |
| Risco de entrega | nenhum | **o código cair no spam** — era o risco nº 1 desta seção |
| Primeiro nome | vem no token (`given_name`) | pedir numa tela extra |
| Credencial guardada | nenhuma | o hash do código |

O ponto que decide: o login por e-mail colocava a porta de entrada do site na dependência
de entrega de e-mail, que é a coisa menos confiável de toda a arquitetura. Com o Google,
a porta não depende disso. O remetente de e-mail continua existindo — ele é para o **aviso
diário de queda de preço**, que é o ponto da feature de observados.

### 1.2 E-mail e senha fica, para quem não tem Google

Google sozinho tranca para fora quem usa Outlook, iCloud ou e-mail do trabalho. Então os
dois caminhos convivem na mesma tela, como na maioria dos sites:

```
┌─────────────────────────────┐
│   [  Entrar com Google  ]   │   ← caminho principal
│   ───────── ou ─────────    │
│   E-mail                    │
│   Senha                     │
│   [       Entrar       ]    │
│   Ainda não tem conta?      │
└─────────────────────────────┘
```

Consequência boa: **o login por senha que já existe não é jogado fora.** O
`AuthController` com BCrypt, o `register`/`login` e a tela que já está na marca
continuam valendo. O Google entra ao lado, não no lugar.

### 1.3 O fluxo do Google

```
1. pessoa clica no botão            → Google Identity Services (script no front)
2. escolhe a conta                  → o Google devolve um ID token (JWT assinado por ele)
3. front manda o token               → POST /api/auth/google  { credential }
4. backend valida a assinatura       → GoogleJsonWebSignature.ValidateAsync
5. acha o usuário por e-mail, ou cria com given_name e email
6. devolve o NOSSO JWT de sempre     → daqui para frente nada mais muda
```

O passo 4 não é opcional e não é formalidade: sem validar a assinatura, qualquer um
monta um JSON com o e-mail de outra pessoa e entra como ela. A validação confere a
assinatura do Google, a expiração **e o `aud`** — o ID token tem que ter sido emitido
para o nosso client id, senão um token legítimo obtido em qualquer outro site serve para
entrar aqui.

### 1.4 Juntar as duas contas pelo e-mail

Mesma pessoa, mesmo e-mail, dois caminhos. A regra: **o e-mail é a identidade**.

- Entrou com Google e já existe conta com aquele e-mail → é a mesma pessoa, entra nela.
  Não criar uma segunda.
- Entrou com Google e não existe → cria, com `password_hash` vazio e o `given_name`.
- Conta criada pelo Google tentando entrar por senha → não há senha; a tela diz para
  usar o botão do Google em vez de "senha incorreta", que mandaria a pessoa tentar
  lembrar uma senha que nunca existiu.

Isto é seguro **porque o Google confirma o e-mail**. O ID token traz `email_verified`, e
o login só prossegue se for verdadeiro — sem essa conferência, alguém com uma conta
Google de e-mail não verificado assumiria a conta de outra pessoa.

### 1.5 O que muda no `users`

```
+ first_name   varchar(100)   -- do given_name, ou pedido no cadastro por senha
+ google_id    varchar(64)?   -- o 'sub' do token: estável mesmo se a pessoa trocar de e-mail
  password_hash                -- passa a ser opcional (conta só-Google não tem)
- cpf                          -- segue saindo: nunca é lido (§2.3)
```

`google_id` e não só o e-mail porque o `sub` do Google é o identificador que não muda;
e-mail de conta Google pode mudar.

### 1.6 O que precisa ser feito fora do código

No Google Cloud Console, uma vez: criar um projeto, configurar a tela de consentimento
(externa, só os escopos `email`, `profile`, `openid`) e criar um **OAuth client ID** do
tipo *Web application*, com as origens JavaScript autorizadas:

```
http://localhost:4200          (desenvolvimento)
https://<domínio de produção>
```

O client id é público (vai no front). **Não existe client secret neste fluxo** — o token
é validado por assinatura, não por segredo compartilhado.

### 1.7 A sessão

Vale o que o §8.1 decidiu, e por Google não muda: JWT de acesso curto e refresh token
rotativo. Entrar de novo pelo Google é mais barato que por e-mail, mas ainda é tirar a
pessoa do que ela estava fazendo.

## 2. Segurança dos dados

> Pergunta sua: guardar só nome e e-mail exige cuidado, ou é exagero?

Exige — mas o cuidado não é onde normalmente se imagina.

### 2.1 O ativo perigoso é o login, não o banco

Nome e e-mail, em si, são dado de baixa sensibilidade. O que é perigoso é que **esse
e-mail é a chave da conta**: quem consegue entrar como ele vira a pessoa. Então o esforço
de segurança vai para a porta, não para criptografar a coluna `email`.

Com os dois caminhos do §1, a porta tem duas fechaduras — e elas não custam o mesmo:

| | Quem guarda a credencial | O que temos de fazer |
|---|---|---|
| **Google** | o Google | validar assinatura, `aud` e `email_verified` (§1.3, §1.4). Nada guardado aqui. |
| **E-mail e senha** | nós | BCrypt com work factor, mínimo de 8 caracteres, e **limite de tentativa de login** |

O limite de tentativa é o que falta hoje: o `POST /auth/login` só tem o rate limiter
global por IP (100/min), que é folgado demais para força bruta distribuída em uma senha.
Entra junto com o Google — travar por e-mail depois de N falhas, com espera crescente.

### 2.2 O que **não** fazer

**Não criptografar e-mail no banco.** É o reflexo errado e custa caro:

- Login precisa de `WHERE email = ?`. Com o campo cifrado, ou você usa criptografia
  determinística (que vaza igualdade e não protege de quase nada), ou mantém um índice
  de hash ao lado — e aí o hash é o alvo, o cifrado não protegeu.
- A chave mora no mesmo servidor que o banco. Quem leu o banco leu a chave.
- O que protege de verdade contra "roubaram o dump" é: banco sem porta para a internet,
  usuário de aplicação com privilégio mínimo (**já feito** — `sherlock_app` no
  `scripts/db-init`), backup cifrado, TLS no transporte.

### 2.3 Dois achados concretos no código de hoje

**(a) O e-mail está indo para o log.** `AuthController.cs`:

```csharp
_logger.LogWarning("Tentativa de registro com email já existente: {Email}", normalizedEmail);
_logger.LogWarning("Tentativa de login com email não encontrado: {Email}", normalizedEmail);
```

O Serilog grava em `logs/sherlock-.log` com `retainedFileCountLimit: 30`. Ou seja: a
base de e-mails está em arquivo texto, 30 dias, fora de qualquer controle do banco. Os
outros logs já fazem a coisa certa e registram `{UserId}`. Corrigir junto com o §1 —
**nunca logar e-mail, nome ou código**, nem em `Debug`.

**(b) `users.cpf` existe e não é usado.** É `long Cpf` no `User`, nunca preenchido,
nunca lido. É o único campo de verdade sensível do modelo. **Dropar na migration.**
O melhor jeito de proteger um dado é não ter o dado — e isso vale como resposta à sua
pergunta: a estratégia de segurança aqui *é* a minimização que você já escolheu ao pedir
só nome e e-mail.

### 2.4 LGPD, o mínimo que não dá para pular

- **Base legal**: execução de contrato (a pessoa pediu para ser avisada) para o aviso de
  preço; consentimento só seria necessário se formos mandar outra coisa.
- **"Excluir minha conta"** na tela de perfil, apagando de verdade — `users`,
  `login_codes`, `watched_books`, `tokens`. As `transactions` ficam anonimizadas
  (`user_id` nulo), porque alimentam o ranking.
- **Rodapé de descadastro** em todo e-mail de aviso. O e-mail de código de login não
  precisa (é transacional, pedido pela própria pessoa).
- Nada de analytics de terceiro carregando o e-mail.

---

## 3. A tela de observados

### 3.1 Fluxo

```
1. pessoa digita no mesmo box de busca     → GET /api/Catalog/suggest  (banco, 0 requisição)
2. escolhe um título                       → vira um cartão: capa, título, autor
3. repete para quantos quiser
4. por livro, escolhe a regra              → preço-alvo  OU  queda considerável
5. clica OK                                → POST /api/Watched  (lote)
                                             dispara a busca normal, estabelece o preço de referência,
                                             e passa a observar todo dia
```

A lista não vira entidade. "A lista ativa" é simplesmente as linhas de `watched_books`
com `active = true` daquela pessoa — você pediu o mais simples possível, e um agregado
`WatchList` só acrescentaria uma tabela sem ganhar nada.

### 3.2 A capa, sem requisição extra

Ordem de busca, da mais barata para a mais cara:

1. **`queries.image_url`** pelo ISBN — livro já consultado alguma vez tem capa no banco.
   Custo zero. Como o ranking de mais consultados alimenta esse acervo, a maioria dos
   livros populares cai aqui.
2. **`POST /api/Catalog/{id}/resolve-isbn`** — já abre a página do produto para pegar o
   ISBN. Estender o parser para extrair **capa, autor e editora na mesma leitura**. Uma
   requisição que já estava acontecendo passa a trazer três coisas em vez de uma.
3. Sem capa: placeholder. Não sai requisição só por imagem.

Guardar a URL da capa em `watched_books.image_url` no momento de adicionar — se a loja
tirar o produto do ar depois, o cartão não fica quebrado. (Hotlink da imagem da loja é
aceitável nesta fase; espelhar imagem é problema de outro dia.)

### 3.3 A regra de aviso — "queda considerável" em relação a quê?

Essa é a decisão que faz a diferença entre um aviso útil e um aviso que a pessoa
desliga na segunda semana.

**A queda de ontem para hoje conta — mas sozinha ela mente.** O caso que quebra é um
só, e é justamente o que a nossa própria medição diz ser a norma nessa plataforma:

```
preço normal há um mês ........ R$100
ontem ......................... R$130
hoje .......................... R$105
```

Pela queda diária isso é "−19%, corre que vai acabar". Na prática a pessoa compraria por
R$105 um livro que custa R$100 na maior parte do tempo — **ela sai pior por causa do
nosso e-mail**. E não é hipótese: [PLANO-PRECO-FRESCO.md](PLANO-PRECO-FRESCO.md) registra
que **98,8% dos produtos estão com `special`** (promoção). Preço de referência inflado é
o estado normal dessas lojas, não a exceção.

O custo disso não é incomodar. É que depois de dois ou três avisos que não eram
oportunidade, a pessoa para de abrir o e-mail — e aí o aviso que era de verdade também
passa batido. Um vigia de preço só vale o que vale a confiança no aviso dele.

**A correção não é descartar a queda diária, é pôr um piso nela.** Dispara se
**qualquer** uma das duas:

| | Condição | Pega o quê |
|---|---|---|
| (a) | `hoje <= menor_30d × 0,85` | A queda **acumulada**: 100 → 97 → 94 → 91 → 88 → 85 ao longo da semana. Nenhum dia caiu 15%, mas hoje é o melhor preço do mês. |
| (b) | `hoje <= ontem × 0,85` **e** `hoje <= menor_30d` | A queda **de ontem para hoje** que você pediu, com um guarda-corpo: só avisa se hoje também não estiver mais caro que o melhor preço do mês. |

A segunda linha é a sua regra. O `e hoje <= menor_30d` é a única coisa que acrescentei, e
ele é exatamente o filtro do exemplo de cima: R$105 não passa por R$100. Repare que (b)
ainda pega um caso que a regra do menor-30d sozinha perdia — spike de R$200 e volta para
R$90 com o normal em R$100: é novo piso e caiu forte, avisa.

Os 15% são o padrão, ajustável pela pessoa, e valem para os dois lados da regra.

A série histórica já existe — `queries` guarda `search_isbn`, `price` e `queried_at` de
toda consulta. Nenhuma tabela nova:

```sql
SELECT MIN(price) FROM queries
WHERE search_isbn = @isbn AND success AND queried_at > now() - interval '30 days';
```

Para a regra de preço-alvo é direto: `preço_hoje <= alvo`.

> **Nos primeiros dias a discussão é acadêmica.** Livro recém-adicionado tem histórico de
> um ponto só, então `menor_30d` é o preço de referência da adição e as duas regras dão a
> mesma resposta. O guarda-corpo só começa a filtrar quando já existe histórico — ou
> seja, ele não custa nada no lançamento e protege depois.

### 3.3.1 A variação diária vai para a tela, sempre

O ponto que você levantou vale independente do e-mail: a pessoa quer saber que mexeu.
Então separa os dois canais —

- **A tela é de graça.** Todo cartão mostra a variação desde ontem (`−R$12 desde ontem`)
  e o menor preço do mês ao lado, sempre, tenha disparado aviso ou não. Quem abrir a
  lista vê tudo que se moveu e decide sozinho.
- **O e-mail é escasso.** Um por dia, disputando atenção e reputação de entrega. Esse
  passa pelo filtro.

Isso exige guardar `PreviousPrice` junto com `LastPrice` no `WatchedBook` (§5) — uma
coluna, e some a necessidade de consultar `queries` só para desenhar a seta.

**Antifadiga**, igualmente importante:

- **Um e-mail por pessoa por dia**, com todos os livros que dispararam. Nunca um e-mail
  por livro — é o que detona entrega e paciência ao mesmo tempo.
- Depois de avisar sobre um livro, **silêncio de 7 dias** para ele, a menos que caia
  mais 10% abaixo do preço já avisado.
- Avisou de preço-alvo? O livro entra em pausa automática. O alvo foi atingido; o
  trabalho acabou. A pessoa reativa se quiser.

### 3.4 Limite por pessoa — e nada de crédito

**Busca é de graça para todo mundo, e observar também.** Decidido em 2026-10-31.

Para a observação isso já era o certo por outro motivo: o custo marginal de um livro que
outra pessoa já observa é **zero** (§4.1), então cobrar por ele seria cobrar por trabalho
que não acontece.

O controle é **teto de 20 livros observados por pessoa**. Simples de explicar, e é
exatamente a variável que dimensiona o trabalho noturno.

> **Consequência que precisa de atenção:** os créditos estavam fazendo dois trabalhos ao
> mesmo tempo — preço *e* freio de abuso. Uma busca custa 67 requisições às lojas, e o
> `available_credits` era o que impedia alguém de ficar disparando busca em sequência.
> Tirando a cobrança, **o único freio que sobra é o rate limiter** (token bucket de 20,
> 10/min por usuário no `Program.cs`). Isso provavelmente basta, mas passa a ser a única
> linha de defesa — e o `StoreRequestGate` vira ainda mais importante, porque é ele que
> garante que abuso vire fila e não vire carga nas lojas.
>
> Recomendação: **não apagar o sistema de créditos — parar de bloquear com ele.**
> `CreditService` continua registrando o consumo; só o `HasSufficientCreditsAsync` deixa
> de barrar. O ledger segue útil como telemetria de quanto cada busca custa de verdade, e
> o dia em que a conta apertar, o mecanismo já está no lugar.

## 4. O motor: por que isso não escala por usuário

> Pergunta sua: consultar todos os livros de todos os usuários não sobrecarrega?

Sobrecarregaria se o laço fosse por usuário. **Não vai ser.**

### 4.1 O laço é por ISBN distinto

O custo não é "nº de usuários × nº de livros". É **nº de ISBNs distintos** — porque o
cache já é por `(isbn, provider)` e o `W16Engine` pula a loja cujo resultado ainda está
fresco (`GetCacheFromDatabase`, linha 170). Se 500 pessoas observam *O Hobbit*, ele é
varrido **uma vez**; as outras 499 leem do cache.

```sql
SELECT DISTINCT isbn FROM watched_books WHERE active;
```

Isso é a feature inteira em uma linha. E tem um efeito colateral bom: a varredura
noturna deixa o cache quente, então o livro observado também responde rápido para quem
buscar ele de dia — o vigia e o `PopularBooksPrefetcher` fazem o mesmo trabalho.

### 4.2 A conta, com os números medidos

De [PLATAFORMA-CEDET.md](PLATAFORMA-CEDET.md): busca por ISBN custa **1,8s / 0,7 KB**,
com vazão sustentada de **~21.500 req/h** no caminho leve. Uma varredura completa de um
ISBN = 67 requisições, 166 KB, ~11s.

Tomando **1 req/s** — menos de 20% do que o servidor deles aguenta, deixando o resto
para gente de verdade:

| Modo | Lojas por ISBN | ISBNs por hora | Em 5h (03h→08h) |
|---|---|---|---|
| Varredura completa | 67 | ~54 | **~270 ISBNs** |
| Só as lojas que têm o livro | ~10 | ~360 | **~1.800 ISBNs** |

O modo enxuto é o que torna isso confortável: **depois da primeira varredura sabemos
quais lojas devolveram aquele livro**. Nas seguintes, consultar só essas — e uma
varredura completa semanal por livro para pegar loja que passou a ter estoque.

Com teto de 20 livros por pessoa e muita repetição entre pessoas, ~1.800 ISBNs/noite
cobre uma base na casa dos milhares de usuários sem nenhuma mudança de arquitetura.

### 4.3 Separar a varredura do aviso

Você pediu "todo dia às 9h". Proponho desmembrar, e a razão é concreta:

- **Varrer às 9h em ponto** significa 67 × N requisições de uma vez no mesmo instante em
  que as pessoas começam a usar o site — e o e-mail só sairia ao meio-dia, quando a
  varredura terminasse.
- **Varrer das 03h às 08h, espalhado; mandar o e-mail às 09h.** A pessoa continua
  recebendo às 9h em ponto, que é o que ela percebe. As lojas levam o trabalho diluído
  de madrugada, quando estão ociosas.

Dois workers, não um:

```
WatchSweepWorker    03h00  varre os ISBNs distintos, espaçado, dentro do StoreRequestGate
WatchNotifyWorker   09h00  avalia as regras, monta um e-mail por pessoa, envia
```

### 4.4 Como o worker se comporta

Molde é o `PopularBooksPrefetcher` — `BackgroundService` registrado no
`ServiceCollectionExtensions`, nada de Hangfire ou Quartz nesta escala.

- **Orçamento próprio dentro do `StoreRequestGate`**, não ao lado dele. O vigia usa no
  máximo ~5 das 20 vagas; busca de gente acordada sempre tem vaga. Concretamente: um
  semáforo de 5 no worker, e cada requisição ainda passa pelo gate global.
- **Espaçamento entre livros** (`DelayBetweenBooksSeconds`), igual ao prefetcher.
- **Falha de um ISBN não derruba a rodada** — loga e segue.
- **Trava entre instâncias**: `pg_try_advisory_lock(<id fixo>)` no início da rodada. Com
  uma réplica é inócuo; no dia em que o compose subir duas, evita varredura dobrada.
  São três linhas, e é o tipo de coisa que só se descobre que faltou em produção.
- **Interruptor de desligar** em `appsettings`, como o `Prefetch:Enabled`.

---

## 5. Modelo de dados

Duas tabelas novas, duas colunas novas, uma coluna a menos.

```csharp
public class WatchedBook
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Isbn { get; set; }

    // Congelados na adição: o cartão não depende da loja continuar no ar
    public string Title { get; set; }
    public string? Author { get; set; }
    public string? ImageUrl { get; set; }

    public WatchRule Rule { get; set; }         // TargetPrice | PercentDrop
    public decimal? TargetPrice { get; set; }   // quando Rule = TargetPrice
    public int DropPercent { get; set; } = 15;  // quando Rule = PercentDrop

    public decimal? ReferencePrice { get; set; }   // menor preço na adição
    public DateTime? ReferencePriceAt { get; set; }

    public decimal? LastPrice { get; set; }        // última varredura
    public decimal? PreviousPrice { get; set; }    // a anterior — a seta "desde ontem" na tela (§3.3.1)
    public DateTime? LastCheckedAt { get; set; }
    public decimal? LastNotifiedPrice { get; set; }
    public DateTime? LastNotifiedAt { get; set; }  // base do silêncio de 7 dias

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

Índices: `(user_id, active)` para a tela, `(isbn)` para o `DISTINCT` do worker,
único em `(user_id, isbn)` para não duplicar.

Não há tabela de credencial nova: o Google não guarda nada aqui e a senha continua
no `users.password_hash` que já existe.

Em `users`: `+ first_name varchar(100)`, `+ google_id varchar(64)` (nulo para quem entra
por senha), `password_hash` passa a nullable (nulo para quem entra pelo Google), **`- cpf`**.

**Histórico de preço não precisa de tabela.** A varredura cria `Transaction` + `Query`
como qualquer busca, e a série fica em `queries` — é de lá que sai o menor-30-dias.

---

## 6. Endpoints

```
POST   /api/auth/google      { credential }                 → { token }   ID token do Google
POST   /api/auth/login       { email, password }            → { token }   continua existindo
POST   /api/auth/register    { email, password, firstName } → { token }   continua existindo
POST   /api/auth/refresh                                    → { token }   (cookie)
POST   /api/auth/logout

GET    /api/Watched                                         → lista da pessoa
POST   /api/Watched          { items: [...] }               → adiciona em lote + dispara a busca
PATCH  /api/Watched/{id}     { rule, targetPrice, active }
DELETE /api/Watched/{id}

GET    /api/User/me                                         → { firstName, email, ... }
DELETE /api/User/me                                         → exclusão de conta (§2.4)
```

---

## 7. Ordem de implementação

Cada fatia é mergeável sozinha.

1. ~~**`IEmailSender` + implementação** em `Sherlock.Infrastructure`, com a versão de
   desenvolvimento que só loga.~~ **Feito** — ver §9.
2. **Entrar com Google**: OAuth client no Google Cloud, `POST /api/auth/google`,
   validação do ID token, `first_name` e `google_id`, drop do `cpf`, botão na
   `login-page` ao lado do formulário de senha, e tirar o e-mail dos logs.
3. **SPF/DKIM/DMARC no domínio.** Deixou de ser o maior risco do plano quando o login
   saiu do e-mail, mas o aviso diário de preço ainda depende disso.
4. **Refresh token** sobre a tabela `tokens`.
5. **`WatchedBook` + CRUD**, e o `watched-service.ts` sai do `localStorage` para HTTP.
   A tela praticamente não muda.
6. **Capa e metadados** no `resolve-isbn`, com o `queries.image_url` como primeira
   parada.
7. **`WatchSweepWorker`** (03h, modo enxuto, advisory lock, orçamento no gate).
8. **`WatchNotifyWorker`** (09h, digest, antifadiga, descadastro).
9. ~~**`demoMode`**: busca segue aberta, login passa a ser exigido só em `/watched` e
   `/profile`.~~ **Feito** — ver §10.

Entre a 5 e a 7 a feature já é usável manualmente — dá para validar a tela antes de
escrever uma linha de worker.

---

## 8. Decisões — fechadas

### 8.1 Sessão: refresh token rotativo

**Decidido: JWT de acesso de 30 min + refresh token opaco e rotativo na tabela `tokens`,
em cookie `HttpOnly`.** Não o JWT de 30 dias.

Dois fatos empurram para cá:

- ~~A conta tem valor em dinheiro.~~ **Cai**: com a busca liberada para todos (§3.4),
  não há saldo a roubar. Era metade do meu argumento e não vale mais — a decisão se
  mantém pelos outros dois, não por este.
- **O cookie sai de graça em produção.** O `environment.prod` aponta para `/api` — mesmo
  nginx, mesma origem, cookie funciona sem nada. Em dev o CORS já tem
  `AllowCredentials()` configurado no `Configurator`. A infraestrutura do caminho "certo"
  já está montada; só falta usar.

E o argumento que de fato decide: **sem senha, expirar sessão custa uma ida ao
e-mail.** Com rotação, quem usa o site nunca é deslogado — a sessão se renova sozinha.
Com JWT de 30 dias fixo, no dia 31 a pessoa cai no meio de uma busca e tem que abrir a
caixa de entrada. Passwordless existe para tirar fricção; não faz sentido reintroduzi-la
no outro lado.

Isto também é o que faz "sair de todos os aparelhos" e a exclusão de conta (§2.4)
funcionarem de verdade em vez de só sumirem da tela.

### 8.2 Horário: varrer 03h→08h, avisar 09h — e tudo em UTC

**Decidido: varredura das 03h às 08h, e-mail às 09h, horário de Brasília.**

O detalhe que não pode passar: **o servidor roda em UTC e o commit `406c453
("fix(ranking): marco de reset em UTC")` mostra que este repo já se queimou exatamente
nisso.** Então a configuração guarda UTC, explicitamente:

| Evento | Brasília | UTC (o que vai no `appsettings`) |
|---|---|---|
| Início da varredura | 03h00 | **06h00** |
| Fim da janela | 08h00 | **11h00** |
| Envio do digest | 09h00 | **12h00** |

O Brasil não tem horário de verão desde 2019, então `America/Sao_Paulo` é −03:00 fixo e a
conversão é constante — nada de `TimeZoneInfo` em tempo de execução, só o comentário
explicando de onde veio o número.

### 8.3 Frete: fica de fora — e o `CLAUDE.md` está errado

**Decidido: a comparação é no preço do livro. Frete não entra.**

A pergunta estava mal formulada da minha parte, porque eu assumi o que o `CLAUDE.md`
afirma:

> `Each has: Id, Name, Url, ProviderCategoryEnum, MinFreeShipping, BaseShippingCost, ...`

**Nenhum desses dois campos existe.** `Provider` tem `Id`, `Url`,
`ProviderCategoryEnum`, `Name`, `IsActive`, `SearchUrlTemplate` — e mais nada. O
`includeShipping` do exemplo de request do `/api/Cart/optimize` também não é lido em
lugar nenhum. Busca por `shipping` e `frete` em todo o `.cs` do repo: **zero
ocorrências.**

Ou seja, não era escolher entre duas bases de comparação: era construir um subsistema de
frete do zero. E frete depende de CEP, o que significa raspar cotação por CEP por loja —
uma varredura maior que a do preço, para um número que muda por pessoa.

Fica para quando o frete existir no produto. Quando existir, o caso que obriga a
revisitar é o do frete grátis por valor mínimo: livro cai de R$105 para R$95 e cruza para
baixo o mínimo de R$100, o total sobe de R$105 para R$115 e o nosso "−10%" piora a vida
da pessoa. É a mesma família do falso desconto do §3.3, e merece o mesmo guarda-corpo.

> **Ação à parte:** corrigir o `CLAUDE.md`. Esses três pontos estão descrevendo código
> que não existe, e é o arquivo que orienta quem chega no repo.

### 8.4 Teto de 20 e intervalo diário: confirmados, sem ação

**Decidido: 20 livros por pessoa, varredura diária, nada a fazer agora.**

Fica registrado só o número que dimensiona isso, para quando fizer falta: a janela de 5h
no modo enxuto comporta **~1.800 ISBNs distintos por noite**, o que com 20 livros por
pessoa e **nenhuma** sobreposição dá **90 usuários**. Na prática a sobreposição é alta —
o repo tem ranking de mais consultados e prefetcher construídos justamente porque a
procura é concentrada em poucos títulos.

Quando apertar, os botões em ordem, antes de mexer em arquitetura:

| Lever | De → para | Capacidade |
|---|---|---|
| hoje | 5h, 1 req/s | ~1.800 ISBNs |
| alargar a janela | 01h→08h (7h) | ~2.500 ISBNs |
| subir o ritmo | 2 req/s (as lojas sustentam ~6) | ~5.000 ISBNs |

A única coisa a implementar agora é o **log da rodada**: nº de ISBNs distintos, duração e
quantos sobraram sem varrer. Enquanto o terceiro for zero, não há decisão a tomar — e
quando deixar de ser, o aviso chega antes do problema.

E o comportamento quando não couber: o worker **prioriza, não falha**. Varre primeiro os
livros com preço-alvo próximo e os que se mexeram nos últimos 7 dias; o resto escorrega
para o dia seguinte. Degradar para "verificado a cada 2 dias" é melhor que estourar a
janela e entrar no horário de pico.

### 8.5 E-mail: o código é nosso, a entrega não

A pergunta foi "dá para fazermos nosso próprio sistema de envio?". A resposta depende de
separar duas coisas que a pergunta junta:

| | O que é | Dificuldade |
|---|---|---|
| **(a)** Escrever o código que envia | `IEmailSender` + MailKit + os templates | **Fácil. ~meio dia. E vamos fazer.** |
| **(b)** Operar o servidor que *entrega* | MTA próprio (Postfix/Maddy) falando com Gmail e Outlook | **Nesta máquina, impossível. Em VPS, caro em tempo para sempre.** |

A parte (a) nunca esteve em questão — é nossa de qualquer jeito. O problema é a (b).

#### Por que "deixar rodando localmente" não entrega

O [DEPLOY.md](DEPLOY.md) diz onde isso roda: **um PC de casa, atrás de Cloudflare
Tunnel, sem abrir portas do roteador**. Três consequências, nessa ordem:

1. **O túnel não ajuda em nada aqui.** Cloudflare Tunnel resolve tráfego *de entrada*
   (HTTP). E-mail que sai é conexão *de saída* na porta 25 — o túnel não participa.
2. **A porta 25 de saída é bloqueada pelo provedor.** Praticamente todo ISP residencial
   brasileiro bloqueia, justamente para conter máquina infectada mandando spam. O
   pacote não sai da rede.
3. **IP residencial está na Spamhaus PBL — por desenho.** A PBL não é lista de punição:
   é a faixa que o *próprio ISP* declara como "daqui não sai e-mail legítimo". Gmail,
   Outlook e Yahoo **recusam na conexão**. Não é cair no spam; é não ser aceito.

Ou seja, não é "difícil": nessa topologia **não funciona**, e nenhuma quantidade de
código resolve, porque o obstáculo não é código.

#### E numa VPS, com IP próprio?

Aí passa a ser possível, e vira trabalho permanente: PTR/rDNS batendo com o HELO,
SPF+DKIM+DMARC (que precisaríamos de qualquer forma), **tratamento de bounce e lista de
supressão** (sem isso você insiste em endereço morto e afunda a própria reputação), e
monitoramento de blacklist — IP novo não tem reputação, e o Outlook em particular
costuma descartar silenciosamente mensagem de IP sem histórico por semanas.

Estimativa honesta: **2 a 5 dias para subir, e manutenção para sempre** — com entrega
irregular nos primeiros meses, que é exatamente o período em que o produto não pode
falhar.

#### O argumento que fecha

**O código de login é a porta de entrada.** Se ele não chega, ninguém usa o site — não é
uma funcionalidade degradada, é o produto inteiro parado. Auto-hospedar significa pôr a
mensagem mais crítica e menos tolerante a falha no canal menos confiável que temos. É o
inverso de onde se quer correr risco.

#### O que fazemos então

```
Sherlock.Business/Interfaces/IEmailSender.cs       ← nossa interface
Sherlock.Infrastructure/Email/SmtpEmailSender.cs   ← MailKit, ~60 linhas, nossa
Sherlock.Infrastructure/Email/Templates/           ← nossos templates
```

Um `SmtpEmailSender` falando SMTP puro. Quem está do outro lado é configuração:

| Ambiente | Do outro lado | Observação |
|---|---|---|
| **dev** | **Mailpit** no `docker-compose.dev.yml` | ~10 linhas de YAML. Captura tudo, interface web em `:8025`, **nenhum e-mail sai da máquina**. É literalmente "nosso sistema rodando localmente" — e para desenvolver é melhor que qualquer provedor. |
| **prod** | relay SMTP da Brevo | 4 variáveis de ambiente. |

**Zero lock-in.** Trocar de provedor é mudar host, porta e credencial no `.env` — nenhuma
linha de C# muda. Brevo hoje, SES quando passar de ~10k/mês, MTA próprio se um dia houver
VPS e vontade. A decisão fica reversível porque a abstração é nossa; o que delegamos é só
a reputação de IP, que é a única coisa que não se escreve.

#### Uma ressalva sobre "um serviço separado que o ratodelivraria aciona"

Mesmo que um dia o MTA seja nosso, **o remetente deve continuar dentro do processo da
API**. Quebrar o envio num serviço à parte acrescenta um salto de rede, um esquema de
autenticação entre os dois e um modo de falha novo — "o serviço de e-mail caiu, ninguém
consegue entrar no site" — sem nada em troca. O que faz sentido isolar é a *fila* (tentar
de novo quando o envio falha), e isso é uma tabela e um `BackgroundService`, não um
serviço separado.

#### Provedor: a 20 usuários, a conta muda

Com **no máximo 20 usuários**, são ~20 e-mails por dia. O limite diário deixa de ser a
restrição, e com isso **cai o motivo que eu tinha dado para preferir Brevo a Resend** —
eu havia escolhido pelo teto de 100/dia do Resend, que a 20 usuários sobra cinco vezes.

Nessa escala o mais rápido de pôr no ar hoje é **SMTP do Gmail com App Password**:

| | Para funcionar hoje | Entrega |
|---|---|---|
| **Gmail + App Password** | Ativar 2FA na conta, gerar a senha de app. **~15 min, zero DNS.** | Ótima — é a infraestrutura do próprio Gmail. 500/dia. |
| Brevo / Resend | Criar conta, verificar remetente, idealmente DKIM no domínio (espera de DNS). | Ótima, e com painel de entrega. |

```
Email__Host=smtp.gmail.com   Email__Port=587   Email__UseStartTls=true
Email__Username=<sua conta>  Email__Password=<app password, 16 letras>
```

O que você abre mão com o Gmail: o remetente é o endereço `@gmail.com` (o Gmail reescreve
o *From* se não for um alias verificado), não há painel de entrega, e não há alinhamento
de DMARC com `ratodelivraria.com.br`. Para 20 pessoas que te conhecem, nada disso pesa.
Quando pesar, é trocar quatro variáveis de ambiente — **nenhuma linha de C#** — e aí vale
voltar para a separação por subdomínio abaixo.

> O resto continua valendo para quando crescer: `auth.` para o código de login e `mail.`
> para o aviso diário, com DKIM próprio em cada, porque a reputação queimada pelo aviso
> recorrente não pode derrubar a entrega do código de login.

---

## 9. O que já está implementado

A fatia 1 do §7 está pronta e no repo (2026-10-31).

| Arquivo | O que é |
|---|---|
| `Sherlock.Business/Interfaces/IEmailSender.cs` | A interface + `EmailSendException` |
| `Sherlock.Infrastructure/Email/EmailSettings.cs` | Configuração (seção `Email`) |
| `Sherlock.Infrastructure/Email/SmtpEmailSender.cs` | MailKit, SMTP puro, 3 tentativas com espera de 1s e 3s |
| `Sherlock.Infrastructure/Email/LogEmailSender.cs` | Padrão: escreve no log, não envia nada |
| `Sherlock.Infrastructure/ServiceCollectionExtensions.cs` | `AddInfrastructure`, escolhe os dois pelo `Email:Enabled` |
| `Sherlock.Tests/Infrastructure/` | `FakeSmtpServer` (SMTP em processo) + 5 testes |

Mais: `Sherlock.Api` passou a referenciar `Sherlock.Infrastructure`, o `Configurator`
chama `AddInfrastructure`, e o **Mailpit** entrou no `docker-compose.dev.yml`.

**O padrão é não enviar nada.** `Email:Enabled` é `false`, então quem clonar o repo roda
o `LogEmailSender` — a mensagem aparece no log da API (dá para pegar o código de login e
entrar) e nenhuma conexão sai da máquina. O `LogEmailSender` avisa no startup, porque em
produção esse log significa que ninguém consegue entrar.

### Verificar

```bash
dotnet test Sherlock.Tests/Sherlock.Tests.csproj --filter "FullyQualifiedName~Infrastructure"
```

Os testes sobem um servidor SMTP de mentira em processo e exercitam o MailKit de verdade
— socket, EHLO/MAIL/RCPT/DATA, `multipart/alternative` e a repetição quando a conexão
cai. Sem Docker e sem rede.

Para ver o e-mail montado, com HTML:

```bash
docker compose -f docker-compose.dev.yml up -d mailpit   # SMTP :1025, web :8025
# e na API:  Email__Enabled=true
```

> **Não verifiquei o caminho do Mailpit nesta máquina** — o Docker aqui pede `sudo`. Os
> arquivos de compose passam no `docker compose config`, e o protocolo é o mesmo que os
> testes exercitam, mas a primeira subida do Mailpit continua por conferir.

---

## 10. Acesso aberto: o que é público e o que exige conta

Feito em 2026-10-31, junto com a tela de login.

Até aqui o `authGuard` cobria o layout inteiro e `/` desviava para `/login`: **ninguém
via o site sem ter conta**, numa home que promete "sem cadastro". O site no ar não sofria
disso porque a branch `feat/demo-master-token` autentica todo visitante como um usuário
master — o que resolve o acesso criando um problema pior, abaixo.

A fronteira agora é uma só pergunta: **o conteúdo é de uma pessoa?**

| | Rota / endpoint | Por quê |
|---|---|---|
| **Aberto** | home, busca, resultado, `/api/BookSearch`, `/api/Cart`, `/api/Catalog`, `/api/Providers`, ranking | Comparar preço é o que o site faz. A transação é gravada com `user_id` nulo — o ranking continua alimentado sem prender a busca a ninguém. |
| **Convida a entrar** | `/watched`, `/history` | A página abre e mostra "entre para continuar". Guard desviaria para o login escondendo que ela existe. |
| **Desvia para o login** | `/profile`, `/credits`, `/api/User`, `/api/Transactions` | Não têm estado deslogado que faça sentido. |

### O histórico de todos

O backend **sempre** filtrou certo — `GetUserHistoryAsync` chama
`GetByUserIdAsync(userId, limit)`, e o detalhe confere posse. O que faz o histórico
aparecer como "o geral de todos" é o `DemoAuthenticationHandler` da branch de demo: se
todo visitante é o mesmo usuário master, o histórico dele é, literalmente, o de todos.

**É o motivo para não levar aquele handler para a main.** Com acesso aberto de verdade,
busca anônima grava `user_id` nulo e não aparece no histórico de ninguém.

`Sherlock.Tests/Integration/AcessoAbertoTests.cs` fixa essa fronteira, inclusive o caso
de duas contas novas verem cada uma o próprio histórico vazio.

### Crédito deixou de barrar

Os três portões de 402 saíram do `BookSearchController` (§3.4: busca é de graça). O
`CreditService` continua registrando o consumo de quem tem conta — vira medida de quanto
cada busca custa, e o mecanismo fica no lugar para o dia em que precisar voltar.
