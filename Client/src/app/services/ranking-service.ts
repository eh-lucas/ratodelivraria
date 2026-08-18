import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface PopularBook {
  isbn: string;
  searches: number;
  title: string;
  lowestPrice: number | null;
  lastSearchedAt: string;
}

/**
 * Ranking dos livros mais consultados. Vem do nosso próprio histórico —
 * não consulta loja, não custa crédito.
 */
@Injectable({ providedIn: 'root' })
export class RankingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/Ranking`;

  mostSearched(limit = 10): Observable<PopularBook[]> {
    return this.http
      .get<PopularBook[]>(`${this.apiUrl}/most-searched?limit=${limit}`)
      // Ranking é enfeite: se falhar, a busca continua funcionando sem ele.
      .pipe(catchError(() => of([])));
  }
}
