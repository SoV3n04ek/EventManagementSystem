import {
  Component,
  ChangeDetectionStrategy,
  computed,
  signal,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/**
 * HeaderComponent — App navigation bar.
 *
 * Fixes:
 * - mobileMenuOpen was `computed(() => false)` — always false!
 *   Now a writable signal(false) with .update() for toggling.
 * - Constructor DI → inject()
 * - OnPush change detection
 */
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.html',
  styleUrl: './header.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent {
  private readonly authService = inject(AuthService);

  // ── Reactive State ──
  readonly mobileMenuOpen = signal(false);
  readonly currentUser = computed(() => this.authService.currentUser());
  readonly isAuthenticated = computed(() => !!this.authService.currentUser());

  readonly userInitials = computed(() => {
    const user = this.currentUser();
    if (!user) return '';
    return user.name
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  });

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update(open => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
  }
}
