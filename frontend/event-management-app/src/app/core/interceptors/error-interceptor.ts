import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';

/**
 * Error Interceptor
 *
 * Handles global HTTP error responses:
 * - 401 Unauthorized → clears session state and redirects to /login.
 * - Skips /auth/me to prevent redirect loops during APP_INITIALIZER bootstrap.
 *
 * No more direct localStorage manipulation — delegates to AuthService.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error) => {
      // On 401, clear session — BUT skip auth bootstrap requests
      // to prevent redirect loops during provideAppInitializer().
      if (error.status === 401 && !req.url.includes('/auth/me')) {
        authService.clearSession();
      }
      return throwError(() => error);
    })
  );
};
