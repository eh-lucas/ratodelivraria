import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router, NavigationEnd, provideRouter } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { BehaviorSubject, of, Subject } from 'rxjs';

import { NavbarComponent } from './navbar';
import { UserService, UserCredits } from '../../services/user-service';
import { AuthService } from '../../services/auth-service';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  let mockUserService: jasmine.SpyObj<UserService>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let routerEventsSubject: Subject<any>;
  let creditsSubject: BehaviorSubject<UserCredits | null>;

  const mockUserCredits: UserCredits = {
    userId: 1,
    username: 'testuser',
    email: 'test@example.com',
    availableCredits: 100,
    totalCreditsUsed: 50,
    estimatedCostPerSearch: 5,
    estimatedSearchesRemaining: 20
  };

  beforeEach(async () => {
    creditsSubject = new BehaviorSubject<UserCredits | null>(null);
    routerEventsSubject = new Subject();

    mockUserService = jasmine.createSpyObj('UserService', ['getCurrentUser', 'clearUserData'], {
      credits$: creditsSubject.asObservable()
    });
    mockUserService.getCurrentUser.and.returnValue(of(mockUserCredits));

    mockAuthService = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'logout']);
    mockAuthService.isAuthenticated.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [NavbarComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideLocationMocks(),
        { provide: UserService, useValue: mockUserService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Authentication state', () => {
    it('should not display navbar when user is not authenticated', () => {
      mockAuthService.isAuthenticated.and.returnValue(false);
      fixture.detectChanges();

      expect(component.isAuthenticated).toBeFalse();
    });

    it('should display navbar when user is authenticated and not on public route', fakeAsync(() => {
      mockAuthService.isAuthenticated.and.returnValue(true);

      // Configura o componente manualmente para simular uma rota nao publica
      fixture.detectChanges(); // Triggers ngOnInit
      tick();

      // Define a rota e re-verifica autenticacao
      component.currentRoute = '/search';
      component['checkAuthentication']();
      tick();

      expect(component.isAuthenticated).toBeTrue();
    }));
  });

  describe('Credits display', () => {
    beforeEach(() => {
      mockAuthService.isAuthenticated.and.returnValue(true);
      component['currentRoute'] = '/search';
    });

    it('should load user credits when authenticated', fakeAsync(() => {
      component['checkAuthentication']();
      fixture.detectChanges();
      creditsSubject.next(mockUserCredits);
      tick();

      expect(component.userCredits).toEqual(mockUserCredits);
    }));

    it('should display null credits when not authenticated', () => {
      mockAuthService.isAuthenticated.and.returnValue(false);
      component['checkAuthentication']();
      fixture.detectChanges();

      expect(component.userCredits).toBeNull();
    });
  });

  describe('Navigation', () => {
    beforeEach(() => {
      mockAuthService.isAuthenticated.and.returnValue(true);
      component['currentRoute'] = '/search';
      component['checkAuthentication']();
      fixture.detectChanges();
    });

    it('should correctly identify active route', () => {
      component.currentRoute = '/search';

      expect(component.isActive('/search')).toBeTrue();
      expect(component.isActive('/profile')).toBeFalse();
    });

    it('should match nested routes as active', () => {
      component.currentRoute = '/search/results';

      expect(component.isActive('/search')).toBeTrue();
    });
  });

  describe('Menu toggle', () => {
    it('should toggle menu open state', () => {
      expect(component.isMenuOpen).toBeFalse();

      component.toggleMenu();
      expect(component.isMenuOpen).toBeTrue();

      component.toggleMenu();
      expect(component.isMenuOpen).toBeFalse();
    });
  });

  describe('Logout', () => {
    let router: Router;

    beforeEach(() => {
      mockAuthService.isAuthenticated.and.returnValue(true);
      component['currentRoute'] = '/search';
      component['checkAuthentication']();
      fixture.detectChanges();
      router = TestBed.inject(Router);
      spyOn(router, 'navigate');
    });

    it('should call authService.logout on logout', () => {
      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
    });

    it('should call userService.clearUserData on logout', () => {
      component.logout();

      expect(mockUserService.clearUserData).toHaveBeenCalled();
    });

    it('should navigate to login page on logout', () => {
      component.logout();

      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('Public routes', () => {
    it('should not show navbar on login page', () => {
      mockAuthService.isAuthenticated.and.returnValue(true);
      component['currentRoute'] = '/login';
      component['checkAuthentication']();

      expect(component.isAuthenticated).toBeFalse();
    });

    it('should not show navbar on register page', () => {
      mockAuthService.isAuthenticated.and.returnValue(true);
      component['currentRoute'] = '/register';
      component['checkAuthentication']();

      expect(component.isAuthenticated).toBeFalse();
    });

    it('should not show navbar on home page', () => {
      mockAuthService.isAuthenticated.and.returnValue(true);
      component['currentRoute'] = '/';
      component['checkAuthentication']();

      expect(component.isAuthenticated).toBeFalse();
    });
  });
});
