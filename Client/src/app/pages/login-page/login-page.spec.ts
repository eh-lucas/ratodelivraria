import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';

import { LoginPage } from './login-page';
import { AuthService } from '../../services/auth-service';

describe('Login', () => {
  let component: LoginPage;
  let fixture: ComponentFixture<LoginPage>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: Router;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['login', 'setToken']);

    await TestBed.configureTestingModule({
      imports: [LoginPage, ReactiveFormsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideLocationMocks(),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    mockRouter = TestBed.inject(Router);
    // A tela usa navigateByUrl, não navigate: o destino pode vir de um
    // ?redirectTo=, então é uma URL e não um array de segmentos.
    spyOn(mockRouter, 'navigateByUrl');

    fixture = TestBed.createComponent(LoginPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize login form with email and password fields', () => {
    expect(component.loginForm).toBeDefined();
    expect(component.loginForm.controls['email']).toBeDefined();
    expect(component.loginForm.controls['password']).toBeDefined();
  });

  it('should have invalid form when fields are empty', () => {
    expect(component.loginForm.valid).toBeFalse();
  });

  it('should have valid form when email and password are provided', () => {
    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'password123'
    });
    expect(component.loginForm.valid).toBeTrue();
  });

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    expect(mockAuthService.login).not.toHaveBeenCalled();
  });

  it('should call authService.login on valid submit', () => {
    mockAuthService.login.and.returnValue(of({ token: 'test-token' }));

    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'password123'
    });
    component.onSubmit();

    expect(mockAuthService.login).toHaveBeenCalledWith('test@example.com', 'password123');
  });

  it('should navigate to home on successful login', () => {
    mockAuthService.login.and.returnValue(of({ token: 'test-token' }));

    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'password123'
    });
    component.onSubmit();

    expect(mockAuthService.setToken).toHaveBeenCalledWith('test-token');
    expect(mockRouter.navigateByUrl).toHaveBeenCalledWith('/home');
  });

  it('should show a message when login fails', () => {
    // A falha vira texto na tela, não console.error: quem errou a senha precisa
    // ler o motivo, e o console não é lugar de falar com ninguém. 401 é
    // credencial errada; qualquer outro status vira "tente de novo", porque
    // acusar a senha quando a API caiu manda procurar erro onde não tem.
    mockAuthService.login.and.returnValue(throwError(() => ({ status: 401 })));

    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'wrongpassword'
    });
    component.onSubmit();

    expect(component.errorMessage).toBe('E-mail ou senha incorretos.');
    expect(component.loading).toBeFalse();
  });

  it('should suggest Google for an account that has no password', () => {
    // Conta nascida do Google nunca teve senha. Dizer "senha incorreta" mandaria
    // a pessoa tentar lembrar uma que não existe.
    mockAuthService.login.and.returnValue(throwError(() => ({
      error: { useGoogle: true, message: 'Esta conta entra pelo Google.' }
    })));

    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'qualquercoisa'
    });
    component.onSubmit();

    expect(component.suggestGoogle).toBeTrue();
  });
});
