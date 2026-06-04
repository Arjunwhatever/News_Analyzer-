import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Functional HTTP interceptor that attaches the JWT Bearer token
 * (read from localStorage) to every outgoing request.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  if (token) {
    const cloned = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
    return next(cloned);
  }

  return next(req);
};
