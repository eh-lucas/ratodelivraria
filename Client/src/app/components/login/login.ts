import { Component } from '@angular/core';
import { Auth } from '../../services/auth';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  username: string = '';
  password: string = '';

  constructor(private authService: Auth, private router: Router) { }

  onLogin(): void {
    this.authService.login(this.username, this.password).subscribe({
      next: (response) => {
        this.authService.setToken(response.token);
        this.router.navigate(['/home']); // não temos essa rota, AINDA
      },
      error: (err) => {
        console.error('Login failed.', err);
      }
    });
  }
}
