import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Em relação a quê a queda é medida. Espelha o enum `WatchRule` do backend, e os
 * números importam: o JSON manda 0 ou 1.
 */
export enum WatchRule {
  TargetPrice = 0,
  PercentDrop = 1,
}

export interface WatchedBook {
  id: number;
  isbn: string;
  title: string;
  author?: string;
  imageUrl?: string;

  rule: WatchRule;
  targetPrice?: number;
  dropPercent: number;

  referencePrice?: number;
  lastPrice?: number;
  previousPrice?: number;
  lastCheckedAt?: string;
  lastProviderName?: string;
  lastProductUrl?: string;

  /** Menor preço dos últimos 30 dias — o contexto que faz a seta significar algo. */
  lowestPrice30d?: number;

  lastNotifiedAt?: string;
  active: boolean;
  createdAt: string;
}

export interface WatchedBookInput {
  isbn: string;
  title?: string;
  author?: string;
  imageUrl?: string;
  rule: WatchRule;
  targetPrice?: number;
  dropPercent?: number;
}

export interface WatchedBatchResult {
  items: WatchedBook[];
  rejected: { isbn: string; reason: string }[];
}

/**
 * A lista de observados vive no servidor.
 *
 * Antes era `localStorage`, e isso era um engano com data marcada: a lista some
 * ao trocar de navegador, e quem manda o e-mail de queda de preço é o servidor —
 * uma lista que só o navegador conhece nunca ia virar aviso nenhum.
 */
@Injectable({ providedIn: 'root' })
export class WatchedService {
  private apiUrl = `${environment.apiUrl}/Watched`;

  private subject = new BehaviorSubject<WatchedBook[]>([]);
  public items$: Observable<WatchedBook[]> = this.subject.asObservable();

  constructor(private http: HttpClient) {}

  /** Recarrega do servidor. A tela chama no ngOnInit. */
  load(): Observable<WatchedBook[]> {
    return this.http.get<WatchedBook[]>(this.apiUrl).pipe(
      tap(items => this.subject.next(items)),
    );
  }

  list(): WatchedBook[] {
    return this.subject.value;
  }

  add(items: WatchedBookInput[]): Observable<WatchedBatchResult> {
    return this.http.post<WatchedBatchResult>(this.apiUrl, { items }).pipe(
      // O lote pode trazer item novo e item reaproveitado na mesma resposta, e
      // casar isso em memória daria divergência silenciosa entre tela e banco.
      // Reler a lista inteira é uma requisição a mais e nenhuma dúvida.
      tap(() => this.load().subscribe()),
    );
  }

  update(id: number, patch: Partial<Pick<WatchedBook, 'rule' | 'targetPrice' | 'dropPercent' | 'active'>>): Observable<WatchedBook> {
    return this.http.patch<WatchedBook>(`${this.apiUrl}/${id}`, patch).pipe(
      tap(atualizado => this.subject.next(
        this.subject.value.map(w => (w.id === id ? atualizado : w)),
      )),
    );
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.subject.next(this.subject.value.filter(w => w.id !== id))),
    );
  }

  togglePause(item: WatchedBook): Observable<WatchedBook> {
    return this.update(item.id, { active: !item.active });
  }

  /** Limpa a lista em memória — chamado no logout, para não vazar entre contas. */
  clear(): void {
    this.subject.next([]);
  }

  static ruleLabel(w: WatchedBook): string {
    return w.rule === WatchRule.TargetPrice
      ? `alvo R$ ${(w.targetPrice ?? 0).toFixed(2)}`
      : `queda de ${w.dropPercent}%`;
  }
}
