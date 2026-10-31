import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Router } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  private token: string | null = null;
  private jwt = new JwtHelperService();

  constructor(private http: HttpClient, private router: Router) { }

  login(email: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/login`, { email, password });
  }

  register(email: string, password: string, username?: string, firstName?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/register`, { email, password, username, firstName });
  }

  /** Troca o ID token do Google pelo nosso JWT. Quem valida o token é o backend. */
  loginWithGoogle(credential: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/google`, { credential });
  }

  logout(): void {
    this.token = null;
    localStorage.removeItem('access_token');
    this.router.navigate(['/login'])
  }

  setToken(token: string): void {
    this.token = token;
    localStorage.setItem('access_token', token);
  }

  getToken(): string | null {
    return this.token || localStorage.getItem('access_token');
  }

  /**
   * Agora confere a validade, não só a presença do token.
   *
   * Passou a importar quando a interface começou a se ramificar por estar ou não
   * logado: com token vencido a tela se montava inteira como logada e aí cada
   * chamada voltava 401 — pior que mostrar o convite para entrar. Token vencido
   * é descartado aqui mesmo.
   */
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      if (this.jwt.isTokenExpired(token)) {
        this.token = null;
        localStorage.removeItem('access_token');
        return false;
      }
      return true;
    } catch {
      // Token ilegível é token inútil.
      this.token = null;
      localStorage.removeItem('access_token');
      return false;
    }
  }
}
