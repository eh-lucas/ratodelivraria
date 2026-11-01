import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { WatchedService, WatchedBook, WatchRule } from '../../services/watched-service';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { AuthRequiredComponent } from '../../components/auth-required/auth-required';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-watched-page',
  standalone: true,
  imports: [TranslatePipe, CommonModule, FormsModule, DatePipe, AuthRequiredComponent],
  templateUrl: './watched-page.html',
  styleUrl: './watched-page.scss',
})
export class WatchedPage implements OnInit, OnDestroy {
  /**
   * Observar livros depende de conta: a lista é de uma pessoa, e o aviso de preço
   * vai para o e-mail dela. Sem sessão, a tela convida a entrar em vez de mostrar
   * uma lista que não pertence a ninguém.
   */
  isAuthenticated = false;

  readonly Rule = WatchRule;

  items: WatchedBook[] = [];
  loading = false;
  loadError = '';

  // Form
  showForm = false;
  newIsbn = '';
  newRule: WatchRule = WatchRule.PercentDrop;
  newTargetPrice: number | null = null;
  newDropPercent = 15;
  formError = '';
  saving = false;

  private sub?: Subscription;

  constructor(private service: WatchedService, private auth: AuthService) {}

  ngOnInit(): void {
    this.isAuthenticated = this.auth.isAuthenticated();
    this.sub = this.service.items$.subscribe(list => (this.items = list));

    if (this.isAuthenticated) this.recarregar();
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private recarregar(): void {
    this.loading = true;
    this.loadError = '';
    this.service.load().subscribe({
      next: () => (this.loading = false),
      error: () => {
        this.loading = false;
        this.loadError = 'Não foi possível carregar sua lista. Tente novamente.';
      },
    });
  }

  openForm(): void {
    this.showForm = true;
    this.formError = '';
    this.newIsbn = '';
    this.newRule = WatchRule.PercentDrop;
    this.newTargetPrice = null;
    this.newDropPercent = 15;
  }

  cancelForm(): void {
    this.showForm = false;
    this.formError = '';
  }

  submit(): void {
    const isbn = this.newIsbn.replace(/\D/g, '');

    if (isbn.length !== 10 && isbn.length !== 13) {
      this.formError = 'Informe um ISBN de 10 ou 13 dígitos.';
      return;
    }
    if (this.newRule === WatchRule.TargetPrice && (this.newTargetPrice == null || this.newTargetPrice <= 0)) {
      this.formError = 'Informe um preço-alvo válido.';
      return;
    }

    this.saving = true;
    this.service.add([{
      isbn,
      rule: this.newRule,
      targetPrice: this.newRule === WatchRule.TargetPrice ? this.newTargetPrice! : undefined,
      dropPercent: this.newDropPercent,
    }]).subscribe({
      next: resultado => {
        this.saving = false;
        // Recusa parcial vem com motivo do servidor — teto de 20 atingido, ISBN
        // inválido. Mostrar o motivo dele é melhor que inventar um aqui.
        const recusa = resultado.rejected[0];
        if (recusa) {
          this.formError = recusa.reason;
          return;
        }
        this.showForm = false;
      },
      error: () => {
        this.saving = false;
        this.formError = 'Não foi possível salvar. Tente novamente.';
      },
    });
  }

  remove(item: WatchedBook): void {
    if (!confirm(`Remover "${item.title}" da lista de observados?`)) return;
    this.service.remove(item.id).subscribe();
  }

  togglePause(item: WatchedBook): void {
    this.service.togglePause(item).subscribe();
  }

  ruleLabel(w: WatchedBook): string {
    return WatchedService.ruleLabel(w);
  }

  formatPrice(v?: number | null): string {
    return v == null ? '—' : `R$ ${v.toFixed(2)}`;
  }

  /**
   * A variação desde a varredura anterior, em reais. Null quando ainda não há
   * dois pontos para comparar — e aí a tela não desenha seta nenhuma, em vez de
   * desenhar uma seta de zero que pareceria "não mudou".
   */
  delta(w: WatchedBook): number | null {
    if (w.lastPrice == null || w.previousPrice == null) return null;
    return w.lastPrice - w.previousPrice;
  }

  /** O preço de hoje empata com o melhor do mês? É isso que faz a queda valer. */
  isMelhorDoMes(w: WatchedBook): boolean {
    return w.lastPrice != null && w.lowestPrice30d != null && w.lastPrice <= w.lowestPrice30d;
  }
}
