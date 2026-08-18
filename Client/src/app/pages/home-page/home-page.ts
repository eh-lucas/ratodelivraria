import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { CartService } from '../../services/cart-service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [TranslatePipe, RouterLink],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage implements OnInit {
  /** Quantidade real de livrarias consultadas. */
  providerCount = 0;

  /**
   * Arredondado para baixo na dezena ("mais de 60"): o número exato muda quando
   * lojas saem do ar, e uma promessa redonda envelhece melhor que uma exata.
   */
  get providerCountRounded(): number {
    return Math.floor(this.providerCount / 10) * 10;
  }

  /** Observar livros depende de conta; na release pública fica marcado como "em breve". */
  readonly watchedAvailable = !environment.demoMode;

  constructor(private cartService: CartService) {}

  ngOnInit(): void {
    this.cartService.getActiveProviders().subscribe({
      next: list => (this.providerCount = list.length),
      error: () => (this.providerCount = 0),
    });
  }
}
