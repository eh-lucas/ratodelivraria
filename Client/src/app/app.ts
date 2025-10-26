import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { InputComponent } from './components/input/input-component'
import { LinkButtonComponent } from './components/link-button-component/link-button-component'
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LinkButtonComponent, InputComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Client');
}
