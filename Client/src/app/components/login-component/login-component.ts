//Angular
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient} from '@angular/common/http';
//Services
import { AuthService } from '../../services/auth-service';
//Components
import { InputComponent } from '../input-component/input-component'
import { LinkButtonComponent } from '../link-button-component/link-button-component'

@Component({
  selector: 'login-component',
  standalone: true,
  imports: [CommonModule, FormsModule, InputComponent, LinkButtonComponent],
  templateUrl: './login-component.html',
  styleUrl: './login-component.scss'
})
export class LoginComponent {
  username: string = '';
  password: string = '';
  constructor(private authService: AuthService, private router: Router) { }
  
  onLogin(): void {
  
    this.authService.login(this.username, this.password).subscribe({
      next: (response: any) => {
        if (response && response.token) {
          this.authService.setToken(response.token);
        }
      this.router.navigate(['/home']);
      },
      error: (err: any) => {
        console.error('Login failed.', err);
      }
    });
  }
}
