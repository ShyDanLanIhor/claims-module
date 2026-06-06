import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  /** When true, a non-empty reason is required before confirming. */
  requireReason?: boolean;
  reasonLabel?: string;
  /** Optional pre-flight checklist shown above the message (e.g. closure conditions). */
  checklist?: string[];
}

export interface ConfirmDialogResult {
  reason?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
      @if (data.checklist?.length) {
        <ul>
          @for (item of data.checklist; track item) {
            <li>{{ item }}</li>
          }
        </ul>
      }
      @if (data.requireReason) {
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ data.reasonLabel ?? 'Reason' }}</mat-label>
          <textarea matInput rows="3" [(ngModel)]="reason"></textarea>
        </mat-form-field>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">{{ data.cancelText ?? 'Cancel' }}</button>
      <button
        mat-flat-button
        color="primary"
        [disabled]="data.requireReason && !reason().trim()"
        (click)="confirm()"
      >
        {{ data.confirmText ?? 'Confirm' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `.full-width { width: 100%; margin-top: 0.5rem; }`,
})
export class ConfirmDialog {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialog, ConfirmDialogResult | null>);

  readonly reason = signal('');

  confirm(): void {
    this.dialogRef.close({ reason: this.reason().trim() || undefined });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
