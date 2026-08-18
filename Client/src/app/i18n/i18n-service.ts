import { Injectable } from '@angular/core';
import { EN } from './translations';

export type Lang = 'pt' | 'en';

const STORAGE_KEY = 'sherlock.lang';

/**
 * Tradução em runtime, sem dependência externa.
 *
 * O idioma é lido do localStorage no bootstrap e fica fixo durante a sessão da
 * página — por isso o pipe pode ser puro (sem custo por ciclo de detecção).
 * Trocar de idioma recarrega a página.
 */
@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly lang: Lang;

  constructor() {
    this.lang = localStorage.getItem(STORAGE_KEY) === 'en' ? 'en' : 'pt';
    document.documentElement.lang = this.lang === 'en' ? 'en' : 'pt-BR';
  }

  /** Traduz usando o próprio texto em português como chave. */
  t(text: string): string {
    if (this.lang === 'pt') return text;
    return EN[text] ?? EN[text.trim()] ?? text;
  }

  toggle(): void {
    localStorage.setItem(STORAGE_KEY, this.lang === 'en' ? 'pt' : 'en');
    location.reload();
  }
}
