import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, ValidationErrors, ReactiveFormsModule, Validators, FormBuilder, FormGroup } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { InputComponent } from '../../components/input-component/input-component';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, InputComponent],
  standalone: true,
  templateUrl: './register-page.html',
  styleUrls: ['./register-page.scss']
})
export class RegisterPage implements OnInit {
  loading = false;
  errorMessage = '';
  registerForm!: FormGroup;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
    }, {
      validators: this.passwordMatchValidator()
    });
  }

  get f() {
    return this.registerForm.controls;
  }

  passwordMatchValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.get('password')?.value;
      const confirmPassword = control.get('confirmPassword')?.value;

      if (password && confirmPassword && password !== confirmPassword) {
        control.get('confirmPassword')?.setErrors({ mismatch: true });
        return { mismatch: true };
      }

      return null;
    };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email, password, username } = this.registerForm.value;

    this.authService.register(email, password, username).subscribe({
      next: (response: any) => {
        if (response && response.token) {
          this.authService.setToken(response.token);
          this.router.navigate(['/search']);
        }
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erro ao criar conta. Tente novamente.';
        this.loading = false;
      }
    });
  }
}
