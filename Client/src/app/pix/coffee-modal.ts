import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as QRCode from 'qrcode';
import { environment } from '../../environments/environment';
import { buildPixPayload } from './pix-payload';
import { TranslatePipe } from '../i18n/translate-pipe';

const PRESET_AMOUNTS = [10, 30, 50];

@Component({
  selector: 'app-coffee-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './coffee-modal.html',
  styleUrl: './coffee-modal.scss',
})
export class CoffeeModalComponent implements OnInit {
  @Output() close = new EventEmitter<void>();

  readonly presets = PRESET_AMOUNTS;
  selected: number | null = PRESET_AMOUNTS[1];
  customAmount = '';
  payload = '';
  qrDataUrl = '';
  copied = false;

  ngOnInit(): void {
    void this.regenerate();
  }

  /** Valor efetivo; null = doador escolhe o valor no app do banco. */
  get amount(): number | null {
    if (this.selected !== null) return this.selected;
    const parsed = parseFloat(this.customAmount.replace(',', '.'));
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  selectPreset(value: number): void {
    this.selected = value;
    this.customAmount = '';
    void this.regenerate();
  }

  onCustomInput(): void {
    this.selected = null;
    void this.regenerate();
  }

  async regenerate(): Promise<void> {
    const amount = this.amount;
    this.payload = buildPixPayload({
      key: environment.pix.key,
      name: environment.pix.name,
      city: environment.pix.city,
      amount: amount ?? undefined,
    });
    this.qrDataUrl = await QRCode.toDataURL(this.payload, {
      margin: 1,
      width: 260,
      errorCorrectionLevel: 'M',
    });
    this.copied = false;
  }

  async copyPayload(): Promise<void> {
    await navigator.clipboard.writeText(this.payload);
    this.copied = true;
    setTimeout(() => (this.copied = false), 2500);
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.close.emit();
  }
}
