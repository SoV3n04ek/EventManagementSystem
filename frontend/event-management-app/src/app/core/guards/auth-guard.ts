import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Auth Guard — Antigravity Protocol (Phase 1)
 *
 * Reads from AuthService.isAuthenticated (computed Signal),
 * which is guaranteed to be resolved by provideAppInitializer()
 * before any route guard fires.
 *
 * No more TokenService/localStorage dependency.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
