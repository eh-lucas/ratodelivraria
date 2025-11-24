//Angular
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, Validators, FormBuilder, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
//Services
import { AuthService } from '../../services/auth-service';
//Components
import { InputComponent } from '../../components/input-component/input-component'

@Component({
  selector: 'login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, InputComponent, ReactiveFormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss'
})

export class LoginPage implements OnInit {
  username: string = '';
  password: string = '';
  loginForm!: FormGroup;

  constructor(private authService: AuthService, private router: Router, private formBuilder: FormBuilder) { }
  
  ngOnInit(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  get f() {
    return this.loginForm.controls;
  }

   onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched(); 
      return;
    }

    const { email, password } = this.loginForm.value;
    this.authService.login(email, password).subscribe({
      next: (response: any) => {
        if (response && response.token) {
          this.authService.setToken(response.token);
          this.router.navigate(['/search']);
        }
      },
      error: (err: any) => {
        console.error('Login failed.', err);
      }
    });
  }
}
