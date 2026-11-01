import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthService } from './auth-service';
import { environment } from '../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        AuthService
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should send POST request to login endpoint', () => {
      const mockResponse = { token: 'test-token' };

      service.login('test@example.com', 'password123').subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'test@example.com', password: 'password123' });
      req.flush(mockResponse);
    });
  });

  describe('register', () => {
    it('should send POST request to register endpoint', () => {
      const mockResponse = { token: 'test-token' };

      service.register('test@example.com', 'password123', 'testuser').subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        email: 'test@example.com',
        password: 'password123',
        username: 'testuser',
        firstName: undefined,
      });
      // O cookie de sessão longa só é gravado se a requisição levar credenciais.
      expect(req.request.withCredentials).toBeTrue();
      req.flush(mockResponse);
    });
  });

  describe('token management', () => {
    it('should store token in localStorage', () => {
      service.setToken('test-token');
      expect(localStorage.getItem('access_token')).toBe('test-token');
    });

    it('should retrieve token from localStorage', () => {
      localStorage.setItem('access_token', 'stored-token');
      expect(service.getToken()).toBe('stored-token');
    });

    it('should remove token on logout', () => {
      localStorage.setItem('access_token', 'test-token');
      service.logout();

      // Sair agora avisa o servidor: sem revogar o refresh token, o cookie
      // continuaria valendo e a próxima visita reentraria sozinha.
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/logout`);
      expect(req.request.method).toBe('POST');
      req.flush(null);

      expect(localStorage.getItem('access_token')).toBeNull();
      expect(service.temSessaoLonga()).toBeFalse();
    });
  });

  describe('isAuthenticated', () => {
    /** JWT com exp em 2099 — o conteúdo não importa, só a validade. */
    function tokenValido(): string {
      const payload = btoa(JSON.stringify({ sub: '1', exp: 4102444800 }));
      return `eyJhbGciOiJIUzI1NiJ9.${payload}.assinatura`;
    }

    it('should return true when token is valid', () => {
      service.setToken(tokenValido());
      expect(service.isAuthenticated()).toBeTrue();
    });

    it('should return false when token does not exist', () => {
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('should discard an unreadable token', () => {
      // "test-token" não é um JWT. Antes a tela se montava como logada e cada
      // chamada voltava 401.
      localStorage.setItem('access_token', 'test-token');
      expect(service.isAuthenticated()).toBeFalse();
      expect(localStorage.getItem('access_token')).toBeNull();
    });

    it('should stay authenticated with an expired token but a live session', () => {
      // O token de acesso dura 30 min; a sessão longa, 30 dias. Quem volta ao
      // site depois do almoço tem o token vencido e sessão viva — o interceptor
      // renova na primeira chamada, então a tela não pode mostrar o visitante.
      const payload = btoa(JSON.stringify({ sub: '1', exp: 1000 }));
      localStorage.setItem('access_token', `eyJhbGciOiJIUzI1NiJ9.${payload}.assinatura`);
      localStorage.setItem('sherlock.session', '1');

      expect(service.isAuthenticated()).toBeTrue();
    });
  });
});
