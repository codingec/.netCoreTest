import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthStore } from '../state/auth.store';
import { environment } from '../../../environments/environment';

/** Adds the logged-in user's JWT to every request sent to the booking API. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthStore).getToken();
  if (!token || !req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
