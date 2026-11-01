/**
 * Dicionário PT -> EN.
 *
 * A chave é o próprio texto em português usado no template: assim não é preciso
 * inventar identificadores, e qualquer string ausente daqui simplesmente aparece
 * em português em vez de quebrar a tela.
 */
export const EN: Record<string, string> = {
  // Navegação e ações gerais
  'Pesquisar': 'Search',
  'Pesquisar livros': 'Search books',
  'Pesquisar um livro': 'Search for a book',
  'Observados': 'Watched',
  'Histórico': 'History',
  'Meu histórico': 'My history',
  'Meu perfil': 'My profile',
  'Meu Perfil': 'My Profile',
  'Sair': 'Sign out',
  'Fazer login': 'Sign in',
  'Criar conta': 'Create account',
  'Abrir menu': 'Open menu',
  'Menu do usuário': 'User menu',
  'Fechar': 'Close',
  'Cancelar': 'Cancel',
  'Remover': 'Remove',
  'Adicionar': 'Add',
  '+ Adicionar': '+ Add',
  'Limpar': 'Clear',
  'Limpar filtros': 'Clear filters',
  'Nova busca': 'New search',
  'Consultar de novo': 'Search again',
  'Tentar de novo': 'Try again',
  'Tentar novamente': 'Try again',
  'Ir à loja': 'Go to store',
  'Anterior': 'Previous',
  'Avançar': 'Next',
  'Proxima': 'Next',
  'Ordenar': 'Sort',
  'Ações': 'Actions',
  '← Voltar ao Perfil': '← Back to Profile',

  // Busca
  'ISBN do livro': 'Book ISBN',
  'Digite o ISBN e pressione Enter': 'Type the ISBN and press Enter',
  'Esse número não parece um ISBN (10 ou 13 dígitos). Você pode buscar assim mesmo.':
    'That does not look like an ISBN (10 or 13 digits). You can search anyway.',
  'Nenhum livro adicionado ainda.': 'No books added yet.',
  'Sites a consultar': 'Sites to search',
  'Sites consultados': 'Sites searched',
  'O rato está garimpando os melhores preços em': 'The rat is digging up the best prices across',
  'site diferente': 'different site',
  'sites diferentes': 'different sites',
  'Filtrar sites...': 'Filter sites...',
  'Filtrar por nome...': 'Filter by name...',
  'Nenhum site com esse nome.': 'No site with that name.',
  'Buscas recentes': 'Recent searches',
  'Todos': 'All',
  'Nenhum': 'None',
  'Custo estimado:': 'Estimated cost:',
  'Custo estimado por busca:': 'Estimated cost per search:',
  'Buscas restantes (estimado):': 'Searches remaining (estimated):',
  'Aumentar quantidade': 'Increase quantity',
  'Diminuir quantidade': 'Decrease quantity',
  'Não foi possível concluir a busca': 'Could not complete the search',

  // Resultados
  'Melhor resultado': 'Best result',
  'Nenhuma oferta com os filtros atuais.': 'No offers match the current filters.',
  'Mostrar só o que não trouxe resultado': 'Show only those with no result',
  'Só sites com todos os livros': 'Only sites with all books',
  'Preço': 'Price',
  'Preço até': 'Price up to',
  'Menor preço': 'Lowest price',
  'Maior preço': 'Highest price',
  'Menor': 'Lowest',
  'Maior': 'Highest',
  'Médio': 'Average',
  'Mediana': 'Median',
  'Economia': 'Savings',
  'Por livro': 'Per book',
  'total do carrinho': 'cart total',
  'Título': 'Title',
  'Título retornado': 'Returned title',
  'Site': 'Site',
  'Sites': 'Sites',
  'Tempo': 'Time',
  'Tempo total': 'Total time',
  'Resposta média': 'Average response',
  'Responderam': 'Responded',
  'Falharam': 'Failed',
  'Do cache': 'From cache',
  'cache': 'cache',
  'Com o livro': 'With the book',
  'Com sucesso': 'Successful',
  'Esta consulta': 'This query',
  '— sem resultado': '— no result',
  '✓ encontrado': '✓ found',
  'Resultado desta sessão, sem custo adicional.': 'Result from this session, at no extra cost.',

  // Observados
  'Meus livros observados': 'My watched books',
  'Nenhum livro observado ainda.': 'No watched books yet.',
  'Livro': 'Book',
  'Preço alvo': 'Target price',
  'Preço-alvo (R$)': 'Target price (R$)',
  'Preço hoje': 'Price today',
  'Desde ontem': 'Since yesterday',
  'Melhor em 30 dias': 'Best in 30 days',
  'sem mudança': 'unchanged',
  'Última consulta': 'Last query',
  'Avisar quando': 'Alert me when',
  'o preço cair bastante': 'the price drops a lot',
  'chegar a um preço que eu escolher': 'it reaches a price I choose',
  'Queda de (%)': 'Drop of (%)',
  'Salvando…': 'Saving…',
  'Carregando…': 'Loading…',
  'pendente': 'pending',
  'Não foi possível carregar sua lista. Tente novamente.':
    'We could not load your list. Please try again.',
  'Não foi possível salvar. Tente novamente.': 'We could not save it. Please try again.',
  'Informe um ISBN de 10 ou 13 dígitos.': 'Enter a 10 or 13 digit ISBN.',
  'O rato confere o preço desses livros todo dia de madrugada. Quando valer a pena, ele te avisa por e-mail — um aviso por dia, com tudo que caiu.':
    'The mouse checks these prices every night. When it is worth it, you get an email — one a day, with everything that dropped.',
  'O aviso só sai se o preço de hoje também for o melhor dos últimos 30 dias — assim você não compra caro por causa de uma promoção que subiu antes de cair.':
    'The alert only goes out if today is also the best price in 30 days — so you do not overpay because of a sale that went up before coming down.',

  // Histórico
  'Carregando histórico...': 'Loading history...',
  'Carregando detalhes...': 'Loading details...',
  'Detalhes da consulta': 'Query details',
  'Você ainda não fez nenhuma consulta.': 'You have not made any queries yet.',
  'Consulta': 'Query',
  'Data': 'Date',
  'Status': 'Status',
  'Tipo': 'Type',
  'Custo': 'Cost',
  'Valor': 'Amount',

  // Perfil e créditos
  'Créditos': 'Credits',
  'creditos': 'credits',
  'creditos disponiveis': 'available credits',
  'Meus Creditos': 'My Credits',
  'Saldo': 'Balance',
  'Saldo Atual': 'Current Balance',
  'Comprar créditos': 'Buy credits',
  'Comprar Creditos': 'Buy Credits',
  'Historico de Creditos': 'Credit History',
  'Nenhuma transacao encontrada.': 'No transactions found.',
  'Nenhum pacote disponivel no momento.': 'No packages available right now.',
  'Escolha o pacote ideal para suas necessidades': 'Choose the package that fits your needs',
  'Informacoes do Usuario': 'User Information',
  'Gerencie suas informacoes e creditos': 'Manage your information and credits',
  'Descricao': 'Description',
  'Total utilizado:': 'Total used:',
  'Teste Grátis.': 'Free trial.',
  'Usuário': 'User',
  'Senha': 'Password',
  'Nome:': 'Name:',
  'Email:': 'Email:',

  // Mensagens vindas do TypeScript (erros e rótulos dinâmicos)
  'Erro ao carregar histórico.': 'Failed to load history.',
  'Não foi possível carregar a lista de sites.': 'Could not load the list of sites.',
  'Não foi possível carregar os dados do usuário.': 'Could not load user data.',
  'Nao foi possivel carregar os pacotes de creditos.': 'Could not load credit packages.',
  'Erro ao processar a compra. Tente novamente.': 'Could not process the purchase. Try again.',
  'Informe um preço-alvo válido.': 'Enter a valid target price.',

  // Substantivos contáveis (usados pelo pipe 'tp')
  'livro': 'book',
  'livros': 'books',
  'livraria': 'bookstore',
  'livrarias': 'bookstores',
  'site': 'site',
  'sites': 'sites',
  'crédito': 'credit',
  'créditos': 'credits',
  'sem resposta': 'unanswered',

  // Aviso da loja que ficou muda em algum livro do carrinho
  'pode sair mais barata': 'may be cheaper',

  // Busca por nome no formulario de observados
  'Escolha um título da lista.': 'Pick a title from the list.',
  'Nenhum título encontrado. Tente outro nome ou informe o ISBN.':
    'No title found. Try another name or enter the ISBN.',
  'O total acima está incompleto e pode ser menor.': 'The total above is incomplete and may be lower.',

  // Fragmentos de frases com números interpolados
  'Informe um ou mais ISBNs e compare o preço em': 'Enter one or more ISBNs and compare the price across',
  'dezenas de livrarias': 'dozens of bookstores',
  'de uma vez.': 'at once.',
  'na busca': 'in this search',
  'todos os': 'all',
  'carregando...': 'loading...',
  'de': 'of',
  'Consultando': 'Searching',
  'em paralelo. Isso leva alguns segundos.': 'in parallel. This takes a few seconds.',
  'Nenhum site encontrou': 'No site found',
  'os livros': 'the books',
  'o livro': 'the book',
  'Consultamos': 'We searched',
  'e nenhum retornou preço para': 'and none returned a price for',
  'Confira o ISBN ou veja o que cada site respondeu abaixo.': 'Check the ISBN or see what each site answered below.',
  'Refazer busca': 'Search again',
  'Não encontrado em nenhum site:': 'Not found on any site:',
  'abaixo da média': 'below average',
  'acima da média': 'above average',
  'Resultados por site': 'Results per site',

  'de economia': 'savings',
  'Pagina': 'Page',
  'oferta': 'offer',
  'ofertas': 'offers',
  'consulta': 'query',
  'consultas': 'queries',
  'sem erro': 'without error',

  // Doação (Pix)
  'Me compre um café': 'Buy me a coffee',
  'O site é gratuito e sem anúncios. Se ele te economizou dinheiro, considere ajudar a manter o servidor no ar.':
    'This site is free and ad-free. If it saved you money, consider helping keep the server running.',
  'Outro valor': 'Other amount',
  'Deixe em branco para escolher no banco': 'Leave blank to choose in your bank app',
  'QR code Pix': 'Pix QR code',
  'Escaneie com o app do seu banco': 'Scan with your bank app',
  'Escaneie e escolha o valor no seu banco': 'Scan and choose the amount in your bank app',
  'Copiar código Pix': 'Copy Pix code',
  'Código copiado!': 'Code copied!',
  'O pagamento vai direto para a conta, sem intermediários.': 'The payment goes straight to the account, with no middleman.',
  'Achou útil?': 'Found it useful?',
  'Ajude a manter o site no ar': 'Help keep the site running',

  'Digite o nome do livro ou o ISBN': 'Type the book name or the ISBN',
  'buscando ISBN...': 'looking up ISBN...',
  'Não foi possível identificar o ISBN deste título.': 'Could not identify the ISBN for this title.',

  'Seu rato ainda não tem a capacidade de observar livros.':
    'Your mouse cannot watch books yet.',
  'Aguarde a nova versão em desenvolvimento.': 'A new version is on the way.',

  'consultados': 'searched',

  // Apresentação da home
  'Compare o preço do mesmo livro em': 'Compare the price of the same book across',
  'de uma vez': 'at once',
  'Um rato de livraria faz o trabalho chato por você: consulta loja por loja, ao vivo, e mostra onde sai mais barato. Sem cadastro e sem anúncios.':
    'A bookworm mouse does the boring part for you: it checks store by store, live, and shows where it costs less. No sign-up, no ads.',
  'Monte sua lista': 'Build your list',
  'Digite o nome do livro ou o ISBN. Pode ser um só ou o carrinho inteiro.':
    'Type the book name or the ISBN. One book or a whole cart.',
  'O rato corre atrás': 'The mouse runs after it',
  'Consultamos todas as livrarias selecionadas ao mesmo tempo, com preço do momento.':
    'We check every selected bookstore at the same time, with live prices.',
  'Compare e compre': 'Compare and buy',
  'Veja o preço de cada loja e qual delas fecha o carrinho inteiro por menos.':
    'See each store price and which one closes the whole cart for less.',
  'mais de': 'more than',
  'as primeiras respostas levam alguns segundos': 'the first answers take a few seconds',

  // Tela de login
  'Entrar': 'Sign in',
  'Entrando…': 'Signing in…',
  'E-mail': 'Email',
  'Acesse para ver seus livros observados e seu histórico.':
    'Sign in to see your watched books and your history.',
  'Informe seu e-mail.': 'Enter your email.',
  'E-mail inválido.': 'Invalid email.',
  'Informe sua senha.': 'Enter your password.',
  'E-mail ou senha incorretos.': 'Wrong email or password.',
  'Não foi possível entrar agora. Tente de novo em instantes.':
    'Could not sign you in right now. Try again in a moment.',
  'Ainda não tem conta?': "Don't have an account?",

  // Páginas legais
  'Política de Privacidade': 'Privacy Policy',
  'Termos de Uso': 'Terms of Use',
  'Ao criar conta você concorda com os': 'By creating an account you agree to the',
  'e a': 'and the',

  // Telas que exigem conta
  'Entre para ver seu histórico': 'Sign in to see your history',
  'O histórico é só seu: mostra as buscas que você fez, com o preço de cada loja no dia da consulta.':
    'The history is yours alone: it shows the searches you made, with each store price on the day you searched.',
  'Entre para observar livros': 'Sign in to watch books',
  'A lista de observados é sua, e o aviso de queda de preço vai para o seu e-mail.':
    'The watch list is yours, and the price-drop alert goes to your email.',
  'Entre para ver esta página': 'Sign in to see this page',
  'É preciso ter uma conta para continuar.': 'You need an account to continue.',

  // Entrar com Google
  'ou': 'or',
  'Esta conta entra pelo Google. Use o botão acima.':
    'This account signs in with Google. Use the button above.',
  'Não foi possível entrar com o Google. Tente de novo em instantes.':
    'Could not sign you in with Google. Try again in a moment.',
  'Não foi possível validar sua conta Google.': 'Could not validate your Google account.',
  'Sua conta Google precisa ter o e-mail verificado.':
    'Your Google account needs a verified email.',
  'Esta conta está desativada.': 'This account is disabled.',

  // Tela de criar conta
  'Criando…': 'Creating…',
  'Primeiro nome': 'First name',
  'Repita a senha': 'Repeat the password',
  'Conta serve para observar livros e guardar seu histórico. Buscar preço continua sem cadastro.':
    'An account is for watching books and keeping your history. Searching prices stays sign-up free.',
  'Informe seu primeiro nome.': 'Enter your first name.',
  'A senha precisa de pelo menos 8 caracteres.': 'The password needs at least 8 characters.',
  'Pelo menos 8 caracteres.': 'At least 8 characters.',
  'As senhas não são iguais.': 'The passwords do not match.',
  'Repita a senha.': 'Repeat the password.',
  'Já existe conta com este e-mail. Tente entrar.':
    'An account with this email already exists. Try signing in.',
  'Não foi possível criar sua conta agora. Tente de novo em instantes.':
    'Could not create your account right now. Try again in a moment.',
  'Já tem conta?': 'Already have an account?',
};
