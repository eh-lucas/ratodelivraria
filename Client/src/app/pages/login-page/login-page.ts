//Angular
import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, Validators, FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
//Services
import { AuthService } from '../../services/auth-service';
//Components
import { InputComponent } from '../../components/input-component/input-component'
import { TranslatePipe } from '../../i18n/translate-pipe';
import { GoogleIdentityService } from '../../services/google-identity';

@Component({
  selector: 'login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, InputComponent, ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss'
})

export class LoginPage implements OnInit, AfterViewInit {
  @ViewChild('googleButton') googleButton?: ElementRef<HTMLElement>;

  /** Só true depois que o botão foi realmente desenhado — ver ngAfterViewInit. */
  googleAvailable = false;

  /** A falha foi "esta conta entra pelo Google": o aviso é instrução, não erro. */
  suggestGoogle = false;

  loginForm!: FormGroup;

  /** Texto da falha de login. É chave do dicionário: o template passa pelo pipe `t`. */
  errorMessage = '';
  loading = false;

  /** Para onde voltar depois de entrar, quando o guard desviou de alguma tela. */
  private redirectTo = '/home';

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private googleIdentity: GoogleIdentityService,
  ) { }

  ngOnInit(): void {
    // Só aceita caminho interno: 'redirect' vem da URL, e um valor absoluto
    // viraria um salto para fora do site logo depois do login.
    const pedido = this.route.snapshot.queryParamMap.get('redirect');
    if (pedido?.startsWith('/') && !pedido.startsWith('//')) {
      this.redirectTo = pedido;
    }

    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  async ngAfterViewInit(): Promise<void> {
    if (!this.googleButton) return;

    // googleAvailable só liga se o botão apareceu de verdade. Se o client id não
    // está configurado, ou o script não carregou, a tela fica sendo o formulário de
    // e-mail e senha — sem um "ou" solto separando nada.
    this.googleAvailable = await this.googleIdentity.renderButton(
      this.googleButton.nativeElement,
      credential => this.onGoogleCredential(credential),
    );
  }

  private onGoogleCredential(credential: string): void {
    this.loading = true;
    this.errorMessage = '';
    this.suggestGoogle = false;

    this.authService.loginWithGoogle(credential).subscribe({
      next: (response: any) => {
        this.loading = false;

        if (response?.token) {
          this.authService.setToken(response.token);
          this.router.navigateByUrl(this.redirectTo);
          return;
        }

        this.errorMessage = 'Não foi possível entrar agora. Tente de novo em instantes.';
      },
      error: (err: any) => {
        this.loading = false;
        this.errorMessage = err?.error?.message
          ?? 'Não foi possível entrar com o Google. Tente de novo em instantes.';
      },
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
    this.suggestGoogle = false;

    const { email, password } = this.loginForm.value;
    this.authService.login(email, password).subscribe({
      next: (response: any) => {
        this.loading = false;

        if (response?.token) {
          this.authService.setToken(response.token);
          this.router.navigateByUrl(this.redirectTo);
          return;
        }

        // Resposta 200 sem token: não dá para entrar, e antes isto passava calado.
        this.errorMessage = 'Não foi possível entrar agora. Tente de novo em instantes.';
      },
      error: (err: any) => {
        this.loading = false;

        // Conta criada pelo Google não tem senha nenhuma para estar errada: o
        // backend marca useGoogle e aqui isso vira instrução, não acusação.
        if (err?.error?.useGoogle) {
          this.suggestGoogle = true;
          this.errorMessage = 'Esta conta entra pelo Google. Use o botão acima.';
          return;
        }

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
