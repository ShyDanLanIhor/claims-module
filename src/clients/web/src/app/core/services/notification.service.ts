import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Thin wrapper over MatSnackBar for consistent success/error toasts (FRS §11.4).*/
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 4000, panelClass: 'snack-success' });
  }

  /** Non-fatal/validation problems (e.g. a 4xx business-rule rejection) — amber. */
  warn(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 6000, panelClass: 'snack-warning' });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Dismiss', { duration: 7000, panelClass: 'snack-error' });
  }
}
