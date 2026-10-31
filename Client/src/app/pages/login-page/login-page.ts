//Angular
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, Validators, FormBuilder, FormGroup } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
//Services
import { AuthService } from '../../services/auth-service';
//Components
import { InputComponent } from '../../components/input-component/input-component'
import { TranslatePipe } from '../../i18n/translate-pipe';

@Component({
  selector: 'login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, InputComponent, ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss'
})

export class LoginPage implements OnInit {
  loginForm!: FormGroup;

  /** Texto da falha de login. É chave do dicionário: o template passa pelo pipe `t`. */
  errorMessage = '';
  loading = false;

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

  /** Só acusa erro depois que a pessoa mexeu no campo — não ao abrir a tela. */
  mostraErro(campo: string): boolean {
    const controle = this.loginForm.get(campo);
    return !!controle && controle.invalid && (controle.touched || controle.dirty);
  }

  onSubmit(): void {
    // O botão não fica mais desabilitado enquanto o formulário é inválido: assim
    // some o caso em que a pessoa clica, nada acontece e a tela não explica por quê.
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email, password } = this.loginForm.value;
    this.authService.login(email, password).subscribe({
      next: (response: any) => {
        this.loading = false;

        if (response?.token) {
          this.authService.setToken(response.token);
          this.router.navigate(['/home']);
          return;
        }

        // Resposta 200 sem token: não dá para entrar, e antes isto passava calado.
        this.errorMessage = 'Não foi possível entrar agora. Tente de novo em instantes.';
      },
      error: (err: any) => {
        this.loading = false;

        // 401 é credencial errada; o resto é problema nosso, e dizer "e-mail ou
        // senha incorretos" quando a API caiu manda a pessoa procurar erro onde
        // não tem.
        this.errorMessage = err?.status === 401
          ? 'E-mail ou senha incorretos.'
          : 'Não foi possível entrar agora. Tente de novo em instantes.';
      }
    });
  }
}
