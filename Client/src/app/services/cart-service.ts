import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, timer, switchMap, map, takeWhile } from 'rxjs';
import { AuthService } from './auth-service';
import { environment } from '../../environments/environment';

export interface CartBookItem {
  isbn: string;
  quantity: number;
}

export interface SearchProgressResponse {
  total: number;
  completed: number;
  done: boolean;
  error?: string | null;
  result?: CartOptimizationResult | null;
}

/** Andamento de uma busca em curso, emitido a cada consulta de status. */
export interface SearchProgressUpdate {
  total: number;
  completed: number;
  done: boolean;
  error: string | null;
  result: CartOptimizationResult | null;
}

export interface CartOptimizationRequest {
  books: CartBookItem[];
  strategy: OptimizationStrategy;
  maxProviders: number;
  includeShipping: boolean;
  providerUrls?: string[];
}

export enum OptimizationStrategy {
  LowestTotal = 0,
  FewestOrders = 1,
  PrioritizeFreeShipping = 2,
  SingleProvider = 3
}

export interface ProviderCartItem {
  title: string;
  author?: string;
  isbn?: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  discount?: number;
  productUrl?: string;
}

export interface ProviderCart {
  providerId: number;
  providerName: string;
  providerUrl: string;
  items: ProviderCartItem[];
  subtotal: number;
  shippingCost: number;
  total: number;
  freeShippingThreshold?: number;
  hasFreeShipping: boolean;
}

export interface BookPriceDetail {
  isbn: string;
  title: string;
  price: number;
  quantity: number;
  totalPrice: number;
  productUrl?: string;
}

export interface ProviderComparison {
  providerId: number;
  providerName: string;
  providerUrl: string;
  booksFound: number;
  totalBooksRequested: number;
  hasAllBooks: boolean;
  totalPrice: number;
  bookPrices: BookPriceDetail[];
  missingIsbns: string[];
}

/**
 * Detalhe bruto de uma consulta (livro × site), incluindo falhas.
 * Diferente de ProviderComparison, que só traz sites com preço válido.
 */
export interface ProviderQueryDetail {
  isbn: string;
  providerId: number;
  providerName: string;
  providerUrl: string;
  success: boolean;
  hasResult: boolean;
  title: string | null;
  author: string | null;
  price: number | null;
  discount: number | null;
  productUrl: string | null;
  responseTimeMs: number;
  errorMessage: string | null;
  errorType: string | null;
  fromCache: boolean;
}

export interface CartOptimizationResult {
  success: boolean;
  message: string;
  totalCost: number;
  booksCost: number;
  shippingCost: number;
  savings: number;
  savingsPercent: number;
  providerCarts: ProviderCart[];
  booksNotFound: string[];
  providerComparisons: ProviderComparison[];
  executionTimeMs: number;
  creditsUsed: number;
  fromCache: boolean;
  totalBooksRequested: number;
  totalQueriesExecuted: number;
  providerQueries: ProviderQueryDetail[];
}

export interface StrategyOption {
  value: number;
  name: string;
  description: string;
}

export interface ProviderOption {
  id: number;
  name: string;
  url: string;
  category: string;
  isActive: boolean;
}

// ========================================
// Interfaces para BookSearch (nova API)
// ========================================

export interface QueryResultItem {
  providerId: number;
  providerName: string;
  providerUrl: string;
  title: string | null;
  author: string | null;
  price: number | null;
  discount: number | null;
  productUrl: string | null;
  success: boolean;
  errorMessage: string | null;
  errorType: string | null;
  responseTimeMs: number;
  credits: number;
}

export interface BookSearchResponse {
  bestResult: QueryResultItem | null;
  allResults: QueryResultItem[];
  totalProviders: number;
  successCount: number;
  errorCount: number;
  totalCredits: number;
  executionTimeMs: number;
  searchedIsbn: string;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private apiUrl = `${environment.apiUrl}/cart`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) { }

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    });
  }

  /**
   * Inicia a busca em segundo plano e acompanha o andamento por polling.
   *
   * Emite o progresso a cada consulta e, ao final, o resultado completo — assim a tela
   * mostra "23 de 67 lojas" em vez de uma espera sem informação.
   */
  optimizeCartWithProgress(request: CartOptimizationRequest): Observable<SearchProgressUpdate> {
    return this.http
      .post<{ jobId: string }>(`${this.apiUrl}/optimize-async`, request, { headers: this.getHeaders() })
      .pipe(
        switchMap(({ jobId }) =>
          // 1,5s equilibra atualização fluida e número de requisições
          timer(0, 1500).pipe(
            switchMap(() =>
              this.http.get<SearchProgressResponse>(
                `${this.apiUrl}/progress/${jobId}`,
                { headers: this.getHeaders() },
              ),
            ),
            map(p => ({
              total: p.total,
              completed: p.completed,
              done: p.done,
              error: p.error ?? null,
              result: p.result ?? null,
            })),
            takeWhile(p => !p.done, true),
          ),
        ),
      );
  }

  optimizeCart(request: CartOptimizationRequest): Observable<CartOptimizationResult> {
    return this.http.post<CartOptimizationResult>(
      `${this.apiUrl}/optimize`,
      request,
      { headers: this.getHeaders() }
    );
  }

  searchBook(isbn: string, providerUrls?: string[]): Observable<CartOptimizationResult> {
    const params: string[] = [`isbn=${encodeURIComponent(isbn)}`];
    if (providerUrls && providerUrls.length > 0) {
      params.push(`providerUrls=${encodeURIComponent(providerUrls.join(','))}`);
    }

    const url = `${this.apiUrl}/search?${params.join('&')}`;
    return this.http.get<CartOptimizationResult>(url, { headers: this.getHeaders() });
  }

  /**
   * Busca livro por ISBN usando a nova API com resultados detalhados
   */
  searchBookByIsbn(isbn: string, providerUrls?: string[]): Observable<BookSearchResponse> {
    const params: string[] = [`isbn=${encodeURIComponent(isbn)}`];
    if (providerUrls && providerUrls.length > 0) {
      params.push(`providerUrls=${encodeURIComponent(providerUrls.join(','))}`);
    }

    const url = `${environment.apiUrl}/BookSearch?${params.join('&')}`;
    return this.http.get<BookSearchResponse>(url, { headers: this.getHeaders() });
  }

  getStrategies(): Observable<StrategyOption[]> {
    return this.http.get<StrategyOption[]>(`${this.apiUrl}/strategies`);
  }

  getProviders(): Observable<ProviderOption[]> {
    return this.http.get<ProviderOption[]>(`${environment.apiUrl}/providers`);
  }

  getActiveProviders(): Observable<ProviderOption[]> {
    return this.http.get<ProviderOption[]>(`${environment.apiUrl}/providers/active`);
  }
}
