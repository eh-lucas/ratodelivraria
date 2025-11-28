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
        username: 'testuser'
      });
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
      expect(localStorage.getItem('access_token')).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('should return true when token exists', () => {
      localStorage.setItem('access_token', 'test-token');
      expect(service.isAuthenticated()).toBeTrue();
    });

    it('should return false when token does not exist', () => {
      expect(service.isAuthenticated()).toBeFalse();
    });
  });
});
