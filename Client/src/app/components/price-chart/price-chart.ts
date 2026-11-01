import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../i18n/translate-pipe';

export interface PricePoint {
  /** Dia em ISO (yyyy-MM-dd). */
  d: string;
  /** Menor preço daquele dia. */
  p: number;
}

interface Plotado {
  x: number;
  y: number;
  ponto: PricePoint;
}

/**
 * O preço de um livro ao longo do tempo, dentro de uma célula da tabela.
 *
 * Sparkline e não gráfico com eixos, porque o lugar define a forma: a linha mora
 * numa coluna de ~180px, ao lado das colunas que já trazem "Preço hoje", "Desde
 * ontem" e "Melhor em 30 dias". A linha mostra o FORMATO da variação; os números
 * continuam legíveis na própria tabela, então nenhum valor depende do tooltip.
 *
 * Série única: sem legenda, que o nome do livro está na mesma linha.
 *
 * O eixo Y não começa em zero, de propósito. Aqui a pergunta é "mudou quanto?", e
 * um livro que varia de R$ 60 a R$ 65 vira uma linha reta se o eixo for de 0 a
 * 65. Zero na base é regra para barras, onde a área codifica a grandeza; numa
 * linha de preço o que importa é a variação — e a tabela ao lado dá a escala
 * absoluta para a inclinação não enganar.
 */
@Component({
  selector: 'app-price-chart',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './price-chart.html',
  styleUrl: './price-chart.scss',
})
export class PriceChartComponent {
  @Input({ required: true }) set points(valor: PricePoint[] | null | undefined) {
    this._points.set((valor ?? []).filter(p => p && p.p > 0));
  }

  private readonly _points = signal<PricePoint[]>([]);

  // Coordenadas do viewBox. Os traços não escalam (non-scaling-stroke) e NÃO há
  // texto dentro do SVG: numa célula de ~180px, um texto de 10px no viewBox de
  // 320 sairia com ~5px na tela. Os valores já estão nas colunas da tabela —
  // "Preço hoje", "Desde ontem", "Melhor em 30 dias" —, então a linha é
  // complemento visual e nenhum número depende do tooltip para ser lido.
  readonly L = 3;
  readonly R = 5;
  readonly T = 5;
  readonly B = 5;
  readonly W = 130;
  readonly H = 34;

  readonly serie = computed<Plotado[]>(() => {
    const pts = this._points();
    if (pts.length === 0) return [];

    const precos = pts.map(p => p.p);
    const min = Math.min(...precos);
    const max = Math.max(...precos);
    const largura = this.W - this.L - this.R;
    const altura = this.H - this.T - this.B;

    // Série constante (ou um ponto só): linha no meio, em vez de dividir por zero
    // ou colar tudo na borda de cima.
    const amplitude = max - min;

    return pts.map((ponto, i) => ({
      ponto,
      x: this.L + (pts.length === 1 ? largura / 2 : (i / (pts.length - 1)) * largura),
      y: amplitude === 0
        ? this.T + altura / 2
        : this.T + altura - ((ponto.p - min) / amplitude) * altura,
    }));
  });

  readonly caminho = computed(() =>
    this.serie().map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' '));

  readonly ultimo = computed<Plotado | null>(() => {
    const s = this.serie();
    return s.length > 0 ? s[s.length - 1] : null;
  });

  readonly menor = computed(() => {
    const pts = this._points();
    return pts.length > 0 ? Math.min(...pts.map(p => p.p)) : 0;
  });

  readonly maior = computed(() => {
    const pts = this._points();
    return pts.length > 0 ? Math.max(...pts.map(p => p.p)) : 0;
  });

  /** Com menos de dois dias não há "ao longo do tempo" para mostrar. */
  readonly temSerie = computed(() => this._points().length >= 2);

  // ===== Tooltip =====

  readonly ativo = signal<Plotado | null>(null);

  /**
   * Ponto mais próximo no eixo X, em vez de exigir acerto no marcador.
   * Um alvo de 8px que a pessoa tem de acertar no centro não é alvo.
   */
  aoMover(evento: MouseEvent, svg: Element): void {
    const s = this.serie();
    if (s.length === 0) return;

    const caixa = svg.getBoundingClientRect();
    const xNoViewBox = ((evento.clientX - caixa.left) / caixa.width) * this.W;

    let perto = s[0];
    for (const p of s) {
      if (Math.abs(p.x - xNoViewBox) < Math.abs(perto.x - xNoViewBox)) perto = p;
    }
    this.ativo.set(perto);
  }

  aoSair(): void {
    this.ativo.set(null);
  }

  /** Posição do tooltip em %, para ele seguir o ponto sem sair do container. */
  esquerdaDoTooltip(p: Plotado): string {
    const pct = (p.x / this.W) * 100;
    return `${Math.min(88, Math.max(12, pct))}%`;
  }

  formatarPreco(valor: number): string {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  rotuloDia(iso?: string): string {
    if (!iso) return '';
    // Sem `new Date(iso)`: a string sem fuso é lida como UTC e volta um dia em
    // qualquer fuso negativo — o gráfico mostraria 31/10 para o dado de 01/11.
    const [, mes, dia] = iso.split('-');
    return `${dia}/${mes}`;
  }

  rotuloDoAtivo(p: Plotado): string {
    return `${this.rotuloDia(p.ponto.d)} · ${this.formatarPreco(p.ponto.p)}`;
  }
}
