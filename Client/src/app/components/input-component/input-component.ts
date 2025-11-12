import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'input-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="custom-input-wrapper">

      @if (iconSrc) { <img class="input-icon-img" [src]="iconSrc" alt="icon"> }

      <input class="input-box" [attr.placeholder]="placeholder" [(ngModel)]="value" [type]="type" >

    </div>
  `,
  styleUrls: ['./input-component.scss']
})
export class InputComponent {
  @Input() iconSrc?: string;
  @Input() hint?: string;
  @Input() placeholder?: string;
  @Input() type?: string = 'text';
  @Input() value: string = '';
}
