import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, ValidationErrors, ReactiveFormsModule, Validators, FormBuilder, FormGroup } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { InputComponent } from '../../components/input-component/input-component';
import { GoogleIdentityService } from '../../services/google-identity';
import { TranslatePipe } from '../../i18n/translate-pipe';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, InputComponent, TranslatePipe],
  standalone: true,
  templateUrl: './register-page.html',
  styleUrls: ['./register-page.scss']
})
export class RegisterPage implements OnInit, AfterViewInit {
  @ViewChild('googleButton') googleButton?: ElementRef<HTMLElement>;

  googleAvailable = false;
  loading = false;
  errorMessage = '';
  registerForm!: FormGroup;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private googleIdentity: GoogleIdentityService,
  ) {}

  ngOnInit(): void {
    // 'username' saiu do formulário: era um campo a mais para a pessoa inventar, e
    // o backend já deriva um a partir do e-mail. O que o produto quer é o primeiro
    // nome, para cumprimentar — nada além disso.
    this.registerForm = this.formBuilder.group({
      firstName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
    }, {
      validators: this.passwordMatchValidator()
    });
  }

  async ngAfterViewInit(): Promise<void> {
    if (!this.googleButton) return;

    // Criar conta pelo Google é o mesmo endpoint de entrar: se o e-mail não existe,
    // a conta nasce ali. Por isso o botão é o mesmo nas duas telas.
    this.googleAvailable = await this.googleIdentity.renderButton(
      this.googleButton.nativeElement,
      credential => this.onGoogleCredential(credential),
    );
  }

  get f() {
    return this.registerForm.controls;
  }

  /** Só acusa erro depois que a pessoa mexeu no campo. */
  mostraErro(campo: string): boolean {
    const controle = this.registerForm.get(campo);
    return !!controle && controle.invalid && (controle.touched || controle.dirty);
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

  private onGoogleCredential(credential: string): void {
    this.loading = true;
    this.errorMessage = '';

    this.authService.loginWithGoogle(credential).subscribe({
      next: (response: any) => {
        this.loading = false;

        if (response?.token) {
          this.authService.setToken(response.token);
          this.router.navigate(['/home']);
          return;
        }

        this.errorMessage = 'Não foi possível criar sua conta agora. Tente de novo em instantes.';
      },
      error: (err: any) => {
        this.loading = false;
        this.errorMessage = err?.error?.message
          ?? 'Não foi possível entrar com o Google. Tente de novo em instantes.';
      },
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email, password, firstName } = this.registerForm.value;

    this.authService.register(email, password, undefined, firstName).subscribe({
      next: (response: any) => {
        this.loading = false;

        if (response?.token) {
          this.authService.setToken(response.token);
          // Vai para a home, não para a busca: quem acabou de criar conta veio pelo
          // que a conta oferece — observar e histórico —, e é na home que isso está.
          this.router.navigate(['/home']);
          return;
        }

        this.errorMessage = 'Não foi possível criar sua conta agora. Tente de novo em instantes.';
      },
      error: (err) => {
        this.loading = false;

        // 409 é e-mail já cadastrado: mandar para o login é mais útil que repetir o
        // erro, porque quase sempre é a própria pessoa que já tem conta.
        this.errorMessage = err?.status === 409
          ? 'Já existe conta com este e-mail. Tente entrar.'
          : err?.error?.message || 'Não foi possível criar sua conta agora. Tente de novo em instantes.';
      }
    });
  }
}
