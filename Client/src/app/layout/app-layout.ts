import { Component, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from './sidebar/sidebar';
import { HeaderComponent } from './header/header';

const SIDEBAR_STORAGE_KEY = 'sherlock.sidebar.collapsed';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, HeaderComponent],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayoutComponent implements OnInit {
  // Estado da sidebar no desktop (colapsada vs expandida)
  collapsed = signal(false);
  // Estado do drawer no mobile (aberto vs fechado)
  mobileOpen = signal(false);

  ngOnInit(): void {
    // Restaura preferência de colapso entre sessões
    const stored = localStorage.getItem(SIDEBAR_STORAGE_KEY);
    if (stored === 'true') this.collapsed.set(true);
  }

  toggleSidebar(): void {
    // Em mobile o botão controla o drawer; em desktop alterna o colapso
    if (window.innerWidth < 768) {
      this.mobileOpen.update(v => !v);
    } else {
      this.collapsed.update(v => {
        const next = !v;
        localStorage.setItem(SIDEBAR_STORAGE_KEY, String(next));
        return next;
      });
    }
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }
}
