import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

// Frequências disponíveis até o backend definir o catálogo final
export type WatchFrequency = 'daily' | 'every_2_days' | 'weekly' | 'monthly';

export interface WatchedBook {
  id: string;
  isbn: string;
  targetPrice: number;
  frequency: WatchFrequency;
  createdAt: string;
  paused: boolean;
  lastCheckedAt?: string;
  lastPrice?: number;
}

const STORAGE_KEY = 'sherlock.watched.v1';

// Mock local; será substituído por backend quando os endpoints existirem.
@Injectable({ providedIn: 'root' })
export class WatchedService {
  private subject = new BehaviorSubject<WatchedBook[]>(this.load());
  public items$: Observable<WatchedBook[]> = this.subject.asObservable();

  private load(): WatchedBook[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as WatchedBook[]) : [];
    } catch {
      return [];
    }
  }

  private persist(items: WatchedBook[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
    this.subject.next(items);
  }

  list(): WatchedBook[] {
    return this.subject.value;
  }

  add(input: { isbn: string; targetPrice: number; frequency: WatchFrequency }): WatchedBook {
    const item: WatchedBook = {
      // Id local; quando vier do backend usaremos o id real
      id: crypto.randomUUID(),
      isbn: input.isbn.trim(),
      targetPrice: input.targetPrice,
      frequency: input.frequency,
      createdAt: new Date().toISOString(),
      paused: false,
    };
    this.persist([item, ...this.subject.value]);
    return item;
  }

  remove(id: string): void {
    this.persist(this.subject.value.filter(w => w.id !== id));
  }

  togglePause(id: string): void {
    this.persist(this.subject.value.map(w => w.id === id ? { ...w, paused: !w.paused } : w));
  }

  static frequencyLabel(f: WatchFrequency): string {
    switch (f) {
      case 'daily': return '1× ao dia';
      case 'every_2_days': return 'a cada 2 dias';
      case 'weekly': return '1× por semana';
      case 'monthly': return '1× por mês';
    }
  }
}
