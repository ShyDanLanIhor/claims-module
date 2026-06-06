import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/** Adds the Bearer token and mock-identity headers the API's CurrentUserService reads. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const user = auth.currentUser();

  return next(
    req.clone({
      setHeaders: {
        Authorization: `Bearer ${auth.token()}`,
        'X-User-Id': user.id,
        'X-User-Name': user.name,
        'X-User-Role': user.role,
      },
    }),
  );
};
