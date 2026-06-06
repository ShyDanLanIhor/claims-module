import { Component, computed, input } from '@angular/core';
import { ClaimStatus } from '../../../core/models/enums';

/** Colour-coded claim status chip (FRS §11.1 badge colours). */
@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [attr.data-status]="status()">{{ label() }}</span>`,
  styleUrl: './status-badge.scss',
})
export class StatusBadge {
  readonly status = input.required<ClaimStatus>();

  readonly label = computed(() => this.status().replace(/([a-z])([A-Z])/g, '$1 $2'));
}
