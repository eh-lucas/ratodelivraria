import { Routes } from '@angular/router';
import { LoginComponent } from './components/login-component/login-component';
import { HomeComponent } from './components/home-component/home-component';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [authGuard], 
  },
  { path: 'login', component: LoginComponent },
  { path: '**', redirectTo: '/login' }
];