import { Component } from '@angular/core';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'home-component',
  imports: [],
  templateUrl: './home-component.html',
  styleUrl: './home-component.scss'
})

export class HomeComponent {
  constructor(private authService: AuthService) { }
  logout(): void {
    this.authService.logout();
  }
}
