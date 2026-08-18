import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from './i18n-service';

/** Uso no template: {{ 'Pesquisar livros' | t }} */
@Pipe({ name: 't', standalone: true })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  transform(value: string): string {
    return this.i18n.t(value);
  }
}
