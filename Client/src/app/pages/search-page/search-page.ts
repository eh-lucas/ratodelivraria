import { Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { CartService, CartBookItem, ProviderOption } from '../../services/cart-service';
import { BookInfo, CatalogService, CatalogSuggestion } from '../../services/catalog-service';
import { RankingService, PopularBook } from '../../services/ranking-service';
import { SearchStateService } from '../../services/search-state';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { PluralPipe } from '../../i18n/plural-pipe';

const RECENT_KEY = 'sherlock.search.recent';
const MAX_RECENT = 5;

interface RecentSearch {
  isbns: string[];
  /** Títulos na mesma ordem dos ISBNs; vazio em registros antigos. */
  titles?: string[];
  at: number;
}

/**
 * Sugestões por termo. 25 é o teto da API; medido em 2026-11-02: 5–17 ms e ~4,4 KB
 * de JSON, contra 6–14 ms e ~1,4 KB com 8. O custo real são as capas (~7–13 KB cada,
 * em paralelo pelo HTTP/2 do CDN).
 */
const SUGGESTION_LIMIT = 25;

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

  // Os mais consultados: serve de ponto de partida para quem chega sem ISBN
  popular: PopularBook[] = [];

  // Sugestões por nome (catálogo local)
  suggestions: CatalogSuggestion[] = [];
  suggestionsOpen = false;
  /** Capas que deram erro: no lugar fica o bloco cinza, sem ícone quebrado. */
  readonly brokenThumbs = new Set<number>();
  /** Idem para as capas da lista de livros, por ISBN. */
  readonly brokenCovers = new Set<string>();

  /**
   * Quantas sugestões cabem na lista sem rolar (420 px / 64 px por linha). As capas
   * dessas vão com prioridade alta; as de baixo também baixam na hora, mas depois.
   */
  readonly visibleSuggestions = 7;
  resolvingSuggestionId: number | null = null;
  suggestionError = '';
  private readonly nameQuery$ = new Subject<string>();

  @ViewChild('providersBox') providersBox?: ElementRef;
  @ViewChild('isbnInput') isbnInput?: ElementRef<HTMLInputElement>;

  constructor(
    private cartService: CartService,
    private catalogService: CatalogService,
    private rankingService: RankingService,
    private state: SearchStateService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadRecent();
    this.state.ensureKnown(this.books.map(b => b.isbn)).subscribe();

    // Autocomplete por nome: espera o usuário parar de digitar antes de consultar
    this.nameQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(term => this.catalogService.suggest(term, SUGGESTION_LIMIT)),
      )
      .subscribe(list => {
        this.suggestions = list;
        this.suggestionsOpen = list.length > 0;
      });

    this.rankingService.mostSearched(10).subscribe(list => {
      this.popular = list;
      list.forEach(l => this.state.remember({ isbn: l.isbn, title: l.title, author: null, thumbUrl: null }));
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

    // ISBN digitado à mão: a pessoa precisa ver qual livro entrou, não o número
    this.state.ensureKnown([isbn]).subscribe();
  }

  /** Título, autor e capa do livro, se o sistema já conhece. */
  infoOf(isbn: string): BookInfo | null {
    return this.state.infoOf(isbn);
  }

  titleOf(isbn: string): string {
    return this.infoOf(isbn)?.title || `ISBN ${isbn}`;
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

    const remember = (isbn: string) =>
      this.state.remember({ isbn, title: suggestion.title, author: suggestion.author, thumbUrl: suggestion.thumbUrl });

    if (suggestion.isbn) {
      remember(suggestion.isbn);
      this.addIsbn(suggestion.isbn);
      return;
    }

    this.resolvingSuggestionId = suggestion.id;
    this.catalogService.resolveIsbn(suggestion.id).subscribe(result => {
      this.resolvingSuggestionId = null;

      if (result.found && result.isbn) {
        remember(result.isbn);
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
    this.saveRecent(this.books.map(b => b.isbn), this.books.map(b => this.infoOf(b.isbn)?.title || ''));

    this.router.navigate(['/resultado'], {
      queryParams: { isbn: this.books.map(b => (b.quantity > 1 ? `${b.isbn}*${b.quantity}` : b.isbn)) },
    });
  }

  // ===== Buscas recentes =====

  private loadRecent(): void {
    try {
      const raw = localStorage.getItem(RECENT_KEY);
      const parsed = raw ? JSON.parse(raw) : [];
      // Registro sem ISBNs (storage mexido) derrubava a tela inteira no template
      this.recent = Array.isArray(parsed)
        ? parsed.filter((r: RecentSearch) => Array.isArray(r?.isbns) && r.isbns.length > 0)
        : [];
    } catch {
      this.recent = [];
    }

    // Buscas antigas, gravadas só com o número: pede os títulos uma vez
    const semTitulo = this.recent.filter(r => !r.titles?.length).flatMap(r => r.isbns);
    if (semTitulo.length > 0) this.state.ensureKnown(semTitulo).subscribe();
  }

  /** Rótulo de uma busca recente: os títulos que conhecemos; o resto fica como ISBN. */
  recentLabel(r: RecentSearch): string {
    return r.isbns
      .map((isbn, i) => r.titles?.[i] || this.infoOf(isbn)?.title || `ISBN ${isbn}`)
      .join(' · ');
  }

  private saveRecent(isbns: string[], titles: string[]): void {
    const key = isbns.join(',');
    const next = [
      { isbns, titles, at: Date.now() },
      ...this.recent.filter(r => r.isbns.join(',') !== key),
    ].slice(0, MAX_RECENT);

    this.recent = next;
    try {
      localStorage.setItem(RECENT_KEY, JSON.stringify(next));
    } catch {
      /* ignora */
    }
  }

  // ===== Mais consultados =====

  /** Um toque no ranking já busca aquele livro: é o caminho mais curto da tela. */
  buscarPopular(livro: PopularBook): void {
    this.state.remember({ isbn: livro.isbn, title: livro.title, author: null, thumbUrl: null });
    this.books = [{ isbn: livro.isbn, quantity: 1 }];
    this.search();
  }

  formatarPreco(valor: number | null): string {
    if (valor == null || valor <= 0) return '';
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  useRecent(r: RecentSearch): void {
    this.books = r.isbns.map(isbn => ({ isbn, quantity: 1 }));
    this.state.ensureKnown(r.isbns).subscribe();
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
