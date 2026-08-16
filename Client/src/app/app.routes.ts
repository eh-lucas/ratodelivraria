import { Routes } from '@angular/router';
import { LoginPage } from './pages/login-page/login-page';
import { RegisterPage } from './pages/register-page/register-page';
import { HomePage } from './pages/home-page/home-page';
import { SearchPage } from './pages/search-page/search-page';
import { WatchedPage } from './pages/watched-page/watched-page';
import { HistoryPage } from './pages/history-page/history-page';
import { ProfilePage } from './pages/profile-page/profile-page';
import { CreditsPage } from './pages/credits-page/credits-page';
import { AppLayoutComponent } from './layout/app-layout';
import { authGuard } from './guards/auth-guard';
import { environment } from '../environments/environment';

// Em modo demo a raiz cai direto no app (sem passar pela tela de login).
const landingRoute = environment.demoMode ? '/home' : '/login';

export const routes: Routes = [
  { path: '', redirectTo: landingRoute, pathMatch: 'full' },
  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },

  // Rotas autenticadas compartilham o shell (sidebar + header)
  {
    path: '',
    component: AppLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'home', component: HomePage },
      { path: 'search', component: SearchPage },
      { path: 'watched', component: WatchedPage },
      { path: 'history', component: HistoryPage },
      { path: 'profile', component: ProfilePage },
      { path: 'credits', component: CreditsPage },
    ],
  },

  { path: '**', redirectTo: landingRoute },
];
