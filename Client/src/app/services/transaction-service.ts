import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TransactionHistory {
  id: number;
  startedAt: string;
  endedAt: string | null;
  executionTimeMs: number;
  totalProvidersQueried: number;
  successfulQueries: number;
  costCredits: number;
  inputParameters: string;
  isSuccess: boolean;
  bestTitle?: string;
  bestAuthor?: string;
  bestPrice?: number;
  bestProvider?: string;
  bestProductUrl?: string;
  /** Capa do livro (da melhor oferta ou de qualquer loja que respondeu). */
  imageUrl?: string;
  /** Lojas com o livro disponível e com preço. */
  offersCount: number;
  /** Lojas que conhecem o livro mas estavam sem estoque. */
  unavailableCount: number;
}

export interface QueryDetail {
  id: number;
  providerId: number;
  providerName: string;
  responseTimeMs: number;
  success: boolean;
  title?: string;
  author?: string;
  price?: number;
  discount?: number;
  productUrl?: string;
  errorMessage?: string;
  fromCache: boolean;
  available: boolean;
}

export interface TransactionDetail extends TransactionHistory {
  queries: QueryDetail[];
}

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private apiUrl = `${environment.apiUrl}/Transactions`;

  constructor(private http: HttpClient) {}

  listMyHistory(limit = 50): Observable<TransactionHistory[]> {
    return this.http.get<TransactionHistory[]>(`${this.apiUrl}?limit=${limit}`);
  }

  getDetail(id: number): Observable<TransactionDetail> {
    return this.http.get<TransactionDetail>(`${this.apiUrl}/${id}`);
  }
}
