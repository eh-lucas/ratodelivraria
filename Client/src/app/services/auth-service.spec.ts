import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth-service';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const spy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: spy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve chamar a API de login com usuário e senha', () => {
    const mockResponse = { token: 'abc123' };

    service.login('vitor', '1234').subscribe(response => {
      expect(response).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${service['apiUrl']}/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ username: 'vitor', password: '1234' });
    req.flush(mockResponse);
  });

  it('deve salvar o token na memória e no localStorage', () => {
    service.setToken('meu_token');
    expect(service['token']).toBe('meu_token');
    expect(localStorage.getItem('access_token')).toBe('meu_token');
  });

  it('deve retornar o token da memória se existir', () => {
    service['token'] = 'token_memoria';
    expect(service.getToken()).toBe('token_memoria');
  });

  it('deve retornar o token do localStorage se não estiver na memória', () => {
    localStorage.setItem('access_token', 'token_local');
    service['token'] = null;
    expect(service.getToken()).toBe('token_local');
  });

  it('deve limpar o token e redirecionar para /login', () => {
    localStorage.setItem('access_token', 'token_antigo');
    service['token'] = 'token_antigo';

    service.logout();

    expect(service['token']).toBeNull();
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('deve retornar true se o token existir', () => {
    service.setToken('token_ok');
    expect(service.isAuthenticated()).toBeTrue();
  });

  it('deve retornar false se o token não existir', () => {
    service.logout();
    expect(service.isAuthenticated()).toBeFalse();
  });
});
