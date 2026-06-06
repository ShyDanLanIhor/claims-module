import { Injectable, computed, signal } from '@angular/core';

/** Tracks the number of in-flight HTTP requests so the shell can show a progress bar. */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly _active = signal(0);
  readonly isLoading = computed(() => this._active() > 0);

  begin(): void {
    this._active.update((n) => n + 1);
  }

  end(): void {
    this._active.update((n) => Math.max(0, n - 1));
  }
}
