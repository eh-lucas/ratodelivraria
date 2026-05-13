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
    spyOn(mockRouter, 'navigate');

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

  it('should navigate to search page on successful login', () => {
    mockAuthService.login.and.returnValue(of({ token: 'test-token' }));

    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'password123'
    });
    component.onSubmit();

    expect(mockAuthService.setToken).toHaveBeenCalledWith('test-token');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/home']);
  });

  it('should handle login error', () => {
    const consoleSpy = spyOn(console, 'error');
    mockAuthService.login.and.returnValue(throwError(() => new Error('Login failed')));

    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'wrongpassword'
    });
    component.onSubmit();

    expect(consoleSpy).toHaveBeenCalled();
  });
});
