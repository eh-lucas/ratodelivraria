import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, timer } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, takeWhile } from 'rxjs/operators';
import { WatchedService, WatchedBook, WatchRule, PricePoint } from '../../services/watched-service';
import { PriceChartComponent } from '../../components/price-chart/price-chart';
import { CatalogService, CatalogSuggestion } from '../../services/catalog-service';
import { TranslatePipe } from '../../i18n/translate-pipe';
import { AuthRequiredComponent } from '../../components/auth-required/auth-required';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-watched-page',
  standalone: true,
  imports: [TranslatePipe, CommonModule, FormsModule, DatePipe, AuthRequiredComponent, PriceChartComponent],
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

  /** Série do gráfico por ISBN. Vem numa chamada só, junto com a lista. */
  historico: Record<string, PricePoint[]> = {};
  loading = false;
  loadError = '';

  // Form
  showForm = false;
  newIsbn = '';

  // Busca por nome, o mesmo caminho do /search: um campo só serve aos dois modos.
  // Antes daqui a pessoa tinha de sair para o /search, achar o ISBN, copiar e
  // voltar — com 54 mil titulos no catalogo, a informacao ja estava no banco.
  suggestions: CatalogSuggestion[] = [];
  suggestionsOpen = false;
  suggestionError = '';
  resolvingSuggestionId: number | null = null;
  /** Titulo e capa da sugestao escolhida, para o cartao nascer com rotulo. */
  private escolhido: { title?: string; imageUrl?: string } = {};
  private readonly nameQuery$ = new Subject<string>();
  newRule: WatchRule = WatchRule.PercentDrop;
  newTargetPrice: number | null = null;
  newDropPercent = 15;
  formError = '';
  saving = false;

  private sub?: Subscription;

  constructor(
    private service: WatchedService,
    private auth: AuthService,
    private catalog: CatalogService,
  ) {}

  ngOnInit(): void {
    this.isAuthenticated = this.auth.isAuthenticated();
    this.sub = this.service.items$.subscribe(list => (this.items = list));

    // Mesmo debounce do /search: espera parar de digitar antes de consultar.
    this.nameQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(term => this.catalog.suggest(term)),
      )
      .subscribe(list => {
        this.suggestions = list;
        this.suggestionsOpen = list.length > 0;
      });

    if (this.isAuthenticated) this.recarregar();
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private recarregar(): void {
    this.loading = true;
    this.loadError = '';
    this.service.history().subscribe({
      next: series => (this.historico = series),
      // Gráfico é complemento: sem ele o cartão continua inteiro, então a falha
      // não vira erro de tela.
      error: () => (this.historico = {}),
    });

    this.service.load().subscribe({
      next: () => (this.loading = false),
      error: () => {
        this.loading = false;
        this.loadError = 'Não foi possível carregar sua lista. Tente novamente.';
      },
    });
  }

  /**
   * Texto que parece ISBN segue direto; o resto vira busca por nome.
   * Mesma regra do /search — duas telas com criterios diferentes para "isto e um
   * ISBN?" seriam dois comportamentos para a mesma pergunta.
   */
  get isSearchingByName(): boolean {
    const raw = this.normalizeIsbn(this.newIsbn);
    return raw.length > 0 && !/^\d{9}[\dXx]$|^\d{13}$/.test(raw) && !/^\d+$/.test(raw);
  }

  private normalizeIsbn(raw: string): string {
    return raw.replace(/[\s-]/g, '').trim();
  }

  onQueryChange(): void {
    this.suggestionError = '';
    this.formError = '';
    // Digitou de novo: a escolha anterior nao vale mais para este texto.
    this.escolhido = {};

    if (!this.isSearchingByName) {
      this.suggestions = [];
      this.suggestionsOpen = false;
      return;
    }

    this.nameQuery$.next(this.newIsbn.trim());
  }

  /**
   * Escolheu um titulo: se o ISBN ja e conhecido, usa; senao pede ao backend que
   * descubra na pagina do produto. Guarda titulo e capa para mandar no request —
   * o WatchBookRequest os aceita e e assim que o cartao nasce com rotulo.
   */
  selectSuggestion(s: CatalogSuggestion): void {
    this.suggestionError = '';

    if (s.isbn) {
      this.aplicarEscolha(s.isbn, s.title);
      return;
    }

    this.resolvingSuggestionId = s.id;
    this.catalog.resolveIsbn(s.id).subscribe(r => {
      this.resolvingSuggestionId = null;

      if (r.found && r.isbn) {
        this.aplicarEscolha(r.isbn, r.title ?? s.title);
      } else {
        this.suggestionError = r.error || 'Não foi possível identificar o ISBN deste título.';
      }
    });
  }

  private aplicarEscolha(isbn: string, title?: string): void {
    this.newIsbn = isbn;
    this.escolhido = { title };
    this.suggestions = [];
    this.suggestionsOpen = false;
  }

  openForm(): void {
    this.showForm = true;
    this.formError = '';
    this.suggestionError = '';
    this.suggestions = [];
    this.suggestionsOpen = false;
    this.escolhido = {};
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
    // Enter durante a busca por nome nao deve virar ISBN: o texto cru nao e um.
    if (this.isSearchingByName) {
      this.formError = this.suggestions.length > 0
        ? 'Escolha um título da lista.'
        : 'Nenhum título encontrado. Tente outro nome ou informe o ISBN.';
      return;
    }

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
      title: this.escolhido.title,
      imageUrl: this.escolhido.imageUrl,
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
        this.aguardarVarredura(isbn);
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

  /**
   * Espera a primeira varredura chegar e então redesenha a linha.
   *
   * O POST responde antes de a varredura terminar — é de propósito, para a pessoa
   * não ficar ~17s parada esperando 67 lojas. Mas a lista era recarregada no mesmo
   * instante, ou seja, sempre antes dos dados: o livro aparecia sem capa e sem
   * preço e ficava assim até alguém recarregar a página na mão.
   *
   * Consulta a cada 5s por até 1 minuto. Para assim que o preço aparece, e desiste
   * em silêncio se não vier — a varredura pode ter falhado, e nesse caso a rodada
   * da madrugada ainda atende. Um aviso de erro aqui seria sobre algo que a pessoa
   * não pediu e não pode resolver.
   */
  private aguardarVarredura(isbn: string): void {
    const ateSeis = 12;

    timer(5000, 5000)
      .pipe(takeWhile(tentativa => tentativa < ateSeis && !this.temDados(isbn)))
      .subscribe(() => {
        this.service.load().subscribe({ error: () => {} });
        this.service.history().subscribe({
          next: series => (this.historico = series),
          error: () => {},
        });
      });
  }

  private temDados(isbn: string): boolean {
    const livro = this.items.find(w => w.isbn === isbn);
    return !!livro && livro.lastPrice != null;
  }

  serieDe(isbn: string): PricePoint[] {
    return this.historico[isbn] ?? [];
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
