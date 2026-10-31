import { Routes } from '@angular/router';
import { LoginPage } from './pages/login-page/login-page';
import { RegisterPage } from './pages/register-page/register-page';
import { HomePage } from './pages/home-page/home-page';
import { SearchPage } from './pages/search-page/search-page';
import { ResultsPage } from './pages/results-page/results-page';
import { WatchedPage } from './pages/watched-page/watched-page';
import { HistoryPage } from './pages/history-page/history-page';
import { ProfilePage } from './pages/profile-page/profile-page';
import { CreditsPage } from './pages/credits-page/credits-page';
import { PrivacyPage } from './pages/legal/privacy-page';
import { TermsPage } from './pages/legal/terms-page';
import { AppLayoutComponent } from './layout/app-layout';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  // O site é aberto: quem chega cai na apresentação, não num formulário de login.
  // Antes '' ia para /login e o authGuard cobria o layout inteiro, então ninguém
  // via o site sem ter conta — numa home que promete "sem cadastro".
  { path: '', redirectTo: '/home', pathMatch: 'full' },

  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },

  {
    path: '',
    component: AppLayoutComponent,
    children: [
      // Abertas a qualquer visitante
      { path: 'home', component: HomePage },
      { path: 'search', component: SearchPage },
      { path: 'resultado', component: ResultsPage },

      // Páginas legais. Abertas de propósito e sem guard: o Google exige a URL da
      // política de privacidade publicamente acessível para publicar o app de
      // login, e não adianta exigir conta para ler o documento que explica o que
      // acontece com os dados de quem ainda vai criar a conta.
      { path: 'privacidade', component: PrivacyPage },
      { path: 'termos', component: TermsPage },

      // Conteúdo de uma pessoa só. Não levam guard de propósito: a página abre e
      // mostra "entre para continuar" em vez de desviar para /login escondendo
      // que ela existe. Quem busca os dados é o componente, e só se houver sessão.
      { path: 'watched', component: WatchedPage },
      { path: 'history', component: HistoryPage },

      // Estas não têm estado deslogado que faça sentido — perfil e saldo não
      // existem sem conta —, então seguem desviando para o login.
      { path: 'profile', component: ProfilePage, canActivate: [authGuard] },
      { path: 'credits', component: CreditsPage, canActivate: [authGuard] },
    ],
  },

  { path: '**', redirectTo: '/home' },
];
