import {
  ApplicationConfig,
  inject,
  provideBrowserGlobalErrorListeners,
  provideAppInitializer,
  provideZoneChangeDetection
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth-interceptor';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { AuthService } from './core/services/auth.service';

/**
 * App Configuration — Antigravity Protocol (Phase 1)
 *
 * Security features:
 * ─────────────────────────────────────────────────
 * 1. withXsrfConfiguration() — CSRF protection.
 *    The server sets a `XSRF-TOKEN` cookie (non-HttpOnly),
 *    Angular reads it and sends it back as `X-XSRF-TOKEN` header.
 * 2. provideAppInitializer() — rehydrates the auth session
 *    from the HttpOnly cookie BEFORE any route guard fires.
 *    This eliminates the race condition between session
 *    rehydration and route guard evaluation.
 * ─────────────────────────────────────────────────
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor]),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN'
      })
    ),
    provideBrowserGlobalErrorListeners(),

    // ── Auth Bootstrap ──
    // Resolves a Promise before the app renders, ensuring
    // AuthService.isAuthenticated() is reliable for guards.
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      return authService.loadCurrentUser();
    })
  ]
};
