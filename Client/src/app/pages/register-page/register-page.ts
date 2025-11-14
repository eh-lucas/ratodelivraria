import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, ValidationErrors, ReactiveFormsModule, Validators, FormBuilder, FormGroup } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule],
  standalone: true,
  templateUrl: './register-page.html',
  styleUrls: ['./register-page.scss']
})

export class RegisterPage implements OnInit {
  loading = false;
  successMessage = '';
  errorMessage = '';
  registerForm!: FormGroup;

  constructor(private formBuilder: FormBuilder, private http: HttpClient) {}

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
    }, {
      validators: this.passwordMatchValidator()
    });
  }

  passwordMatchValidator() {
  return (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (password && confirmPassword && password !== confirmPassword) {
      return { mismatch: true };
    }

    return null;
  };
  }

  submit() {
    if (this.registerForm.invalid) return;

    this.loading = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.http.post('/api/register', this.registerForm.value).subscribe({
      next: () => {
        this.successMessage = 'Conta criada com sucesso!';
        this.loading = false;
        this.registerForm.reset();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Erro ao criar conta.';
        this.loading = false;
      }
    });
  }
}