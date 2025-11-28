import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

import { UserService, UserCredits, CreditTransaction, PagedResult, CreditPackage, CreditOperationResult, PurchaseRequest } from './user-service';
import { environment } from '../../environments/environment';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

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
      }
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false
  };

  const mockPackages: CreditPackage[] = [
    {
      id: 1,
      name: 'Starter',
      description: 'Pacote inicial',
      credits: 50,
      bonusCredits: 0,
      totalCredits: 50,
      price: 9.90,
      priceFormatted: 'R$ 9,90',
      pricePerCredit: 0.198,
      isPopular: false,
      savingsPercent: 0
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        UserService
      ]
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getCurrentUser', () => {
    it('should fetch current user data', fakeAsync(() => {
      let result: UserCredits | undefined;
      service.getCurrentUser().subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUserCredits);
      tick();

      expect(result).toEqual(mockUserCredits);
    }));

    it('should update credits$ BehaviorSubject', fakeAsync(() => {
      let creditsValue: UserCredits | null = null;
      service.credits$.subscribe(c => creditsValue = c);

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      req.flush(mockUserCredits);
      tick();

      expect(creditsValue).not.toBeNull();
      expect(creditsValue!.userId).toBe(mockUserCredits.userId);
      expect(creditsValue!.availableCredits).toBe(mockUserCredits.availableCredits);
    }));
  });

  describe('getCredits', () => {
    it('should fetch user credits', fakeAsync(() => {
      let result: UserCredits | undefined;
      service.getCredits().subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/User/credits`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUserCredits);
      tick();

      expect(result).toEqual(mockUserCredits);
    }));

    it('should update credits$ BehaviorSubject', fakeAsync(() => {
      let creditsValue: UserCredits | null = null;
      service.credits$.subscribe(c => creditsValue = c);

      service.getCredits().subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/User/credits`);
      req.flush(mockUserCredits);
      tick();

      expect(creditsValue).not.toBeNull();
      expect(creditsValue!.userId).toBe(mockUserCredits.userId);
      expect(creditsValue!.availableCredits).toBe(mockUserCredits.availableCredits);
    }));
  });

  describe('refreshCredits', () => {
    it('should call getCredits internally', fakeAsync(() => {
      service.refreshCredits();

      const req = httpMock.expectOne(`${environment.apiUrl}/User/credits`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUserCredits);
      tick();
    }));
  });

  describe('getCreditHistory', () => {
    it('should fetch credit history with default pagination', fakeAsync(() => {
      let result: PagedResult<CreditTransaction> | undefined;
      service.getCreditHistory().subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/User/credits/history?page=1&pageSize=20`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCreditHistory);
      tick();

      expect(result).toEqual(mockCreditHistory);
    }));

    it('should fetch credit history with custom pagination', fakeAsync(() => {
      let result: PagedResult<CreditTransaction> | undefined;
      service.getCreditHistory(2, 10).subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/User/credits/history?page=2&pageSize=10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCreditHistory);
      tick();

      expect(result).toEqual(mockCreditHistory);
    }));
  });

  describe('getCreditPackages', () => {
    it('should fetch available credit packages', fakeAsync(() => {
      let result: CreditPackage[] | undefined;
      service.getCreditPackages().subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/Credits/packages`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPackages);
      tick();

      expect(result).toEqual(mockPackages);
    }));
  });

  describe('getPackageById', () => {
    it('should fetch a specific package by ID', fakeAsync(() => {
      let result: CreditPackage | undefined;
      service.getPackageById(1).subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/Credits/packages/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPackages[0]);
      tick();

      expect(result).toEqual(mockPackages[0]);
    }));
  });

  describe('purchaseCredits', () => {
    const mockPurchaseResult: CreditOperationResult = {
      success: true,
      message: 'Compra realizada com sucesso',
      amount: 50,
      newBalance: 150,
      transactionId: 123
    };

    const purchaseRequest: PurchaseRequest = {
      packageId: 1,
      paymentId: 'PAY-12345'
    };

    it('should send purchase request', fakeAsync(() => {
      let result: CreditOperationResult | undefined;
      service.purchaseCredits(purchaseRequest).subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/Credits/purchase`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(purchaseRequest);
      req.flush(mockPurchaseResult);
      tick();

      expect(result).toEqual(mockPurchaseResult);
    }));

    it('should update credits$ on successful purchase', fakeAsync(() => {
      // First set initial credits
      service.getCurrentUser().subscribe();
      const userReq = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      userReq.flush(mockUserCredits);
      tick();

      let creditsValue: UserCredits | null = null;
      service.credits$.subscribe(c => creditsValue = c);

      service.purchaseCredits(purchaseRequest).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/Credits/purchase`);
      req.flush(mockPurchaseResult);
      tick();

      expect(creditsValue!.availableCredits).toBe(mockPurchaseResult.newBalance);
    }));

    it('should not update credits$ on failed purchase', fakeAsync(() => {
      // First set initial credits
      service.getCurrentUser().subscribe();
      const userReq = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      userReq.flush(mockUserCredits);
      tick();

      const failedResult: CreditOperationResult = {
        success: false,
        message: 'Erro',
        amount: 0,
        newBalance: 100
      };

      let creditsValue: UserCredits | null = null;
      service.credits$.subscribe(c => creditsValue = c);

      service.purchaseCredits(purchaseRequest).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/Credits/purchase`);
      req.flush(failedResult);
      tick();

      expect(creditsValue!.availableCredits).toBe(mockUserCredits.availableCredits);
    }));
  });

  describe('estimateSearchCost', () => {
    it('should estimate search cost for given provider count', fakeAsync(() => {
      const mockEstimate = {
        providerCount: 10,
        estimatedCost: 5,
        description: 'Custo estimado para 10 providers'
      };

      let result: any;
      service.estimateSearchCost(10).subscribe(data => result = data);

      const req = httpMock.expectOne(`${environment.apiUrl}/Credits/estimate?providerCount=10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockEstimate);
      tick();

      expect(result).toEqual(mockEstimate);
    }));
  });

  describe('updateCreditsAfterConsumption', () => {
    it('should update credits after consumption', fakeAsync(() => {
      // First set initial credits
      service.getCurrentUser().subscribe();
      const req = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      req.flush(mockUserCredits);
      tick();

      let creditsValue: UserCredits | null = null;
      service.credits$.subscribe(c => creditsValue = c);

      service.updateCreditsAfterConsumption(10);

      expect(creditsValue!.availableCredits).toBe(mockUserCredits.availableCredits - 10);
      expect(creditsValue!.totalCreditsUsed).toBe(mockUserCredits.totalCreditsUsed + 10);
    }));

    it('should not fail if credits are null', () => {
      expect(() => service.updateCreditsAfterConsumption(10)).not.toThrow();
    });
  });

  describe('clearUserData', () => {
    it('should clear credits$ BehaviorSubject', fakeAsync(() => {
      // First set initial credits
      service.getCurrentUser().subscribe();
      const req = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      req.flush(mockUserCredits);
      tick();

      let creditsValue: UserCredits | null = mockUserCredits;
      service.credits$.subscribe(c => creditsValue = c);

      service.clearUserData();

      expect(creditsValue).toBeNull();
    }));
  });

  describe('credits$ Observable', () => {
    it('should initially emit null', fakeAsync(() => {
      let value: UserCredits | null = undefined as any;
      service.credits$.subscribe(c => value = c);
      tick();

      expect(value).toBeNull();
    }));

    it('should emit updated values when credits change', fakeAsync(() => {
      const values: (UserCredits | null)[] = [];
      service.credits$.subscribe(c => values.push(c));

      service.getCurrentUser().subscribe();
      const req = httpMock.expectOne(`${environment.apiUrl}/User/me`);
      req.flush(mockUserCredits);
      tick();

      expect(values.length).toBe(2);
      expect(values[0]).toBeNull();
      expect(values[1]).toEqual(mockUserCredits);
    }));
  });
});
