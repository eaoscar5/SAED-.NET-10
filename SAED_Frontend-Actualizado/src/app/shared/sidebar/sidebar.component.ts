import { Component, OnInit, HostListener } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ROUTES } from './menu-items';
import { RouteInfo } from './sidebar.metadata';
import { AuthService } from 'src/app/core/services/auth.service';
import { ThemeToggleComponent } from '../theme-toggle/theme-toggle.component';

@Component({
    selector: 'app-sidebar',
    imports: [RouterModule, CommonModule, ThemeToggleComponent],
    templateUrl: './sidebar.component.html',
    styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit {

  sidebarnavItems: RouteInfo[] = [];
  showMenu    = '';
  sidebarOpen = false; // false = cerrado en mobile

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    const sesion = this.authService.getSesion();
    const rol    = sesion?.rol ?? '';
    this.sidebarnavItems = ROUTES.filter(item =>
      !item.role || item.role.length === 0 || item.role.includes(rol)
    );
    const activeParent = this.sidebarnavItems.find(item =>
      item.submenu.some(sub => this.router.url === sub.path || this.router.url.startsWith(`${sub.path}/`))
    );
    this.showMenu = activeParent?.title ?? '';
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  closeSidebar(): void {
    this.sidebarOpen = false;
  }

  toggleSubmenu(title: string): void {
    this.showMenu = this.showMenu === title ? '' : title;
  }

  logout(): void {
    if (!confirm('¿Estás seguro de que deseas cerrar sesión?')) {
      return;
    }

    this.authService.logout();
    this.closeSidebar();
  }

  // Cerrar sidebar al redimensionar a desktop
  @HostListener('window:resize', ['$event'])
  onResize(event: Event): void {
    if ((event.target as Window).innerWidth > 768) {
      this.sidebarOpen = false;
    }
  }
}
