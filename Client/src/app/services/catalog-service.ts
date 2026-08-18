import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface CatalogSuggestion {
  id: number;
  title: string;
  /** Só vem preenchido quando o ISBN já foi resolvido antes. */
  isbn: string | null;
}

export interface ResolveIsbnResult {
  found: boolean;
  isbn?: string;
  title?: string;
  error?: string;
}

/**
 * Sugestões de título vindas do catálogo espelhado das lojas.
 * Consulta local: não custa crédito nem dispara scraping.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Catalog`;

  suggest(query: string, limit = 8): Observable<CatalogSuggestion[]> {
    const params = `q=${encodeURIComponent(query)}&limit=${limit}`;
    return this.http
      .get<CatalogSuggestion[]>(`${this.apiUrl}/suggest?${params}`)
      .pipe(catchError(() => of([])));
  }

  /** Abre a página do produto na loja para descobrir o ISBN. */
  resolveIsbn(catalogItemId: number): Observable<ResolveIsbnResult> {
    return this.http
      .post<ResolveIsbnResult>(`${this.apiUrl}/${catalogItemId}/resolve-isbn`, {})
      .pipe(catchError(err => of(err?.error ?? { found: false, error: 'Falha ao consultar a loja.' })));
  }
}
