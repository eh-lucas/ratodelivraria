import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { UserMenuComponent } from '../user-menu/user-menu';
import { UserService, UserCredits } from '../../services/user-service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, UserMenuComponent],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class HeaderComponent implements OnInit, OnDestroy {
  @Output() menuToggle = new EventEmitter<void>();

  userCredits: UserCredits | null = null;
  userMenuOpen = false;

  private creditsSub?: Subscription;

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    // Dispara fetch inicial e assina o stream global de créditos
    this.userService.getCurrentUser().subscribe();
    this.creditsSub = this.userService.credits$.subscribe(c => {
      this.userCredits = c;
    });
  }

  ngOnDestroy(): void {
    this.creditsSub?.unsubscribe();
  }

  toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.userMenuOpen = !this.userMenuOpen;
  }

  closeUserMenu(): void {
    this.userMenuOpen = false;
  }

  onMobileMenu(): void {
    this.menuToggle.emit();
  }

  get userInitial(): string {
    return this.userCredits?.username?.[0]?.toUpperCase() ?? '?';
  }
}
