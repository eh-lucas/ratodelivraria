import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { Router } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { environment } from '../../environments/environment';

const TOKEN_KEY = 'access_token';

/**
 * Marca local de que existe sessão longa no servidor.
 *
 * O refresh token mora num cookie `HttpOnly`, que o JavaScript não lê — e é
 * justamente esse o ponto dele. Então o front guarda só esta bandeira para saber
 * se vale a pena tentar renovar: sem ela, toda resposta 401 de visitante
 * dispararia um refresh inútil.
 *
 * Ela não é credencial de nada. Forjar esta chave no navegador rende, no máximo,
 * uma chamada a /auth/refresh que volta 401.
 */
const SESSION_FLAG = 'sherlock.session';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  private token: string | null = null;
  private jwt = new JwtHelperService();

  /** Verdadeiro enquanto uma renovação está em voo. Lido pelo interceptor. */
  renovando = false;

  private tokenRenovado = new BehaviorSubject<string | null>(null);
  readonly tokenRenovado$ = this.tokenRenovado.asObservable();

  constructor(private http: HttpClient, private router: Router) { }

  login(email: string, password: string): Observable<any> {
    // `withCredentials` é o que faz o cookie de sessão viajar. Em produção front
    // e API dividem a origem e isto seria dispensável; em desenvolvimento são
    // portas diferentes, e sem a flag o cookie simplesmente não é gravado.
    return this.http.post(`${this.apiUrl}/auth/login`, { email, password }, { withCredentials: true })
      .pipe(tap(() => this.marcarSessao()));
  }

  register(email: string, password: string, username?: string, firstName?: string): Observable<any> {
    return this.http
      .post(`${this.apiUrl}/auth/register`, { email, password, username, firstName }, { withCredentials: true })
      .pipe(tap(() => this.marcarSessao()));
  }

  /** Troca o ID token do Google pelo nosso JWT. Quem valida o token é o backend. */
  loginWithGoogle(credential: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/google`, { credential }, { withCredentials: true })
      .pipe(tap(() => this.marcarSessao()));
  }

  /**
   * Troca o cookie de sessão por um token de acesso novo. O cookie roda a cada
   * uso, então esta chamada precisa acontecer uma de cada vez — quem garante isso
   * é a bandeira `renovando`, lida pelo interceptor.
   */
  refresh(): Observable<string> {
    this.renovando = true;
    this.tokenRenovado.next(null);

    return this.http
      .post<{ token: string }>(`${this.apiUrl}/auth/refresh`, {}, { withCredentials: true })
      .pipe(
        map(resposta => resposta.token),
        tap({
          next: novo => {
            this.setToken(novo);
            this.renovando = false;
            this.tokenRenovado.next(novo);
          },
          error: () => {
            this.renovando = false;
            this.encerrarSessaoLocal();
          },
        }),
      );
  }

  /**
   * Sai de verdade: pede ao servidor para revogar a sessão longa antes de limpar
   * o lado de cá. Sem esta chamada o cookie continuaria valendo, e qualquer
   * visita seguinte ao site reentraria sozinha — "sair" que não sai.
   */
  logout(): void {
    this.http.post(`${this.apiUrl}/auth/logout`, {}, { withCredentials: true }).subscribe({
      // Falhou a chamada? A sessão local vai embora de qualquer jeito: quem
      // clicou em sair não pode continuar logado porque a rede caiu.
      next: () => this.encerrarSessaoLocal(),
      error: () => this.encerrarSessaoLocal(),
    });

    this.encerrarSessaoLocal();
    this.router.navigate(['/login']);
  }

  /** Limpa só o lado do navegador. Usado quando o servidor já recusou a sessão. */
  encerrarSessaoLocal(): void {
    this.token = null;
    this.renovando = false;
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(SESSION_FLAG);
  }

  temSessaoLonga(): boolean {
    return localStorage.getItem(SESSION_FLAG) === '1';
  }

  setToken(token: string): void {
    this.token = token;
    localStorage.setItem(TOKEN_KEY, token);
    this.marcarSessao();
  }

  getToken(): string | null {
    return this.token || localStorage.getItem(TOKEN_KEY);
  }

  private marcarSessao(): void {
    localStorage.setItem(SESSION_FLAG, '1');
  }

  /**
   * Agora confere a validade, não só a presença do token.
   *
   * Passou a importar quando a interface começou a se ramificar por estar ou não
   * logado: com token vencido a tela se montava inteira como logada e aí cada
   * chamada voltava 401 — pior que mostrar o convite para entrar. Token vencido
   * é descartado aqui mesmo.
   *
   * O token de acesso dura 30 minutos, mas a sessão longa dura 30 dias: por isso
   * um token vencido com cookie de sessão ainda conta como autenticado — o
   * interceptor renova na primeira chamada. Sem esta linha, quem voltasse ao site
   * depois do almoço veria a tela de visitante apesar de ter sessão.
   */
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return this.temSessaoLonga();

    try {
      if (this.jwt.isTokenExpired(token)) {
        this.token = null;
        localStorage.removeItem(TOKEN_KEY);
        return this.temSessaoLonga();
      }
      return true;
    } catch {
      // Token ilegível é token inútil.
      this.token = null;
      localStorage.removeItem(TOKEN_KEY);
      return this.temSessaoLonga();
    }
  }
}
