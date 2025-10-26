import { Component } from '@angular/core';

@Component({
  selector: 'input[custom-input]',
  imports: [],
  template: '<ng-content/>',
  styleUrl: './input-component.scss'
})
export class InputComponent {

}
