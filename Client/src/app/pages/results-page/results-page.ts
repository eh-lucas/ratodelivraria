import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  CartService,
  CartBookItem,
  CartOptimizationResult,
  OptimizationStrategy,
  ProviderComparison,
  ProviderQueryDetail,
  PartialOffer,
} from '../../services/cart-service';
import { UserService } from '../../services/user-service';
import { SearchStateService } from '../../services/search-state';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { PluralPipe } from '../../i18n/plural-pipe';
import { CoffeeModalComponent } from '../../pix/coffee-modal';

/** Oferta consolidada de um site: o carrinho inteiro naquela loja. */
interface Offer {
  providerId: number;
  providerName: string;
  providerUrl: string;
  link: string;
  total: number;
  booksFound: number;
  totalBooks: number;
  hasAllBooks: boolean;
  items: OfferItem[];
  avgResponseMs: number;
  fromCache: boolean;
  /** % abaixo da média dos sites comparáveis (negativo = acima da média) */
  vsAverage: number;
  /** Amazon tem destaque próprio na lista; ver pinAmazon(). */
  isAmazon: boolean;
  /** Posição real por preço (1 = mais barata). Fixar o card não pode mentir sobre isso. */
  priceRank: number;
  /** Quanto esta oferta custa a mais que a melhor. 0 quando é a melhor. */
  overBest: number;
}

interface OfferItem {
  isbn: string;
  title: string;
  price: number;
  quantity: number;
  productUrl?: string;
  imageUrl?: string;
}

/** Estatísticas de um ISBN entre todos os sites que o encontraram. */
interface BookStats {
  isbn: string;
  title: string;
  foundIn: number;
  min: number;
  avg: number;
  max: number;
}

interface ProgressItem {
  url: string;
  name: string;
}

type SortKey = 'price' | 'price-desc';

@Component({
  selector: 'results-page',
  standalone: true,
  imports: [CoffeeModalComponent, TranslatePipe, PluralPipe, CommonModule, FormsModule],
  templateUrl: './results-page.html',
  styleUrl: './results-page.scss',
})
export class ResultsPage implements OnInit {
  /** Lojas já consultadas nesta busca e total esperado. */
  searchedCount = 0;
  searchTotal = 0;

  /** Percentual concluído, para a barra de progresso. */
  get searchPercent(): number {
    if (this.searchTotal <= 0) return 0;
    return Math.min(100, Math.round((this.searchedCount / this.searchTotal) * 100));
  }

  /** Modal de doação via Pix. */
  showCoffee = false;

  books: CartBookItem[] = [];
  providerUrls: string[] = [];

  result: CartOptimizationResult | null = null;
  isLoading = false;
  errorMessage = '';
  fromCachedSnapshot = false;

  progressItems: ProgressItem[] = [];

  /**
   * Ofertas que já chegaram, enquanto o resto ainda responde.
   *
   * A busca toda leva ~17s porque 67 livrarias dividem 2 IPs — teto do servidor
   * delas. Mas a primeira responde em ~1,5s, e é isso que a pessoa vê agora em
   * vez de barra de progresso.
   */
  partialOffers: PartialOffer[] = [];

  // Filtros do rail
  sortKey: SortKey = 'price';
  onlyComplete = false;
  providerFilter = '';
  priceCeiling: number | null = null;

  // Painel de diagnóstico
  diagnosticsOpen = false;
  onlyFailures = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private cartService: CartService,
    private userService: UserService,
    private state: SearchStateService,
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const raw = params.getAll('isbn');
      this.books = this.parseBooks(raw);

      if (this.books.length === 0) {
        this.router.navigate(['/search']);
        return;
      }

