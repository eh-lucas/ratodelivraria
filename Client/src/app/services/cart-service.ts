import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth-service';

export interface CartBookItem {
  title: string;
  isbn?: string;
  author?: string;
  quantity: number;
}

export interface CartOptimizationRequest {
  books: CartBookItem[];
  strategy: OptimizationStrategy;
  maxProviders: number;
  includeShipping: boolean;
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
  executionTimeMs: number;
  creditsUsed: number;
  fromCache: boolean;
}

export interface StrategyOption {
  value: number;
  name: string;
  description: string;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private apiUrl = 'http://localhost:5177/api/cart';

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

  optimizeCart(request: CartOptimizationRequest): Observable<CartOptimizationResult> {
    return this.http.post<CartOptimizationResult>(
      `${this.apiUrl}/optimize`,
      request,
      { headers: this.getHeaders() }
    );
  }

  searchBook(title: string, isbn?: string, author?: string): Observable<CartOptimizationResult> {
    let url = `${this.apiUrl}/search?title=${encodeURIComponent(title)}`;
    if (isbn) url += `&isbn=${encodeURIComponent(isbn)}`;
    if (author) url += `&author=${encodeURIComponent(author)}`;

    return this.http.get<CartOptimizationResult>(url, { headers: this.getHeaders() });
  }

  getStrategies(): Observable<StrategyOption[]> {
    return this.http.get<StrategyOption[]>(`${this.apiUrl}/strategies`);
  }
}
