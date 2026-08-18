import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UserService, UserCredits, CreditTransaction, PagedResult } from '../../services/user-service';
import { TranslatePipe } from '../../i18n/translate-pipe';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [TranslatePipe, CommonModule, RouterLink],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss'
})
export class ProfilePage implements OnInit {
  userCredits: UserCredits | null = null;
  creditHistory: CreditTransaction[] = [];
  loading = true;
  historyLoading = false;
  error: string | null = null;

  // Paginação
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  hasNextPage = false;
  hasPreviousPage = false;

  constructor(
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.loadUserData();
    this.loadCreditHistory();
  }

  loadUserData(): void {
    this.loading = true;
    this.error = null;

    this.userService.getCurrentUser().subscribe({
      next: (credits) => {
        this.userCredits = credits;
        this.loading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar dados do usuário:', err);
        this.error = 'Não foi possível carregar os dados do usuário.';
        this.loading = false;
      }
    });
  }

  loadCreditHistory(): void {
    this.historyLoading = true;

    this.userService.getCreditHistory(this.currentPage, this.pageSize).subscribe({
      next: (result: PagedResult<CreditTransaction>) => {
        this.creditHistory = result.items;
        this.totalPages = result.totalPages;
        this.hasNextPage = result.hasNextPage;
        this.hasPreviousPage = result.hasPreviousPage;
        this.historyLoading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar histórico:', err);
        this.historyLoading = false;
      }
    });
  }

  nextPage(): void {
    if (this.hasNextPage) {
      this.currentPage++;
      this.loadCreditHistory();
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage) {
      this.currentPage--;
      this.loadCreditHistory();
    }
  }

  getTransactionTypeClass(type: string): string {
    switch (type) {
      case 'Purchase':
      case 'Bonus':
        return 'text-green-600';
      case 'Consumption':
        return 'text-red-600';
      case 'Refund':
        return 'text-blue-600';
      default:
        return 'text-gray-600';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
