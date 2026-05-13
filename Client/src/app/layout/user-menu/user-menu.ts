import { Component, ElementRef, EventEmitter, HostListener, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth-service';
import { UserService } from '../../services/user-service';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-menu.html',
  styleUrl: './user-menu.scss',
})
export class UserMenuComponent {
  @Output() close = new EventEmitter<void>();

  constructor(
    private elementRef: ElementRef,
    private authService: AuthService,
    private userService: UserService,
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
    this.close.emit();
    this.router.navigate(['/login']);
  }
}
