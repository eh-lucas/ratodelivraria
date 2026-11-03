import { Injectable, inject } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { CartBookItem, CartOptimizationResult } from './cart-service';
import { BookInfo, CatalogService } from './catalog-service';

/**
 * Estado compartilhado entre a tela de busca e a tela de resultado.
 *
 * A tela de resultado é quem dispara a consulta (para funcionar também quando
 * aberta direto pela URL), mas o resultado fica guardado aqui para que voltar
 * e avançar no navegador não gaste créditos de novo. Persistimos em
 * sessionStorage para sobreviver a um F5 — buscar tem custo, repetir sem o
 * usuário pedir seria cobrar duas vezes pela mesma coisa.
 */
export interface SearchSnapshot {
  signature: string;
  books: CartBookItem[];
  providerUrls: string[];
  providerNames: Record<string, string>;
  result: CartOptimizationResult;
  completedAt: number;
}

const STORAGE_KEY = 'sherlock.search.lastResult';
const BOOKS_KEY = 'sherlock.search.books';
const MAX_KNOWN_BOOKS = 200;

@Injectable({ providedIn: 'root' })
export class SearchStateService {
  /** Seleção de sites feita na tela de busca; a de resultado reaproveita. */
  selectedProviderUrls: string[] = [];

  /** Nome por URL, para exibir sites que falharam (o resultado só traz os que acharam preço). */
  providerNames: Record<string, string> = {};

  private snapshot: SearchSnapshot | null = null;

  /**
   * Título, autor e capa por ISBN, para as telas mostrarem o livro e não o
   * número. Alimentado pelas sugestões, pelo ranking e pela própria busca;
   * o que falta é pedido à API uma vez e fica guardado na sessão.
   */
  private knownBooks: Record<string, BookInfo> = {};
  private readonly catalog = inject(CatalogService);

  constructor() {
    this.restore();
  }

  infoOf(isbn: string): BookInfo | null {
    return this.knownBooks[isbn] ?? null;
  }

  remember(info: BookInfo): void {
    if (!info?.isbn) return;
    const current = this.knownBooks[info.isbn];
    // Não troca um título conhecido por vazio nem uma capa por nenhuma
    this.knownBooks[info.isbn] = {
      isbn: info.isbn,
      title: info.title || current?.title || null,
      author: info.author || current?.author || null,
      thumbUrl: info.thumbUrl || current?.thumbUrl || null,
    };
    this.persistBooks();
  }

  /**
   * Busca na API só os ISBNs que ainda não conhece direito; devolve a lista completa.
   * "Direito" = com autor ou capa: o ranking entrega só o título, e isso não
   * basta para a linha do livro.
   */
  ensureKnown(isbns: string[]): Observable<BookInfo[]> {
    const complete = (b?: BookInfo) => !!b?.title && !!(b.author || b.thumbUrl);
    const missing = [...new Set(isbns)].filter(i => !complete(this.knownBooks[i]));
    if (missing.length === 0) return of(isbns.map(i => this.knownBooks[i]).filter(Boolean));

    return this.catalog.lookup(missing.slice(0, 20)).pipe(
      tap(list => list.forEach(b => this.remember(b))),
    );
  }

  /** Assinatura da consulta: mesmos livros + mesmos sites = mesmo resultado. */
  buildSignature(books: CartBookItem[], providerUrls: string[]): string {
    const b = books
      .map(x => `${x.isbn}:${x.quantity}`)
      .sort()
      .join(',');
    const p = [...providerUrls].sort().join(',');
    return `${b}|${p}`;
  }

  get(signature: string): SearchSnapshot | null {
    return this.snapshot?.signature === signature ? this.snapshot : null;
  }

  save(snapshot: SearchSnapshot): void {
    this.snapshot = snapshot;
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(snapshot));
    } catch {
      // Cota estourada ou storage indisponível: seguimos só com o cache em memória
    }
  }

  clear(): void {
    this.snapshot = null;
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignora */
    }
  }

  private persistBooks(): void {
    try {
      const entries = Object.values(this.knownBooks).slice(-MAX_KNOWN_BOOKS);
      sessionStorage.setItem(BOOKS_KEY, JSON.stringify(entries));
    } catch {
      /* ignora */
    }
  }

  private restore(): void {
    try {
      const books = sessionStorage.getItem(BOOKS_KEY);
      if (books) {
        const list = JSON.parse(books);
        if (Array.isArray(list)) {
          for (const b of list) {
            if (b && typeof b.isbn === 'string') this.knownBooks[b.isbn] = b;
          }
        }
      }
    } catch {
      /* ignora */
    }

    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as SearchSnapshot;
      if (parsed?.signature && parsed?.result) {
        this.snapshot = parsed;
        this.providerNames = parsed.providerNames ?? {};
        this.selectedProviderUrls = parsed.providerUrls ?? [];
      }
    } catch {
      /* snapshot corrompido: ignora */
    }
  }
}
