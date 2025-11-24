import { Routes } from '@angular/router';
import { LoginPage } from './pages/login-page/login-page';
import { RegisterPage } from './pages/register-page/register-page';
import { HomeComponent } from './components/home-component/home-component';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/register', pathMatch: 'full' },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [authGuard], 
  },
  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },
  { path: '**', redirectTo: '/register' }
];