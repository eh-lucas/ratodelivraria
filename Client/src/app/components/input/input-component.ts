import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'custom-input',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="custom-input-wrapper">

      @if (iconSrc) { <img class="input-icon-img" [src]="iconSrc" alt="icon"> }

      @if (hint) { <span class="input-hint">{{ hint }}</span> }

      <input class="input-placeholder" [attr.placeholder]="placeholder" [(ngModel)]="value">

    </div>
  `,
  styleUrls: ['./input-component.scss']
})
export class InputComponent {
  @Input() iconSrc?: string;
  @Input() hint?: string;
  @Input() placeholder?: string;
  @Input() value: string = '';
}
