import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { UserService, UserCredits } from '../../services/user-service';
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class NavbarComponent implements OnInit, OnDestroy {
  userCredits: UserCredits | null = null;
  isAuthenticated = false;
  isMenuOpen = false;
  currentRoute = '';

  // Rotas onde o navbar nao deve aparecer
  private publicRoutes = ['/', '/login', '/register'];

  private creditsSubscription?: Subscription;
  private routerSubscription?: Subscription;

  constructor(
    private userService: UserService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Define a rota atual ANTES de verificar autenticacao
    this.currentRoute = this.router.url;

    // Verifica autenticacao inicial
    this.checkAuthentication();

    // Observa mudancas de rota para re-verificar autenticacao
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.currentRoute = event.urlAfterRedirects;
        this.isMenuOpen = false;
        this.checkAuthentication();
      });
  }

  private isPublicRoute(): boolean {
    return this.publicRoutes.includes(this.currentRoute);
  }

  private checkAuthentication(): void {
    // Nao mostrar navbar em rotas publicas
    if (this.isPublicRoute()) {
      this.isAuthenticated = false;
      return;
    }

    this.isAuthenticated = this.authService.isAuthenticated();

    if (this.isAuthenticated && !this.creditsSubscription) {
      // Carrega creditos iniciais
      this.userService.getCurrentUser().subscribe();

      // Observa mudancas nos creditos
      this.creditsSubscription = this.userService.credits$.subscribe(credits => {
        this.userCredits = credits;
      });
    } else if (!this.isAuthenticated) {
      this.userCredits = null;
      this.creditsSubscription?.unsubscribe();
      this.creditsSubscription = undefined;
    }
  }

  ngOnDestroy(): void {
    this.creditsSubscription?.unsubscribe();
    this.routerSubscription?.unsubscribe();
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
  }

  isActive(route: string): boolean {
    return this.currentRoute === route || this.currentRoute.startsWith(route + '/');
  }

  logout(): void {
    this.authService.logout();
    this.userService.clearUserData();
    this.router.navigate(['/login']);
  }
}