      this.resolveProvidersAndRun();
    });
  }

  /** `9788594090782*2` → { isbn, quantity: 2 } */
  private parseBooks(raw: string[]): CartBookItem[] {
    return raw
      .map(entry => {
        const [isbn, qty] = entry.split('*');
        const quantity = Number(qty);
        return { isbn: isbn.trim(), quantity: Number.isFinite(quantity) && quantity > 0 ? quantity : 1 };
      })
      .filter(b => b.isbn.length > 0);
  }

  /**
   * Sem seleção de sites em memória (link direto, F5) buscamos os ativos.
   * A consulta só dispara depois disso — a assinatura do cache depende dos sites.
   */
  private resolveProvidersAndRun(): void {
    if (this.state.selectedProviderUrls.length > 0) {
      this.providerUrls = this.state.selectedProviderUrls;
      this.run();
      return;
    }

    this.isLoading = true;
    this.cartService.getActiveProviders().subscribe({
      next: list => {
        this.providerUrls = list.map(p => p.url);
        this.state.selectedProviderUrls = this.providerUrls;
        this.state.providerNames = Object.fromEntries(list.map(p => [p.url, p.name]));
        this.run();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Não foi possível carregar a lista de sites.';
      },
    });
  }

  /** Reaproveita o resultado guardado quando a consulta é a mesma — buscar custa crédito. */
  private run(force = false): void {
    const signature = this.state.buildSignature(this.books, this.providerUrls);

    if (!force) {
      const cached = this.state.get(signature);
      if (cached) {
        this.applyResult(cached.result);
        this.fromCachedSnapshot = true;
        this.isLoading = false;
        return;
      }
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.result = null;
    this.allOffers = [];
    this.comparableTotals = [];
    this.cachedBookStats = [];
    this.cachedBestOffer = null;
    this.fromCachedSnapshot = false;
    this.progressItems = this.providerUrls.map(url => ({
      url,
      name: this.state.providerNames[url] ?? url,
    }));

    this.searchedCount = 0;
    this.searchTotal = this.providerUrls.length;

    this.cartService
      .optimizeCartWithProgress({
        books: this.books,
        strategy: OptimizationStrategy.LowestTotal,
        maxProviders: 0,
        includeShipping: true,
        providerUrls: this.providerUrls,
      })
      .subscribe({
        next: update => {
          this.searchedCount = update.completed;
          if (update.total > 0) this.searchTotal = update.total;
          if (!update.done) this.partialOffers = update.offers;

          if (!update.done) return;

          this.partialOffers = [];

          if (update.error || !update.result) {
            this.errorMessage = update.error || 'Erro ao consultar os livros.';
            this.isLoading = false;
            return;
          }

          const r = update.result;
          this.applyResult(r);
          this.isLoading = false;
          this.priceCeiling = null;
          this.state.save({
            signature,
            books: this.books,
            providerUrls: this.providerUrls,
            providerNames: this.state.providerNames,
            result: r,
            completedAt: Date.now(),
          });
          if (r.creditsUsed) this.userService.updateCreditsAfterConsumption(r.creditsUsed);
        },
        error: err => {
          this.errorMessage = err?.error?.error || err?.error?.message || 'Erro ao consultar os livros.';
          this.isLoading = false;
        },
      });
  }

  refresh(): void {
    this.run(true);
  }

  newSearch(): void {
    this.router.navigate(['/search']);
  }

  // ===== Ofertas =====

  // Derivados do resultado. Calculados uma vez por resposta em vez de a cada
  // ciclo de detecção de mudança — são ~84 sites e o template lê isso várias vezes.
  private allOffers: Offer[] = [];
  private comparableTotals: number[] = [];
  private cachedBookStats: BookStats[] = [];
  private cachedBestOffer: Offer | null = null;

  private applyResult(r: CartOptimizationResult): void {
    this.result = r;
    this.allOffers = this.buildOffers(r);

    const complete = this.allOffers.filter(o => o.hasAllBooks);
    this.comparableTotals = (complete.length > 0 ? complete : this.allOffers)
      .map(o => o.total)
      .sort((a, b) => a - b);

    this.cachedBestOffer = complete.length > 0
      ? complete.reduce((best, o) => (o.total < best.total ? o : best))
      : null;

    this.rankOffers();

    this.cachedBookStats = this.buildBookStats();
  }

  /** Agrupa as queries brutas por site (tempo de resposta, cache, erro). */
  private groupQueriesByProvider(r: CartOptimizationResult): Map<string, ProviderQueryDetail[]> {
    const map = new Map<string, ProviderQueryDetail[]>();
    for (const q of r.providerQueries ?? []) {
      const list = map.get(q.providerName) ?? [];
      list.push(q);
      map.set(q.providerName, list);
    }
    return map;
  }

  private buildOffers(result: CartOptimizationResult): Offer[] {
    const byProvider = this.groupQueriesByProvider(result);
    const comparisons = result.providerComparisons ?? [];

    // Média usada como referência: só sites comparáveis entre si (mesmo nº de livros)
    const complete = comparisons.filter(c => c.hasAllBooks);
    const reference = complete.length > 0 ? complete : comparisons;
    const avg = reference.length > 0
      ? reference.reduce((acc, c) => acc + c.totalPrice, 0) / reference.length
      : 0;

    return comparisons.map(c => {
      const queries = byProvider.get(c.providerName) ?? [];
      const times = queries.map(q => q.responseTimeMs).filter(t => t > 0);

      return {
        providerId: c.providerId || queries[0]?.providerId || 0,
        providerName: c.providerName,
        providerUrl: c.providerUrl || queries[0]?.providerUrl || '',
        link: this.providerLink(c, queries),
        total: c.totalPrice,
        booksFound: c.booksFound,
        totalBooks: c.totalBooksRequested,
        hasAllBooks: c.hasAllBooks,
        items: (c.bookPrices ?? []).map(b => ({
          isbn: b.isbn,
          title: b.title,
          price: b.price,
          quantity: b.quantity,
          productUrl: b.productUrl,
          imageUrl: b.imageUrl,
        })),
        avgResponseMs: times.length > 0 ? Math.round(times.reduce((a, b) => a + b, 0) / times.length) : 0,
        fromCache: queries.length > 0 && queries.every(q => q.fromCache),
        // Só compara com a média quem faz parte do conjunto de referência
        vsAverage: avg > 0 && (complete.length === 0 || c.hasAllBooks)
          ? ((avg - c.totalPrice) / avg) * 100
          : 0,
        // Pelo domínio, não pelo nome: o nome do provider pode mudar no cadastro.
        isAmazon: (c.providerUrl || queries[0]?.providerUrl || '').includes('amazon.com.br'),
        priceRank: 0,
        overBest: 0,
      };
    });
  }

  /**
   * Posição real de cada oferta por preço, e o quanto ela custa a mais que a
   * melhor. O card da Amazon é fixado no topo da lista; sem estes dois números
   * o destaque viraria uma promessa falsa de "segunda mais barata".
   */
  private rankOffers(): void {
    const ordenadas = [...this.allOffers]
      .filter(o => o.total > 0)
      .sort((a, b) => a.total - b.total);

    const menor = ordenadas[0]?.total ?? 0;

    ordenadas.forEach((o, i) => {
      o.priceRank = i + 1;
      o.overBest = menor > 0 ? o.total - menor : 0;
    });
  }

  /** A oferta da Amazon, se ela respondeu com preço nesta busca. */
  get amazonOffer(): Offer | null {
    return this.allOffers.find(o => o.isAmazon && o.total > 0) ?? null;
  }

  /**
   * Traz a Amazon para o topo: primeiro lugar quando é a mais barata, segundo
   * quando não é. Só mexe em quem sobreviveu aos filtros — se a pessoa filtrou
   * a Amazon para fora, ela fica fora.
   */
  private pinAmazon(list: Offer[]): Offer[] {
    const i = list.findIndex(o => o.isAmazon);
    if (i < 0) return list;

    const amazon = list[i];
    if (amazon.total <= 0) return list;

    const resto = [...list.slice(0, i), ...list.slice(i + 1)];
    const posicao = amazon.priceRank === 1 ? 0 : 1;
    resto.splice(posicao, 0, amazon);
    return resto;
  }

  private providerLink(c: ProviderComparison, queries: ProviderQueryDetail[]): string {
    return (
      c.bookPrices?.find(b => b.productUrl)?.productUrl ||
      queries.find(q => q.productUrl)?.productUrl ||
      c.providerUrl ||
      queries[0]?.providerUrl ||
      '#'
    );
  }

  get offers(): Offer[] {
    let list = this.allOffers;

    if (this.onlyComplete) {
      list = list.filter(o => o.hasAllBooks);
    }

    const q = this.providerFilter.trim().toLowerCase();
    if (q) {
      list = list.filter(o => o.providerName.toLowerCase().includes(q));
    }

    if (this.priceCeiling != null) {
      list = list.filter(o => o.total <= this.priceCeiling!);
    }

    const ordenada = [...list].sort((a, b) => {
      switch (this.sortKey) {
        case 'price':
          // Sites com o carrinho completo vêm primeiro; entre iguais, o mais barato
          if (a.hasAllBooks !== b.hasAllBooks) return a.hasAllBooks ? -1 : 1;
          return a.total - b.total;
        case 'price-desc':
          if (a.hasAllBooks !== b.hasAllBooks) return a.hasAllBooks ? -1 : 1;
          return b.total - a.total;
      }
    });

    return this.pinAmazon(ordenada);
  }

  get bestOffer(): Offer | null {
    return this.cachedBestOffer;
  }

  /**
   * Capa do livro buscado. Vem no mesmo JSON de preço das lojas, então basta
   * pegar a primeira que chegou — todas apontam para o mesmo arquivo no
   * static.cedet.com.br, porque o id do produto é global entre as lojas.
   */
  get coverUrl(): string | null {
    for (const offer of this.allOffers) {
      for (const item of offer.items) {
        if (item.imageUrl) return item.imageUrl;
      }
    }
    return null;
  }

  /** Durante a busca, a capa sai da primeira oferta parcial que trouxe uma. */
  get partialCoverUrl(): string | null {
    return this.partialOffers.find(o => o.imageUrl)?.imageUrl ?? null;
  }

  get partialBestPrice(): number | null {
    return this.partialOffers.length > 0 ? this.partialOffers[0].price : null;
  }

  isBest(o: Offer): boolean {
    return this.cachedBestOffer?.providerName === o.providerName;
  }

  get hasOffers(): boolean {
    return this.allOffers.length > 0;
  }

  /** Total antes dos filtros do rail, para o contador "N de M ofertas". */
  get allOffersCount(): number {
    return this.allOffers.length;
  }

  // ===== Métricas =====

  get priceMin(): number | null {
    const t = this.comparableTotals;
    return t.length > 0 ? t[0] : null;
  }

  get priceMax(): number | null {
    const t = this.comparableTotals;
    return t.length > 0 ? t[t.length - 1] : null;
  }

  get priceAvg(): number | null {
    const t = this.comparableTotals;
    return t.length > 0 ? t.reduce((a, b) => a + b, 0) / t.length : null;
  }

  get priceMedian(): number | null {
    const t = this.comparableTotals;
    if (t.length === 0) return null;
    const mid = Math.floor(t.length / 2);
    return t.length % 2 === 0 ? (t[mid - 1] + t[mid]) / 2 : t[mid];
  }

  /** Quanto se economiza pegando o melhor em vez do mais caro. */
  get savings(): number {
    const min = this.priceMin;
    const max = this.priceMax;
    return min != null && max != null ? max - min : 0;
  }

  get savingsPercent(): number {
    const max = this.priceMax;
    return max && max > 0 ? (this.savings / max) * 100 : 0;
  }

  get totalQueried(): number {
    return this.providerUrls.length;
  }

  private get queries(): ProviderQueryDetail[] {
    return this.result?.providerQueries ?? [];
  }

  get queryCount(): number {
    return this.queries.length;
  }

  /** Sites que responderam sem erro (podem não ter o livro). */
  get respondedCount(): number {
    return new Set(this.queries.filter(q => q.success).map(q => q.providerName)).size;
  }

  get withResultCount(): number {
    return new Set(this.queries.filter(q => q.hasResult).map(q => q.providerName)).size;
  }

  get failedCount(): number {
    const responded = new Set(this.queries.filter(q => q.success).map(q => q.providerName));
    return new Set(
      this.queries.filter(q => !q.success && !responded.has(q.providerName)).map(q => q.providerName),
    ).size;
  }

  get successRate(): number {
    const total = this.queries.length;
    if (total === 0) return 0;
    return (this.queries.filter(q => q.success).length / total) * 100;
  }

  get avgResponseMs(): number {
    const times = this.queries.map(q => q.responseTimeMs).filter(t => t > 0);
    if (times.length === 0) return 0;
    return Math.round(times.reduce((a, b) => a + b, 0) / times.length);
  }

  get cachedQueries(): number {
    return this.queries.filter(q => q.fromCache).length;
  }

  // ===== Por livro =====

  get bookStats(): BookStats[] {
    return this.cachedBookStats;
  }

  private buildBookStats(): BookStats[] {
    return this.books.map(b => {
      const found = this.queries.filter(
        q => q.isbn === b.isbn && q.hasResult && q.price != null && q.price > 0,
      );
      const prices = found.map(q => q.price!).sort((x, y) => x - y);

      return {
        isbn: b.isbn,
        title: found.find(q => q.title)?.title ?? '—',
        foundIn: found.length,
        min: prices[0] ?? 0,
        avg: prices.length > 0 ? prices.reduce((x, y) => x + y, 0) / prices.length : 0,
        max: prices[prices.length - 1] ?? 0,
      };
    });
  }

  get notFoundIsbns(): string[] {
    return this.result?.booksNotFound ?? [];
  }

  // ===== Diagnóstico =====

  get diagnosticRows(): ProviderQueryDetail[] {
    const rows = this.onlyFailures ? this.queries.filter(q => !q.hasResult) : this.queries;
    return [...rows].sort((a, b) => {
      if (a.isbn !== b.isbn) return a.isbn.localeCompare(b.isbn);
      // Quem achou o livro primeiro, depois por tempo de resposta
      if (a.hasResult !== b.hasResult) return a.hasResult ? -1 : 1;
      return a.responseTimeMs - b.responseTimeMs;
    });
  }

  toggleDiagnostics(): void {
    this.diagnosticsOpen = !this.diagnosticsOpen;
  }

  queryStatus(q: ProviderQueryDetail): 'ok' | 'empty' | 'fail' {
    if (q.hasResult) return 'ok';
    return q.success ? 'empty' : 'fail';
  }

  // ===== Filtro de preço =====

  get priceSliderMax(): number {
    const t = this.comparableTotals;
    return t.length > 0 ? Math.ceil(t[t.length - 1]) : 0;
  }

  get priceSliderMin(): number {
    const t = this.comparableTotals;
    return t.length > 0 ? Math.floor(t[0]) : 0;
  }

  get effectiveCeiling(): number {
    return this.priceCeiling ?? this.priceSliderMax;
  }

  onCeilingChange(value: string): void {
    const n = Number(value);
    this.priceCeiling = n >= this.priceSliderMax ? null : n;
  }

  clearFilters(): void {
    this.onlyComplete = false;
    this.providerFilter = '';
    this.priceCeiling = null;
    this.sortKey = 'price';
  }

  get hasActiveFilters(): boolean {
    return this.onlyComplete || this.providerFilter.trim().length > 0 || this.priceCeiling != null;
  }

  // ===== Helpers =====

  formatPrice(value: number | null | undefined): string {
    if (value == null) return '—';
    return `R$ ${value.toFixed(2)}`;
  }

  formatMs(ms: number): string {
    if (!ms) return '—';
    return ms >= 1000 ? `${(ms / 1000).toFixed(1)}s` : `${ms}ms`;
  }

  get searchLabel(): string {
    return this.books.map(b => (b.quantity > 1 ? `${b.isbn} (${b.quantity}x)` : b.isbn)).join(' · ');
  }

  get isMultiBook(): boolean {
    return this.books.length > 1;
  }

  trackByName(_: number, o: Offer): string {
    return o.providerName;
  }
}
