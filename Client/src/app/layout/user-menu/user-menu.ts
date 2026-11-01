import { Component, ElementRef, EventEmitter, HostListener, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { UserService } from '../../services/user-service';
import { WatchedService } from '../../services/watched-service';
import { environment } from '../../../environments/environment';
import { TranslatePipe } from '../../i18n/translate-pipe';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [TranslatePipe, CommonModule, RouterLink],
  templateUrl: './user-menu.html',
  styleUrl: './user-menu.scss',
})
export class UserMenuComponent {
  @Output() close = new EventEmitter<void>();

  // Em modo demo não há compra de créditos por usuário — esconde esse item.
  readonly demoMode = environment.demoMode;

  /**
   * O menu agora atende os dois estados. Antes, "Sair" só aparecia fora do modo
   * demo e não havia "Entrar" em lugar nenhum — num site aberto, o visitante não
   * tinha por onde chegar ao login a não ser digitando a URL.
   */
  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  constructor(
    private elementRef: ElementRef,
    private authService: AuthService,
    private userService: UserService,
    private watchedService: WatchedService,
    private router: Router,
  ) {}

  // Fecha o dropdown ao clicar fora dele
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.close.emit();
    }
  }

  // Fecha ao apertar Esc
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close.emit();
  }

  onItemClick(): void {
    this.close.emit();
  }

  logout(): void {
    this.authService.logout();
    this.userService.clearUserData();
    // A lista de observados fica em memória no serviço. Sem limpar, quem entra
    // em seguida na mesma aba vê por um instante os livros de quem saiu.
    this.watchedService.clear();
    this.close.emit();
    this.router.navigate(['/login']);
  }
}
