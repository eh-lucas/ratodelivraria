import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';

import { RegisterPage } from './register-page';
import { AuthService } from '../../services/auth-service';

describe('RegisterPage', () => {
  let component: RegisterPage;
  let fixture: ComponentFixture<RegisterPage>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: Router;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['register', 'setToken']);

    await TestBed.configureTestingModule({
      imports: [RegisterPage, ReactiveFormsModule],
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

    fixture = TestBed.createComponent(RegisterPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize register form with all required fields', () => {
    expect(component.registerForm).toBeDefined();
    expect(component.registerForm.controls['username']).toBeDefined();
    expect(component.registerForm.controls['email']).toBeDefined();
    expect(component.registerForm.controls['password']).toBeDefined();
    expect(component.registerForm.controls['confirmPassword']).toBeDefined();
  });

  it('should have invalid form when fields are empty', () => {
    expect(component.registerForm.valid).toBeFalse();
  });

  it('should validate minimum password length (8 characters)', () => {
    component.registerForm.controls['password'].setValue('1234567'); // 7 characters
    component.registerForm.controls['password'].markAsTouched();
    component.registerForm.controls['password'].updateValueAndValidity();

    expect(component.registerForm.controls['password'].hasError('minlength')).toBeTrue();
  });

  it('should validate password match', () => {
    component.registerForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      confirmPassword: 'differentpassword'
    });
    component.registerForm.updateValueAndValidity();
    expect(component.registerForm.hasError('mismatch')).toBeTrue();
  });

  it('should have valid form when all fields are correct', () => {
    component.registerForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      confirmPassword: 'password123'
    });
    expect(component.registerForm.valid).toBeTrue();
  });

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    expect(mockAuthService.register).not.toHaveBeenCalled();
  });

  it('should call authService.register on valid submit', () => {
    mockAuthService.register.and.returnValue(of({ token: 'test-token' }));

    component.registerForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      confirmPassword: 'password123'
    });
    component.onSubmit();

    expect(mockAuthService.register).toHaveBeenCalled();
  });

  it('should navigate to search page on successful registration', () => {
    mockAuthService.register.and.returnValue(of({ token: 'test-token' }));

    component.registerForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      confirmPassword: 'password123'
    });
    component.onSubmit();

    expect(mockAuthService.setToken).toHaveBeenCalledWith('test-token');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/search']);
  });

  it('should display error message on registration failure', () => {
    mockAuthService.register.and.returnValue(throwError(() => ({
      error: { message: 'Email already exists' }
    })));

    component.registerForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      confirmPassword: 'password123'
    });
    component.onSubmit();

    expect(component.errorMessage).toBe('Email already exists');
    expect(component.loading).toBeFalse();
  });
});
