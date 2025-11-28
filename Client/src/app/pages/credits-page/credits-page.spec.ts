import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError, Observable } from 'rxjs';

import { CreditsPage } from './credits-page';
import { UserService, CreditPackage, UserCredits, CreditOperationResult } from '../../services/user-service';

describe('CreditsPage', () => {
  let component: CreditsPage;
  let fixture: ComponentFixture<CreditsPage>;
  let mockUserService: jasmine.SpyObj<UserService>;
  let mockRouter: Router;

  const mockUserCredits: UserCredits = {
    userId: 1,
    username: 'testuser',
    email: 'test@example.com',
    availableCredits: 100,
    totalCreditsUsed: 50,
    estimatedCostPerSearch: 5,
    estimatedSearchesRemaining: 20
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
    },
    {
      id: 2,
      name: 'Popular',
      description: 'Mais vendido',
      credits: 200,
      bonusCredits: 50,
      totalCredits: 250,
      price: 29.90,
      priceFormatted: 'R$ 29,90',
      pricePerCredit: 0.1196,
      isPopular: true,
      savingsPercent: 40
    },
    {
      id: 3,
      name: 'Premium',
      description: 'Melhor custo-beneficio',
      credits: 500,
      bonusCredits: 200,
      totalCredits: 700,
      price: 59.90,
      priceFormatted: 'R$ 59,90',
      pricePerCredit: 0.0855,
      isPopular: false,
      savingsPercent: 57
    }
  ];

  const mockPurchaseResult: CreditOperationResult = {
    success: true,
    message: 'Compra realizada com sucesso',
    amount: 250,
    newBalance: 350,
    transactionId: 123
  };

  beforeEach(async () => {
    mockUserService = jasmine.createSpyObj('UserService', [
      'getCreditPackages',
      'getCurrentUser',
      'purchaseCredits'
    ]);
    mockUserService.getCreditPackages.and.returnValue(of(mockPackages));
    mockUserService.getCurrentUser.and.returnValue(of(mockUserCredits));
    mockUserService.purchaseCredits.and.returnValue(of(mockPurchaseResult));

    await TestBed.configureTestingModule({
      imports: [CreditsPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideLocationMocks(),
        { provide: UserService, useValue: mockUserService }
      ]
    }).compileComponents();

    mockRouter = TestBed.inject(Router);
    spyOn(mockRouter, 'navigate');

    fixture = TestBed.createComponent(CreditsPage);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load packages on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(mockUserService.getCreditPackages).toHaveBeenCalled();
      expect(component.packages).toEqual(mockPackages);
    }));

    it('should load user credits on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(mockUserService.getCurrentUser).toHaveBeenCalled();
      expect(component.userCredits).toEqual(mockUserCredits);
    }));

    it('should set loading to false after packages load', fakeAsync(() => {
      expect(component.loading).toBeTrue();
      fixture.detectChanges();
      tick();

      expect(component.loading).toBeFalse();
    }));
  });

  describe('Error handling', () => {
    it('should display error when packages fail to load', fakeAsync(() => {
      mockUserService.getCreditPackages.and.returnValue(throwError(() => new Error('Network error')));
      fixture.detectChanges();
      tick();

      expect(component.error).toBeTruthy();
      expect(component.loading).toBeFalse();
    }));

    it('should handle user credits load failure gracefully', fakeAsync(() => {
      mockUserService.getCurrentUser.and.returnValue(throwError(() => new Error('Network error')));
      fixture.detectChanges();
      tick();

      expect(component.userCredits).toBeNull();
    }));
  });

  describe('Package selection', () => {
    it('should select a package', () => {
      component.selectPackage(2);

      expect(component.selectedPackageId).toBe(2);
    });

    it('should clear error and success messages on selection', () => {
      component.error = 'Some error';
      component.successMessage = 'Some success';

      component.selectPackage(1);

      expect(component.error).toBeNull();
      expect(component.successMessage).toBeNull();
    });
  });

  describe('Purchase', () => {
    beforeEach(fakeAsync(() => {
      fixture.detectChanges();
      tick();
    }));

    it('should purchase package successfully', fakeAsync(() => {
      const pkg = mockPackages[1];
      component.purchasePackage(pkg);
      tick();

      expect(mockUserService.purchaseCredits).toHaveBeenCalled();
      expect(component.successMessage).toContain('sucesso');
      expect(component.purchasing).toBeFalse();
    }));

    it('should update user credits after successful purchase', fakeAsync(() => {
      const pkg = mockPackages[1];
      component.purchasePackage(pkg);
      tick();

      expect(component.userCredits!.availableCredits).toBe(mockPurchaseResult.newBalance);
    }));

    it('should not allow purchase while another is in progress', fakeAsync(() => {
      component.purchasing = true;
      const pkg = mockPackages[1];
      component.purchasePackage(pkg);

      expect(mockUserService.purchaseCredits).not.toHaveBeenCalled();
    }));

    it('should handle purchase error', fakeAsync(() => {
      mockUserService.purchaseCredits.and.returnValue(throwError(() => new Error('Payment failed')));

      const pkg = mockPackages[1];
      component.purchasePackage(pkg);
      tick();

      expect(component.error).toBeTruthy();
      expect(component.purchasing).toBeFalse();
    }));

    it('should handle failed purchase result', fakeAsync(() => {
      const failedResult: CreditOperationResult = {
        success: false,
        message: 'Saldo insuficiente',
        amount: 0,
        newBalance: 100
      };
      mockUserService.purchaseCredits.and.returnValue(of(failedResult));

      const pkg = mockPackages[1];
      component.purchasePackage(pkg);
      tick();

      expect(component.error).toBe('Saldo insuficiente');
      expect(component.purchasing).toBeFalse();
    }));

    it('should set selectedPackageId during purchase', fakeAsync(() => {
      const pkg = mockPackages[1];
      // Usa delay para poder verificar o estado intermediario
      const delayedObservable = new Observable<typeof mockPurchaseResult>(subscriber => {
        // Simula resposta assincrona
        setTimeout(() => {
          subscriber.next(mockPurchaseResult);
          subscriber.complete();
        }, 100);
      });
      mockUserService.purchaseCredits.and.returnValue(delayedObservable);

      component.purchasePackage(pkg);
      // Verifica o estado DURANTE a compra (antes do tick)
      expect(component.selectedPackageId).toBe(pkg.id);
      expect(component.purchasing).toBeTrue();

      tick(100); // Completa a requisicao
    }));

    it('should clear selectedPackageId after purchase completes', fakeAsync(() => {
      const pkg = mockPackages[1];
      component.purchasePackage(pkg);
      tick();

      expect(component.selectedPackageId).toBeNull();
    }));
  });

  describe('Package icons', () => {
    it('should return rocket icon for Starter package', () => {
      expect(component.getPackageIcon('Starter')).toBe('rocket');
      expect(component.getPackageIcon('Inicial')).toBe('rocket');
    });

    it('should return star icon for Basic package', () => {
      expect(component.getPackageIcon('Basic')).toBe('star');
      expect(component.getPackageIcon('Basico')).toBe('star');
    });

    it('should return fire icon for Popular package', () => {
      expect(component.getPackageIcon('Popular')).toBe('fire');
    });

    it('should return crown icon for Premium package', () => {
      expect(component.getPackageIcon('Premium')).toBe('crown');
      expect(component.getPackageIcon('Pro')).toBe('crown');
    });

    it('should return default package icon for unknown names', () => {
      expect(component.getPackageIcon('Unknown')).toBe('package');
    });
  });

  describe('Package colors', () => {
    it('should return blue for Starter package', () => {
      expect(component.getPackageColor('Starter')).toBe('blue');
      expect(component.getPackageColor('Inicial')).toBe('blue');
    });

    it('should return green for Basic package', () => {
      expect(component.getPackageColor('Basic')).toBe('green');
      expect(component.getPackageColor('Basico')).toBe('green');
    });

    it('should return purple for Popular package', () => {
      expect(component.getPackageColor('Popular')).toBe('purple');
    });

    it('should return yellow for Premium package', () => {
      expect(component.getPackageColor('Premium')).toBe('yellow');
      expect(component.getPackageColor('Pro')).toBe('yellow');
    });

    it('should return gray for unknown package names', () => {
      expect(component.getPackageColor('Unknown')).toBe('gray');
    });
  });

  describe('Data reload', () => {
    it('should reload data when loadData is called', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      mockUserService.getCreditPackages.calls.reset();
      mockUserService.getCurrentUser.calls.reset();

      component.loadData();
      tick();

      expect(mockUserService.getCreditPackages).toHaveBeenCalled();
      expect(mockUserService.getCurrentUser).toHaveBeenCalled();
    }));

    it('should clear error when loading data', fakeAsync(() => {
      component.error = 'Previous error';
      component.loadData();
      tick();

      expect(component.error).toBeNull();
    }));
  });
});
