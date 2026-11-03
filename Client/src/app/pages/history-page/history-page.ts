import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { PluralPipe } from '../../i18n/plural-pipe';
import { AuthRequiredComponent } from '../../components/auth-required/auth-required';
import { AuthService } from '../../services/auth-service';
import { SearchStateService } from '../../services/search-state';
import {
  TransactionService,
  TransactionHistory,
  TransactionDetail,
} from '../../services/transaction-service';

interface ParsedInput {
  type: 'cart' | 'single' | 'unknown';
  isbn: string | null;
  description: string;
}

/** Um dia do histórico: cabeçalho ("Hoje", "Ontem", data) e as consultas dele. */
interface DayGroup {
  key: string;
  label: string;
  items: TransactionHistory[];
}

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [TranslatePipe, PluralPipe, CommonModule, DatePipe, AuthRequiredComponent],
  templateUrl: './history-page.html',
  styleUrl: './history-page.scss',
})
export class HistoryPage implements OnInit {
  transactions: TransactionHistory[] = [];
  groups: DayGroup[] = [];
  isLoading = false;
  errorMessage = '';

  // Modal de detalhes
  detail: TransactionDetail | null = null;
  detailLoading = false;

  /** Capas que o CDN não serviu: o espaço cinza fica no lugar, sem ícone quebrado. */
  readonly brokenCovers = new Set<number>();

  /** Histórico é da pessoa. Sem sessão não há o que mostrar nem o que pedir. */
  isAuthenticated = false;

  constructor(
    private service: TransactionService,
    private auth: AuthService,
    private router: Router,
    private state: SearchStateService,
  ) {}

  ngOnInit(): void {
    this.isAuthenticated = this.auth.isAuthenticated();

    // Sem a guarda, a tela abria e disparava a chamada só para levar 401 — e a
    // pessoa via "Erro ao carregar histórico" onde devia ver um convite a entrar.
    if (this.isAuthenticated) {
      this.load();
    }
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.service.listMyHistory(50).subscribe({
      next: list => {
        this.transactions = list;
        this.groups = this.groupByDay(list);
        this.isLoading = false;

        // Consulta em que nenhuma loja respondeu título: pergunta ao catálogo
        const semTitulo = list
          .filter(t => !t.bestTitle)
          .map(t => this.parseInput(t.inputParameters).isbn)
          .filter((i): i is string => !!i);
        if (semTitulo.length > 0) this.state.ensureKnown(semTitulo).subscribe();
      },
      error: err => {
        this.errorMessage = err?.error?.error || 'Erro ao carregar histórico.';
        this.isLoading = false;
      },
    });
  }

  /** Qual detalhe a pessoa pediu por último: resposta de um pedido anterior é descartada. */
  private detailRequest = 0;

  openDetail(t: TransactionHistory): void {
    const request = ++this.detailRequest;
    this.detail = null;
    this.detailLoading = true;
    this.service.getDetail(t.id).subscribe({
      next: d => {
        if (request !== this.detailRequest) return;
        this.detail = d;
        this.detailLoading = false;
      },
      error: () => {
        if (request !== this.detailRequest) return;
        this.detailLoading = false;
      },
    });
  }

  goSearch(): void {
    this.router.navigate(['/search']);
  }

  closeDetail(): void {
    this.detailRequest++;
    this.detail = null;
    this.detailLoading = false;
  }

  /**
   * Refaz a consulta na tela de resultado. Se a mesma busca já foi feita nesta
   * sessão, a tela reaproveita o resultado guardado; senão consulta de novo, e
   * isso custa crédito — por isso o botão diz "Buscar de novo", não "Ver".
   */
  searchAgain(t: TransactionHistory): void {
    const isbn = this.parseInput(t.inputParameters).isbn;
    if (!isbn) return;
    this.router.navigate(['/resultado'], { queryParams: { isbn } });
  }

  // Diferencia carrinho de consulta unitária. Backend salva { isbn, isCart } por transação.
  // Mesmo no carrinho, cada livro vira uma transação separada — o flag isCart indica o contexto.
  parseInput(json: string): ParsedInput {
    if (!json) return { type: 'unknown', isbn: null, description: '—' };

    try {
      const data = JSON.parse(json);

      if (data?.isbn) {
        return {
          type: data.isCart ? 'cart' : 'single',
          isbn: String(data.isbn),
          description: `ISBN ${data.isbn}`,
        };
      }
      if (data?.title) {
        return { type: 'single', isbn: null, description: data.title };
      }

      return { type: 'unknown', isbn: null, description: '—' };
    } catch {
      return { type: 'unknown', isbn: null, description: '—' };
    }
  }

  /** Título para o cartão: o que as lojas devolveram; senão o que a sessão ou o catálogo sabem; o ISBN só em último caso. */
  titleOf(t: TransactionHistory): string {
    if (t.bestTitle) return t.bestTitle;
    const parsed = this.parseInput(t.inputParameters);
    return (parsed.isbn && this.state.infoOf(parsed.isbn)?.title) || parsed.description;
  }

  authorOf(t: TransactionHistory): string | null {
    if (t.bestAuthor) return t.bestAuthor;
    const isbn = this.parseInput(t.inputParameters).isbn;
    return (isbn && this.state.infoOf(isbn)?.author) || null;
  }

  coverOf(t: TransactionHistory): string | null {
    if (t.imageUrl) return t.imageUrl;
    const isbn = this.parseInput(t.inputParameters).isbn;
    return (isbn && this.state.infoOf(isbn)?.thumbUrl) || null;
  }

  formatPrice(price?: number | null): string {
    if (price == null) return '—';
    return `R$ ${price.toFixed(2)}`;
  }

  /**
   * Agrupa por dia local. A lista já vem ordenada da mais recente para a mais
   * antiga, então basta cortar onde o dia muda.
   */
  private groupByDay(list: TransactionHistory[]): DayGroup[] {
    const groups: DayGroup[] = [];
    const today = this.dayKey(new Date());
    const yesterday = this.dayKey(new Date(Date.now() - 86_400_000));

    for (const t of list) {
      const date = new Date(t.startedAt);
      const key = this.dayKey(date);
      let group = groups[groups.length - 1];

      if (!group || group.key !== key) {
        const label =
          key === today ? 'Hoje' :
          key === yesterday ? 'Ontem' :
          date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'long', year: 'numeric' });
        group = { key, label, items: [] };
        groups.push(group);
      }
      group.items.push(t);
    }

    return groups;
  }

  private dayKey(d: Date): string {
    return `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
  }
}
