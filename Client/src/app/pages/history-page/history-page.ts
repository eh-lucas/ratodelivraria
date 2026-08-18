import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TranslatePipe } from '../../i18n/translate-pipe';
import {
  TransactionService,
  TransactionHistory,
  TransactionDetail,
} from '../../services/transaction-service';

interface ParsedInput {
  type: 'cart' | 'single' | 'unknown';
  description: string;
  isbns: string[];
}

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [TranslatePipe, CommonModule, DatePipe],
  templateUrl: './history-page.html',
  styleUrl: './history-page.scss',
})
export class HistoryPage implements OnInit {
  transactions: TransactionHistory[] = [];
  isLoading = false;
  errorMessage = '';

  // Modal de detalhes
  detail: TransactionDetail | null = null;
  detailLoading = false;

  constructor(private service: TransactionService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.service.listMyHistory(50).subscribe({
      next: list => {
        this.transactions = list;
        this.isLoading = false;
      },
      error: err => {
        this.errorMessage = err?.error?.error || 'Erro ao carregar histórico.';
        this.isLoading = false;
      },
    });
  }

  openDetail(t: TransactionHistory): void {
    this.detail = null;
    this.detailLoading = true;
    this.service.getDetail(t.id).subscribe({
      next: d => {
        this.detail = d;
        this.detailLoading = false;
      },
      error: () => {
        this.detailLoading = false;
      },
    });
  }

  closeDetail(): void {
    this.detail = null;
  }

  // Diferencia carrinho de consulta unitária. Backend salva { isbn, isCart } por transação.
  // Mesmo no carrinho, cada livro vira uma transação separada — o flag isCart indica o contexto.
  parseInput(json: string): ParsedInput {
    if (!json) return { type: 'unknown', description: '—', isbns: [] };

    try {
      const data = JSON.parse(json);

      if (data?.isbn) {
        return {
          type: data.isCart ? 'cart' : 'single',
          description: `ISBN ${data.isbn}`,
          isbns: [data.isbn],
        };
      }
      if (data?.title) {
        return { type: 'single', description: data.title, isbns: [] };
      }

      return { type: 'unknown', description: '—', isbns: [] };
    } catch {
      return { type: 'unknown', description: '—', isbns: [] };
    }
  }

  formatPrice(price?: number): string {
    if (price == null) return '—';
    return `R$ ${price.toFixed(2)}`;
  }
}
