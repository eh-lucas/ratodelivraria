import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';

import { ProfilePage } from './profile-page';
import { UserService, UserCredits, CreditTransaction, PagedResult } from '../../services/user-service';

describe('ProfilePage', () => {
  let component: ProfilePage;
  let fixture: ComponentFixture<ProfilePage>;
  let mockUserService: jasmine.SpyObj<UserService>;

  const mockUserCredits: UserCredits = {
    userId: 1,
    username: 'testuser',
    email: 'test@example.com',
    availableCredits: 100,
    totalCreditsUsed: 50,
    estimatedCostPerSearch: 5,
    estimatedSearchesRemaining: 20
  };

  const mockCreditHistory: PagedResult<CreditTransaction> = {
    items: [
      {
        id: 1,
        type: 'Purchase',
        typeDescription: 'Compra',
        amount: 100,
        balanceAfter: 150,
        description: 'Compra de pacote',
        packageName: 'Pacote Basico',
        createdAt: '2024-01-15T10:30:00Z'
      },
      {
        id: 2,
        type: 'Consumption',
        typeDescription: 'Consumo',
        amount: -5,
        balanceAfter: 145,
        description: 'Busca de livro',
        createdAt: '2024-01-15T11:00:00Z'
      }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 10,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false
  };

  beforeEach(async () => {
    mockUserService = jasmine.createSpyObj('UserService', [
      'getCurrentUser',
      'getCreditHistory'
    ]);
    mockUserService.getCurrentUser.and.returnValue(of(mockUserCredits));
    mockUserService.getCreditHistory.and.returnValue(of(mockCreditHistory));

    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideLocationMocks(),
        { provide: UserService, useValue: mockUserService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProfilePage);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load user data on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(mockUserService.getCurrentUser).toHaveBeenCalled();
      expect(component.userCredits).toEqual(mockUserCredits);
    }));

    it('should load credit history on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(mockUserService.getCreditHistory).toHaveBeenCalledWith(1, 10);
      expect(component.creditHistory).toEqual(mockCreditHistory.items);
    }));

    it('should set loading to false after data loads', fakeAsync(() => {
      expect(component.loading).toBeTrue();
      fixture.detectChanges();
      tick();

      expect(component.loading).toBeFalse();
    }));
  });

  describe('Error handling', () => {
    it('should display error message when user data fails to load', fakeAsync(() => {
      mockUserService.getCurrentUser.and.returnValue(throwError(() => new Error('Network error')));
      fixture.detectChanges();
      tick();

      expect(component.error).toBeTruthy();
      expect(component.loading).toBeFalse();
    }));

    it('should handle credit history load failure gracefully', fakeAsync(() => {
      mockUserService.getCreditHistory.and.returnValue(throwError(() => new Error('Network error')));
      fixture.detectChanges();
      tick();

      expect(component.historyLoading).toBeFalse();
    }));
  });

  describe('Pagination', () => {
    it('should update pagination info from response', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(component.totalPages).toBe(1);
      expect(component.hasNextPage).toBeFalse();
      expect(component.hasPreviousPage).toBeFalse();
    }));

    it('should navigate to next page when hasNextPage is true', fakeAsync(() => {
      const pagedResult: PagedResult<CreditTransaction> = {
        ...mockCreditHistory,
        hasNextPage: true,
        totalPages: 3
      };
      mockUserService.getCreditHistory.and.returnValue(of(pagedResult));
      fixture.detectChanges();
      tick();

      component.nextPage();
      tick();

      expect(component.currentPage).toBe(2);
      expect(mockUserService.getCreditHistory).toHaveBeenCalledWith(2, 10);
    }));

    it('should not navigate to next page when hasNextPage is false', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      component.nextPage();
      tick();

      expect(component.currentPage).toBe(1);
    }));

    it('should navigate to previous page when hasPreviousPage is true', fakeAsync(() => {
      const pagedResult: PagedResult<CreditTransaction> = {
        ...mockCreditHistory,
        hasPreviousPage: true,
        page: 2
      };
      mockUserService.getCreditHistory.and.returnValue(of(pagedResult));
      fixture.detectChanges();
      component.currentPage = 2;
      tick();

      component.previousPage();
      tick();

      expect(component.currentPage).toBe(1);
    }));

    it('should not navigate to previous page when hasPreviousPage is false', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      component.previousPage();
      tick();

      expect(component.currentPage).toBe(1);
    }));
  });

  describe('Transaction type styling', () => {
    it('should return green class for Purchase type', () => {
      expect(component.getTransactionTypeClass('Purchase')).toBe('text-green-600');
    });

    it('should return green class for Bonus type', () => {
      expect(component.getTransactionTypeClass('Bonus')).toBe('text-green-600');
    });

    it('should return red class for Consumption type', () => {
      expect(component.getTransactionTypeClass('Consumption')).toBe('text-red-600');
    });

    it('should return blue class for Refund type', () => {
      expect(component.getTransactionTypeClass('Refund')).toBe('text-blue-600');
    });

    it('should return gray class for unknown type', () => {
      expect(component.getTransactionTypeClass('Unknown')).toBe('text-gray-600');
    });
  });

  describe('Date formatting', () => {
    it('should format date correctly in Brazilian format', () => {
      const dateString = '2024-01-15T10:30:00Z';
      const formatted = component.formatDate(dateString);

      expect(formatted).toContain('/');
      expect(formatted).toContain(':');
    });
  });

  describe('Load user data retry', () => {
    it('should allow retrying user data load', fakeAsync(() => {
      mockUserService.getCurrentUser.and.returnValue(throwError(() => new Error('Network error')));
      fixture.detectChanges();
      tick();

      expect(component.error).toBeTruthy();

      mockUserService.getCurrentUser.and.returnValue(of(mockUserCredits));
      component.loadUserData();
      tick();

      expect(component.error).toBeNull();
      expect(component.userCredits).toEqual(mockUserCredits);
    }));
  });
});
