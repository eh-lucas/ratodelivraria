import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { WatchedService, WatchedBook, WatchFrequency } from '../../services/watched-service';

@Component({
  selector: 'app-watched-page',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './watched-page.html',
  styleUrl: './watched-page.scss',
})
export class WatchedPage implements OnInit, OnDestroy {
  items: WatchedBook[] = [];

  // Form
  showForm = false;
  newIsbn = '';
  newTargetPrice: number | null = null;
  newFrequency: WatchFrequency = 'daily';
  formError = '';

  frequencies: { value: WatchFrequency; label: string }[] = [
    { value: 'daily', label: '1× ao dia' },
    { value: 'every_2_days', label: 'a cada 2 dias' },
    { value: 'weekly', label: '1× por semana' },
    { value: 'monthly', label: '1× por mês' },
  ];

  private sub?: Subscription;

  constructor(private service: WatchedService) {}

  ngOnInit(): void {
    this.sub = this.service.items$.subscribe(list => (this.items = list));
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  openForm(): void {
    this.showForm = true;
    this.formError = '';
    this.newIsbn = '';
    this.newTargetPrice = null;
    this.newFrequency = 'daily';
  }

  cancelForm(): void {
    this.showForm = false;
    this.formError = '';
  }

  submit(): void {
    const isbn = this.newIsbn.trim();
    if (!isbn) {
      this.formError = 'Informe o ISBN.';
      return;
    }
    if (this.newTargetPrice == null || this.newTargetPrice <= 0) {
      this.formError = 'Informe um preço-alvo válido.';
      return;
    }
    if (this.items.some(i => i.isbn === isbn)) {
      this.formError = 'Este ISBN já está sendo observado.';
      return;
    }

    this.service.add({
      isbn,
      targetPrice: this.newTargetPrice,
      frequency: this.newFrequency,
    });
    this.showForm = false;
  }

  remove(id: string): void {
    if (confirm('Remover este livro da lista de observados?')) {
      this.service.remove(id);
    }
  }

  togglePause(id: string): void {
    this.service.togglePause(id);
  }

  frequencyLabel(f: WatchFrequency): string {
    return WatchedService.frequencyLabel(f);
  }

  formatPrice(v?: number): string {
    return v == null ? '—' : `R$ ${v.toFixed(2)}`;
  }
}
