import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Auth Interceptor — Antigravity Protocol (Phase 1)
 *
 * With HttpOnly cookies, the browser manages token transmission automatically.
 * This interceptor ensures `withCredentials: true` is set on every outgoing
 * request so the browser includes cookies in cross-origin requests.
 *
 * Manual `Authorization` header injection is eliminated — the server reads
 * the token from the cookie, not the header.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const secureReq = req.clone({
    withCredentials: true
  });

  return next(secureReq);
};
