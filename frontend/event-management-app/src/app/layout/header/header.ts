import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class HeaderComponent  {
  mobileMenuOpen = computed(() => false);
  private _mobileMenuOpen = false;

  currentUser = computed(() => this.authService.currentUser());
  isAuthenticated = computed(() => !!this.authService.currentUser());

  constructor(private authService: AuthService) {}

  toggleMobileMenu(): void {
    this._mobileMenuOpen = !this._mobileMenuOpen;
  }

  closeMobileMenu(): void {
    this._mobileMenuOpen = false;
  }

  logout(): void {
    this.authService.logout();
  }

  getUserInitials(): string {
    const user = this.currentUser();
    if (!user) return '';

    return user.name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }
}
