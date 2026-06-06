import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

interface ApiError {
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/** Catches HTTP errors at the service boundary and surfaces a friendly snackbar (FRS §11.4). */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // A "soft" 422 (BR-C-02 policy-period warning) is converted by the owning component into an
      // acknowledge dialog, so suppress the global error toast to avoid a confusing double-surface.
      if (!isHandledSoftWarning(error)) {
        const message = buildMessage(error);
        // Surface at the appropriate severity (§11.4): client/validation problems (4xx) are warnings;
        // server faults (5xx) and unreachable-API (status 0) are errors.
        if (error.status >= 400 && error.status < 500) {
          notifications.warn(message);
        } else {
          notifications.error(message);
        }
      }
      return throwError(() => error);
    }),
  );
};

function isHandledSoftWarning(error: HttpErrorResponse): boolean {
  const body = error.error as ApiError | null;
  return error.status === 422 && body?.errors?.['acknowledgeWarnings'] != null;
}

function buildMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Cannot reach the server. Check that the API is running.';
  }

  const body = error.error as ApiError | string | null;
  if (body && typeof body === 'object') {
    const fieldMessages = body.errors
      ? Object.values(body.errors).flat().filter(Boolean)
      : [];
    if (fieldMessages.length > 0) {
      return fieldMessages.join(' ');
    }
    if (body.title) {
      return body.title;
    }
  }

  return `Request failed (${error.status}).`;
}
