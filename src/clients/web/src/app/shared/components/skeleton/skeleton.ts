import { Component, input } from '@angular/core';

/**
 * Lightweight shimmer placeholder shown while a component's data loads, in place of a blank area
 * (improves perceived performance). Either pass an explicit list of line widths via [widths], or a
 * line count via [rows] (defaults to full-width lines). Honours prefers-reduced-motion.
 */
@Component({
  selector: 'app-skeleton',
  template: `
    @for (width of lines(); track $index) {
      <div class="skeleton-line" [style.width]="width"></div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .skeleton-line {
        height: 1rem;
        margin: 0.6rem 0;
        border-radius: 6px;
        background: linear-gradient(
          90deg,
          var(--mat-sys-surface-container) 25%,
          var(--mat-sys-surface-container-high) 37%,
          var(--mat-sys-surface-container) 63%
        );
        background-size: 400% 100%;
        animation: skeleton-shimmer 1.4s ease infinite;
      }

      @media (prefers-reduced-motion: reduce) {
        .skeleton-line {
          animation: none;
        }
      }

      @keyframes skeleton-shimmer {
        0% {
          background-position: 100% 50%;
        }
        100% {
          background-position: 0 50%;
        }
      }
    `,
  ],
})
export class Skeleton {
  /** Number of full-width lines to render when [widths] is not supplied. */
  readonly rows = input<number>(3);

  /** Explicit per-line widths (e.g. ['40%', '70%']); overrides [rows]. */
  readonly widths = input<string[]>([]);

  protected lines(): string[] {
    const explicit = this.widths();
    return explicit.length > 0 ? explicit : Array.from({ length: this.rows() }, () => '100%');
  }
}
