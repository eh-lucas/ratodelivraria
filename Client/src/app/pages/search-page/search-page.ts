import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CartService,
  CartBookItem,
  CartOptimizationRequest,
  CartOptimizationResult,
  OptimizationStrategy,
  StrategyOption,
  ProviderOption
} from '../../services/cart-service';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'search-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss'
})
export class SearchPage implements OnInit {
  // Lista de livros no carrinho
  books: CartBookItem[] = [];

  // Formulário de novo livro
  newBook: CartBookItem = {
    title: '',
    isbn: '',
    author: '',
    quantity: 1
  };

  // Configurações de otimização
  selectedStrategy: OptimizationStrategy = OptimizationStrategy.LowestTotal;
  maxProviders: number = 0;
  includeShipping: boolean = true;
  strategies: StrategyOption[] = [];

  // Providers
  providers: ProviderOption[] = [];
  selectedProviderUrls: string[] = [];

  // Resultado
  result: CartOptimizationResult | null = null;

  // Estado
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(
    private cartService: CartService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadStrategies();
    this.loadProviders();
  }

  loadStrategies(): void {
    this.cartService.getStrategies().subscribe({
      next: (strategies) => {
        this.strategies = strategies;
      },
      error: (err) => {
        console.error('Erro ao carregar estratégias', err);
        // Fallback para estratégias padrão
        this.strategies = [
          { value: 0, name: 'LowestTotal', description: 'Menor custo total (livros + frete)' },
          { value: 1, name: 'FewestOrders', description: 'Menor número de pedidos' },
          { value: 2, name: 'PrioritizeFreeShipping', description: 'Prioriza frete grátis' },
          { value: 3, name: 'SingleProvider', description: 'Comprar tudo em um único site' }
        ];
      }
    });
  }

  loadProviders(): void {
    this.cartService.getActiveProviders().subscribe({
      next: (providers) => {
        this.providers = providers;
        // Por padrão, seleciona todos os providers
        this.selectedProviderUrls = providers.map(p => p.url);
      },
      error: (err) => {
        console.error('Erro ao carregar providers', err);
      }
    });
  }

  toggleProvider(url: string): void {
    const index = this.selectedProviderUrls.indexOf(url);
    if (index > -1) {
      this.selectedProviderUrls.splice(index, 1);
    } else {
      this.selectedProviderUrls.push(url);
    }
  }

  isProviderSelected(url: string): boolean {
    return this.selectedProviderUrls.includes(url);
  }

  selectAllProviders(): void {
    this.selectedProviderUrls = this.providers.map(p => p.url);
  }

  deselectAllProviders(): void {
    this.selectedProviderUrls = [];
  }

  addBook(): void {
    if (!this.newBook.title.trim()) {
      this.errorMessage = 'Digite o título do livro';
      return;
    }

    this.books.push({
      title: this.newBook.title.trim(),
      isbn: this.newBook.isbn?.trim() || undefined,
      author: this.newBook.author?.trim() || undefined,
      quantity: this.newBook.quantity || 1
    });

    // Limpa formulário
    this.newBook = {
      title: '',
      isbn: '',
      author: '',
      quantity: 1
    };
    this.errorMessage = '';
  }

  removeBook(index: number): void {
    this.books.splice(index, 1);
  }

  clearCart(): void {
    this.books = [];
    this.result = null;
    this.errorMessage = '';
  }

  optimizeCart(): void {
    if (this.books.length === 0) {
      this.errorMessage = 'Adicione pelo menos um livro ao carrinho';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.result = null;

    const request: CartOptimizationRequest = {
      books: this.books,
      strategy: this.selectedStrategy,
      maxProviders: this.maxProviders,
      includeShipping: this.includeShipping
    };

    this.cartService.optimizeCart(request).subscribe({
      next: (result) => {
        this.result = result;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro na otimização', err);
        this.errorMessage = err.error?.error || 'Erro ao otimizar carrinho. Tente novamente.';
        this.isLoading = false;
      }
    });
  }

  quickSearch(): void {
    if (!this.newBook.title.trim()) {
      this.errorMessage = 'Digite o título do livro para buscar';
      return;
    }

    if (this.selectedProviderUrls.length === 0) {
      this.errorMessage = 'Selecione pelo menos um site para buscar';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.cartService.searchBook(
      this.newBook.title.trim(),
      this.newBook.isbn?.trim(),
      this.newBook.author?.trim(),
      this.selectedProviderUrls
    ).subscribe({
      next: (result) => {
        this.result = result;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro na busca', err);
        this.errorMessage = err.error?.error || 'Erro ao buscar livro. Tente novamente.';
        this.isLoading = false;
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  getStrategyName(value: number): string {
    const strategy = this.strategies.find(s => s.value === value);
    return strategy?.description || 'Menor custo total';
  }
}
