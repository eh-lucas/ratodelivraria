import { Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { CartService, CartBookItem, ProviderOption } from '../../services/cart-service';
import { CatalogService, CatalogSuggestion } from '../../services/catalog-service';
import { SearchStateService } from '../../services/search-state';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { PluralPipe } from '../../i18n/plural-pipe';

const RECENT_KEY = 'sherlock.search.recent';
const MAX_RECENT = 5;

interface RecentSearch {
  isbns: string[];
  at: number;
}

@Component({
  selector: 'search-page',
  standalone: true,
  imports: [TranslatePipe, PluralPipe, CommonModule, FormsModule],
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss',
})
export class SearchPage implements OnInit {
  // Livros a consultar
  books: CartBookItem[] = [];
  newIsbn = '';

  // Sites disponíveis e selecionados
  providers: ProviderOption[] = [];
  selectedProviderUrls: string[] = [];
  providersOpen = false;
  providerFilter = '';
  providersLoading = true;

  errorMessage = '';
  recent: RecentSearch[] = [];

  // Sugestões por nome (catálogo local)
  suggestions: CatalogSuggestion[] = [];
  suggestionsOpen = false;
  resolvingSuggestionId: number | null = null;
  suggestionError = '';
  private readonly nameQuery$ = new Subject<string>();

  @ViewChild('providersBox') providersBox?: ElementRef;
  @ViewChild('isbnInput') isbnInput?: ElementRef<HTMLInputElement>;

  constructor(
    private cartService: CartService,
    private catalogService: CatalogService,
    private state: SearchStateService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadRecent();

    // Autocomplete por nome: espera o usuário parar de digitar antes de consultar
    this.nameQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(term => this.catalogService.suggest(term)),
      )
      .subscribe(list => {
        this.suggestions = list;
        this.suggestionsOpen = list.length > 0;
      });

    this.cartService.getActiveProviders().subscribe({
      next: list => {
        this.providers = list;
        this.state.providerNames = Object.fromEntries(list.map(p => [p.url, p.name]));
        // Mantém a seleção anterior da sessão; na primeira visita marca todos
        const previous = this.state.selectedProviderUrls.filter(u => list.some(p => p.url === u));
        this.selectedProviderUrls = previous.length > 0 ? previous : list.map(p => p.url);
        this.providersLoading = false;
      },
      error: () => {
        this.errorMessage = 'Não foi possível carregar a lista de sites.';
        this.providersLoading = false;
      },
    });
  }

  // ===== Livros =====

  addBook(): void {
    // Enter durante busca por nome: usa a primeira sugestão, nunca o texto cru.
    if (this.isSearchingByName) {
      if (this.suggestions.length > 0) this.selectSuggestion(this.suggestions[0]);
      return;
    }

    const isbn = this.normalizeIsbn(this.newIsbn);
    if (!isbn) return;

    if (this.books.some(b => b.isbn === isbn)) {
      this.newIsbn = '';
      return;
    }

    this.books.push({ isbn, quantity: 1 });
    this.newIsbn = '';
    this.isbnInput?.nativeElement.focus();
  }

  // ===== Busca por nome =====

  /** Texto que parece ISBN (10 ou 13 dígitos) segue o fluxo antigo; o resto vira busca por nome. */
  get isSearchingByName(): boolean {
    const raw = this.normalizeIsbn(this.newIsbn);
    return raw.length > 0 && !/^\d{9}[\dXx]$|^\d{13}$/.test(raw) && !/^\d+$/.test(raw);
  }

  onQueryChange(): void {
    this.suggestionError = '';

    if (!this.isSearchingByName) {
      this.suggestions = [];
      this.suggestionsOpen = false;
      return;
    }

    this.nameQuery$.next(this.newIsbn.trim());
  }

  /**
   * Ao escolher uma sugestão: se o ISBN já é conhecido, adiciona direto;
   * senão, pede ao backend que descubra na página do produto.
   */
  selectSuggestion(suggestion: CatalogSuggestion): void {
    this.suggestionError = '';

    if (suggestion.isbn) {
      this.addIsbn(suggestion.isbn);
      return;
    }

    this.resolvingSuggestionId = suggestion.id;
    this.catalogService.resolveIsbn(suggestion.id).subscribe(result => {
      this.resolvingSuggestionId = null;

      if (result.found && result.isbn) {
        this.addIsbn(result.isbn);
      } else {
        this.suggestionError = result.error || 'Não foi possível identificar o ISBN deste título.';
      }
    });
  }

  private addIsbn(isbn: string): void {
    if (!this.books.some(b => b.isbn === isbn)) {
      this.books.push({ isbn, quantity: 1 });
    }

    this.newIsbn = '';
    this.suggestions = [];
    this.suggestionsOpen = false;
    this.isbnInput?.nativeElement.focus();
  }

  closeSuggestions(): void {
    this.suggestionsOpen = false;
  }

  removeBook(isbn: string): void {
    this.books = this.books.filter(b => b.isbn !== isbn);
  }

  changeQuantity(isbn: string, delta: number): void {
    this.books = this.books.map(b =>
      b.isbn === isbn ? { ...b, quantity: Math.max(1, Math.min(99, b.quantity + delta)) } : b,
    );
  }

  clearBooks(): void {
    this.books = [];
  }

  // Remove hífens e espaços; ISBN é digitado de várias formas
  private normalizeIsbn(raw: string): string {
    return raw.replace(/[\s-]/g, '').trim();
  }

  /** Formato plausível de ISBN — serve só para avisar, não bloqueia a busca. */
  get isbnLooksValid(): boolean {
    if (this.isSearchingByName) return true;

    const isbn = this.normalizeIsbn(this.newIsbn);
    if (!isbn) return true;
    return /^\d{9}[\dXx]$|^\d{13}$/.test(isbn);
  }

  get canAdd(): boolean {
    if (this.isSearchingByName) return this.suggestions.length > 0;
    return this.normalizeIsbn(this.newIsbn).length > 0;
  }

  // ===== Sites =====

  toggleProviders(): void {
    this.providersOpen = !this.providersOpen;
  }

  toggleProvider(url: string): void {
    this.selectedProviderUrls = this.selectedProviderUrls.includes(url)
      ? this.selectedProviderUrls.filter(u => u !== url)
      : [...this.selectedProviderUrls, url];
  }

  selectAllProviders(): void {
    this.selectedProviderUrls = this.providers.map(p => p.url);
  }

  clearProviders(): void {
    this.selectedProviderUrls = [];
  }

  get filteredProviders(): ProviderOption[] {
    const q = this.providerFilter.trim().toLowerCase();
    if (!q) return this.providers;
    return this.providers.filter(p => p.name.toLowerCase().includes(q));
  }

  get allProvidersSelected(): boolean {
    return this.providers.length > 0 && this.selectedProviderUrls.length === this.providers.length;
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (this.providersOpen && this.providersBox && !this.providersBox.nativeElement.contains(e.target)) {
      this.providersOpen = false;
    }
  }

  // ===== Custo e disparo =====

  get estimatedCredits(): number {
    // Cada combinação livro × site é uma query = 1 crédito.
    // A quantidade não multiplica: o mesmo ISBN é consultado uma vez só.
    return this.books.length * this.selectedProviderUrls.length;
  }

  get canSearch(): boolean {
    return this.books.length > 0 && this.selectedProviderUrls.length > 0;
  }

  search(): void {
    if (!this.canSearch) return;

    // A tela de resultado é quem dispara a consulta; aqui só passamos o contexto
    this.state.selectedProviderUrls = this.selectedProviderUrls;
    this.saveRecent(this.books.map(b => b.isbn));

    this.router.navigate(['/resultado'], {
      queryParams: { isbn: this.books.map(b => (b.quantity > 1 ? `${b.isbn}*${b.quantity}` : b.isbn)) },
    });
  }

  // ===== Buscas recentes =====

  private loadRecent(): void {
    try {
      const raw = localStorage.getItem(RECENT_KEY);
      this.recent = raw ? (JSON.parse(raw) as RecentSearch[]) : [];
    } catch {
      this.recent = [];
    }
  }

  private saveRecent(isbns: string[]): void {
    const key = isbns.join(',');
    const next = [
      { isbns, at: Date.now() },
      ...this.recent.filter(r => r.isbns.join(',') !== key),
    ].slice(0, MAX_RECENT);

    this.recent = next;
    try {
      localStorage.setItem(RECENT_KEY, JSON.stringify(next));
    } catch {
      /* ignora */
    }
  }

  useRecent(r: RecentSearch): void {
    this.books = r.isbns.map(isbn => ({ isbn, quantity: 1 }));
  }

  clearRecent(): void {
    this.recent = [];
    try {
      localStorage.removeItem(RECENT_KEY);
    } catch {
      /* ignora */
    }
  }

  trackByIsbn(_: number, item: CartBookItem): string {
    return item.isbn;
  }
}
