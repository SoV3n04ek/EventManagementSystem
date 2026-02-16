import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, firstValueFrom, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../../models/user';

/**
 * AuthService
 *
 * Architecture decisions:
 * ─────────────────────────────────────────────────────────────
 * 1. inject() over constructor DI — Angular 20.x best practice.
 * 2. signal<User | null> for currentUser — NOT linkedSignal,
 *    because the user state is independently writable (set on login,
 *    cleared on logout) rather than derived from another signal.
 * 3. Zero TokenService/localStorage references — the JWT lives
 *    exclusively in an HttpOnly cookie managed by the browser.
 * 4. Session rehydration via loadCurrentUser() — called through
 *    provideAppInitializer() in app.config.ts so the auth state
 *    is resolved BEFORE any route guard fires.
 * ─────────────────────────────────────────────────────────────
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // ── Injected Dependencies (inject() over constructor DI) ──
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly apiUrl = `${environment.apiUrl}/auth`;

  // ── Reactive Auth State ──
  readonly currentUser = signal<User | null>(null);
  readonly isAuthenticated = computed(() => !!this.currentUser());

  // ── Public API ──

  register(data: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  login(credentials: LoginRequest): Observable<{ data: { user: User } }> {
    return this.http.post<{ data: { user: User } }>(`${this.apiUrl}/login`, credentials)
      .pipe(
        tap(response => {
          // The server sets the HttpOnly cookie automatically.
          // We only need to capture the user object for Signal state.
          this.currentUser.set(response.data.user);
        })
      );
  }

  logout(): void {
    // Fire-and-forget: tell the server to invalidate the HttpOnly cookie.
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe();
    this.clearSession();
  }

  /**
   * Clears the local auth state and redirects to login.
   * Called by the error interceptor on 401 responses.
   * Intentionally does NOT make an HTTP call to avoid circular triggers.
   */
  clearSession(): void {
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  /**
   * Bootstrap method — called via provideAppInitializer() in app.config.ts.
   *
   * Step 1: Purge any stale localStorage tokens from pre-refactor sessions.
   * Step 2: Attempt to rehydrate the session from the HttpOnly cookie
   *         by calling /auth/me.
   *
   * On failure (401 / network error), the user simply isn't authenticated.
   * This resolves silently — no redirect, no error message.
   * The error interceptor skips /auth/me URLs to prevent redirect loops.
   */
  loadCurrentUser(): Promise<void> {
    // ── Migration cleanup: purge stale localStorage tokens ──
    try {
      localStorage.removeItem('auth_token');
    } catch {
      // Ignore SSR or restricted environments
    }

    return firstValueFrom(
      this.http.get<User>(`${this.apiUrl}/me`).pipe(
        tap(user => this.currentUser.set(user)),
        catchError(() => {
          this.currentUser.set(null);
          return of(null);
        })
      )
    ).then(() => void 0);
  }
}
