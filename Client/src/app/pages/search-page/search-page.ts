import { Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CartService,
  CartBookItem,
  CartOptimizationResult,
  OptimizationStrategy,
  ProviderOption,
} from '../../services/cart-service';
import { UserService } from '../../services/user-service';

// Item da lista de progresso exibida no painel "Resultado" durante a consulta
interface ProgressItem {
  providerId: number;
  providerName: string;
  providerUrl: string;
  status: 'pending' | 'success' | 'not_found' | 'error';
  price?: number;
}

@Component({
  selector: 'search-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss',
})
export class SearchPage implements OnInit {
  // Carrinho de ISBNs a consultar
  books: CartBookItem[] = [];
  newIsbn = '';

  // Providers disponíveis e selecionados
  providers: ProviderOption[] = [];
  selectedProviderUrls: string[] = [];
  providersOpen = false;
  providerFilter = '';

  // Resultado / estado
  result: CartOptimizationResult | null = null;
  isLoading = false;
  errorMessage = '';

  // Progresso da consulta — populado ao iniciar busca, atualizado ao receber resposta
  progressItems: ProgressItem[] = [];

  @ViewChild('providersBox') providersBox?: ElementRef;

  constructor(
    private cartService: CartService,
    private userService: UserService,
  ) {}

  ngOnInit(): void {
    this.cartService.getActiveProviders().subscribe({
      next: list => {
        this.providers = list;
        // Por padrão consulta em todos os providers ativos
        this.selectedProviderUrls = list.map(p => p.url);
      },
      error: () => {
        this.errorMessage = 'Não foi possível carregar a lista de sites.';
      },
    });
  }

  // ===== Carrinho =====

  addBook(): void {
    const isbn = this.newIsbn.trim();
    if (!isbn) return;
    // Evita duplicatas
    if (this.books.some(b => b.isbn === isbn)) {
      this.newIsbn = '';
      return;
    }
    this.books.push({ isbn, quantity: 1 });
    this.newIsbn = '';
  }

  removeBook(isbn: string): void {
    this.books = this.books.filter(b => b.isbn !== isbn);
  }

  // ===== Providers dropdown =====

  toggleProviders(): void {
    this.providersOpen = !this.providersOpen;
  }

  toggleProvider(url: string): void {
    if (this.selectedProviderUrls.includes(url)) {
      this.selectedProviderUrls = this.selectedProviderUrls.filter(u => u !== url);
    } else {
      this.selectedProviderUrls = [...this.selectedProviderUrls, url];
    }
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

  // Fecha o dropdown ao clicar fora
  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (this.providersOpen && this.providersBox && !this.providersBox.nativeElement.contains(e.target)) {
      this.providersOpen = false;
    }
  }

  // ===== Custo estimado =====

  get estimatedCredits(): number {
    // Cada combinação livro × provider = 1 query = 1 crédito (estimativa)
    return this.books.length * this.selectedProviderUrls.length;
  }

  get canSearch(): boolean {
    return this.books.length > 0 && this.selectedProviderUrls.length > 0 && !this.isLoading;
  }

  // ===== Busca =====

  search(): void {
    if (!this.canSearch) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.result = null;

    // Mostra cada provider selecionado como "consultando..." enquanto a busca roda
    this.progressItems = this.providers
      .filter(p => this.selectedProviderUrls.includes(p.url))
      .map(p => ({ providerId: p.id, providerName: p.name, providerUrl: p.url, status: 'pending' as const }));

    this.cartService
      .optimizeCart({
        books: this.books,
        strategy: OptimizationStrategy.LowestTotal,
        maxProviders: 0,
        includeShipping: true,
        providerUrls: this.selectedProviderUrls,
      })
      .subscribe({
        next: r => {
          this.applyProgressFromResult(r);
          this.result = r;
          this.isLoading = false;
          if (r.creditsUsed) this.userService.updateCreditsAfterConsumption(r.creditsUsed);
        },
        error: err => {
          // Em caso de falha geral, marca todos como erro
          this.progressItems = this.progressItems.map(p => ({ ...p, status: 'error' }));
          this.errorMessage = err?.error?.message || 'Erro ao consultar livros.';
          this.isLoading = false;
        },
      });
  }

  // Resolve o status de cada provider da lista de progresso a partir do resultado agregado.
  // Comparamos por providerName porque o backend retorna providerId=0 / providerUrl="" em providerComparisons.
  private applyProgressFromResult(r: CartOptimizationResult): void {
    const byName = new Map(r.providerComparisons.map(c => [c.providerName, c]));
    this.progressItems = this.progressItems.map(item => {
      const cmp = byName.get(item.providerName);
      if (!cmp) {
        return { ...item, status: 'error' };
      }
      if (cmp.booksFound === 0) {
        return { ...item, status: 'not_found' };
      }
      return {
        ...item,
        status: 'success',
        price: cmp.totalPrice,
      };
    });
  }

  // ===== Helpers de exibição =====

  trackByIsbn(_: number, item: CartBookItem): string {
    return item.isbn;
  }

  get topProviders() {
    if (!this.result) return [];
    // Já vem ordenado pelo backend; pega top 5 que têm todos os livros
    return this.result.providerComparisons.slice(0, 5);
  }

  get bestProvider() {
    return this.result?.providerComparisons.find(p => p.hasAllBooks) ?? null;
  }
}
