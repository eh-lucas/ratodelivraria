import { Component, Input, forwardRef } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'input-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="custom-input-wrapper">
      @if (iconSrc) { <img class="input-icon-img" [src]="iconSrc" alt="" aria-hidden="true"> }
      <input
        class="input-box"
        [class.has-icon]="!!iconSrc"
        [attr.id]="inputId"
        [attr.name]="inputId"
        [attr.placeholder]="placeholder"
        [attr.aria-label]="ariaLabel || placeholder"
        [attr.autocomplete]="autocomplete"
        [attr.inputmode]="inputMode"
        [attr.aria-invalid]="ariaInvalid ? 'true' : null"
        [attr.aria-describedby]="ariaDescribedby"
        [type]="type"
        [disabled]="disabled"
        [value]="value"
        (input)="onInput($event)"
        (blur)="onTouched()"
      />
    </div>
  `,
  styleUrls: ['./input-component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ]
})

export class InputComponent implements ControlValueAccessor {
  @Input() iconSrc?: string;
  @Input() hint?: string;
  @Input() placeholder?: string;
  @Input() type?: string = 'text';

  /** Vira id e name do campo. Sem name, gerenciador de senha nenhum reconhece o formulário. */
  @Input() inputId?: string;

  /** 'email', 'current-password', 'one-time-code'... É o que faz o preenchimento automático funcionar. */
  @Input() autocomplete?: string;

  /** 'numeric' abre o teclado de números no celular — serve para o código de login. */
  @Input() inputMode?: string;

  /** Rótulo para leitor de tela quando o placeholder não basta. */
  @Input() ariaLabel?: string;

  /** Marca o campo como inválido para a tecnologia assistiva. */
  @Input() ariaInvalid = false;

  /** Id do elemento que descreve o erro deste campo. */
  @Input() ariaDescribedby?: string;

  @Input() disabled = false;

  value: string = '';

  onChange = (_: any) => {};

  onTouched = () => {};

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.value = input.value;
    this.onChange(this.value);
  }

  writeValue(value: any): void {
    this.value = value || '';
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
