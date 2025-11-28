import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserCredits {
  userId: number;
  username: string;
  email: string;
  availableCredits: number;
  totalCreditsUsed: number;
  estimatedCostPerSearch: number;
  estimatedSearchesRemaining: number;
}

export interface CreditTransaction {
  id: number;
  type: string;
  typeDescription: string;
  amount: number;
  balanceAfter: number;
  description: string;
  packageName?: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreditPackage {
  id: number;
  name: string;
  description: string;
  credits: number;
  bonusCredits: number;
  totalCredits: number;
  price: number;
  priceFormatted: string;
  pricePerCredit: number;
  isPopular: boolean;
  savingsPercent: number;
}

export interface CreditOperationResult {
  success: boolean;
  message: string;
  amount: number;
  newBalance: number;
  transactionId?: number;
}

export interface PurchaseRequest {
  packageId: number;
  paymentId: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = environment.apiUrl;

  // BehaviorSubject para manter o estado dos créditos atualizados globalmente
  private creditsSubject = new BehaviorSubject<UserCredits | null>(null);
  public credits$ = this.creditsSubject.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Obtém informações do usuário atual, incluindo créditos
   */
  getCurrentUser(): Observable<UserCredits> {
    return this.http.get<UserCredits>(`${this.apiUrl}/User/me`).pipe(
      tap(credits => this.creditsSubject.next(credits))
    );
  }

  /**
   * Obtém o saldo de créditos do usuário
   */
  getCredits(): Observable<UserCredits> {
    return this.http.get<UserCredits>(`${this.apiUrl}/User/credits`).pipe(
      tap(credits => this.creditsSubject.next(credits))
    );
  }

  /**
   * Atualiza o saldo de créditos no BehaviorSubject
   */
  refreshCredits(): void {
    this.getCredits().subscribe();
  }

  /**
   * Obtém o histórico de transações de créditos
   */
  getCreditHistory(page: number = 1, pageSize: number = 20): Observable<PagedResult<CreditTransaction>> {
    return this.http.get<PagedResult<CreditTransaction>>(
      `${this.apiUrl}/User/credits/history?page=${page}&pageSize=${pageSize}`
    );
  }

  /**
   * Lista os pacotes de créditos disponíveis
   */
  getCreditPackages(): Observable<CreditPackage[]> {
    return this.http.get<CreditPackage[]>(`${this.apiUrl}/Credits/packages`);
  }

  /**
   * Obtém detalhes de um pacote específico
   */
  getPackageById(packageId: number): Observable<CreditPackage> {
    return this.http.get<CreditPackage>(`${this.apiUrl}/Credits/packages/${packageId}`);
  }

  /**
   * Compra um pacote de créditos
   */
  purchaseCredits(request: PurchaseRequest): Observable<CreditOperationResult> {
    return this.http.post<CreditOperationResult>(`${this.apiUrl}/Credits/purchase`, request).pipe(
      tap(result => {
        if (result.success) {
          // Atualiza o saldo após compra bem-sucedida
          const current = this.creditsSubject.value;
          if (current) {
            this.creditsSubject.next({
              ...current,
              availableCredits: result.newBalance
            });
          }
        }
      })
    );
  }

  /**
   * Estima o custo de uma busca
   */
  estimateSearchCost(providerCount: number): Observable<{ providerCount: number; estimatedCost: number; description: string }> {
    return this.http.get<{ providerCount: number; estimatedCost: number; description: string }>(
      `${this.apiUrl}/Credits/estimate?providerCount=${providerCount}`
    );
  }

  /**
   * Atualiza localmente o saldo após consumo (chamado pelo cart-service ou search)
   */
  updateCreditsAfterConsumption(creditsUsed: number): void {
    const current = this.creditsSubject.value;
    if (current) {
      this.creditsSubject.next({
        ...current,
        availableCredits: current.availableCredits - creditsUsed,
        totalCreditsUsed: current.totalCreditsUsed + creditsUsed
      });
    }
  }

  /**
   * Limpa os dados do usuário (chamado no logout)
   */
  clearUserData(): void {
    this.creditsSubject.next(null);
  }
}
