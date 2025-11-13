import { Component, Input, forwardRef } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'input-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="custom-input-wrapper">
      @if (iconSrc) { <img class="input-icon-img" [src]="iconSrc" alt="icon"> }
      <input
        class="input-box"
        [attr.placeholder]="placeholder"
        [type]="type"
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
}
