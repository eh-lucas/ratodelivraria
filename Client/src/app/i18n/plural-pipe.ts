import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from './i18n-service';

/**
 * Escolhe a forma singular ou plural e traduz.
 * Uso: {{ 'livro|livros' | tp:books.length }}
 */
@Pipe({ name: 'tp', standalone: true })
export class PluralPipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  transform(forms: string, count: number): string {
    const [one, many] = forms.split('|');
    return this.i18n.t(count === 1 ? one : (many ?? one));
  }
}
