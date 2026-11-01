import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../i18n/translate-pipe';

/**
 * Tela de "entre para continuar", usada onde o conteúdo é de uma pessoa só.
 *
 * É componente e não um guard porque o site é aberto: mandar o visitante para
 * /login no meio da navegação esconderia que a página existe. Aqui ele vê o que
 * tem do outro lado e decide.
 */
@Component({
  selector: 'app-auth-required',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="auth-required">
      <img src="/assets/rato.png" alt="" class="mascote" aria-hidden="true" />
      <h1 class="title">{{ title | t }}</h1>
      <p class="text">{{ text | t }}</p>
      <div class="actions">
        <a routerLink="/login" class="primary">{{ 'Entrar' | t }}</a>
        <a routerLink="/register" class="secondary">{{ 'Criar conta' | t }}</a>
      </div>
    </div>
  `,
  styleUrl: './auth-required.scss',
})
export class AuthRequiredComponent {
  @Input() title = 'Entre para ver esta página';
  @Input() text = 'É preciso ter uma conta para continuar.';
}
