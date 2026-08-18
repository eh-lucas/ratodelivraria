import { Injectable } from '@angular/core';
import { CartBookItem, CartOptimizationResult } from './cart-service';

/**
 * Estado compartilhado entre a tela de busca e a tela de resultado.
 *
 * A tela de resultado é quem dispara a consulta (para funcionar também quando
 * aberta direto pela URL), mas o resultado fica guardado aqui para que voltar
 * e avançar no navegador não gaste créditos de novo. Persistimos em
 * sessionStorage para sobreviver a um F5 — buscar tem custo, repetir sem o
 * usuário pedir seria cobrar duas vezes pela mesma coisa.
 */
export interface SearchSnapshot {
  signature: string;
  books: CartBookItem[];
  providerUrls: string[];
  providerNames: Record<string, string>;
  result: CartOptimizationResult;
  completedAt: number;
}

const STORAGE_KEY = 'sherlock.search.lastResult';

@Injectable({ providedIn: 'root' })
export class SearchStateService {
  /** Seleção de sites feita na tela de busca; a de resultado reaproveita. */
  selectedProviderUrls: string[] = [];

  /** Nome por URL, para exibir sites que falharam (o resultado só traz os que acharam preço). */
  providerNames: Record<string, string> = {};

  private snapshot: SearchSnapshot | null = null;

  constructor() {
    this.restore();
  }

  /** Assinatura da consulta: mesmos livros + mesmos sites = mesmo resultado. */
  buildSignature(books: CartBookItem[], providerUrls: string[]): string {
    const b = books
      .map(x => `${x.isbn}:${x.quantity}`)
      .sort()
      .join(',');
    const p = [...providerUrls].sort().join(',');
    return `${b}|${p}`;
  }

  get(signature: string): SearchSnapshot | null {
    return this.snapshot?.signature === signature ? this.snapshot : null;
  }

  save(snapshot: SearchSnapshot): void {
    this.snapshot = snapshot;
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(snapshot));
    } catch {
      // Cota estourada ou storage indisponível: seguimos só com o cache em memória
    }
  }

  clear(): void {
    this.snapshot = null;
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignora */
    }
  }

  private restore(): void {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as SearchSnapshot;
      if (parsed?.signature && parsed?.result) {
        this.snapshot = parsed;
        this.providerNames = parsed.providerNames ?? {};
        this.selectedProviderUrls = parsed.providerUrls ?? [];
      }
    } catch {
      /* snapshot corrompido: ignora */
    }
  }
}
