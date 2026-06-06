import { Injectable, computed, signal } from '@angular/core';
import { UserRole } from '../models/enums';

export interface MockUser {
  id: string;
  name: string;
  role: UserRole;
}

/**
 * Mock authentication (FRS §3). Hardcoded users for the three roles; a role switcher drives the UI.
 * The auth interceptor turns the current user into a Bearer token + identity headers the API reads.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly StorageKey = 'claims.currentUserId';

  readonly users: MockUser[] = [
    { id: 'aaaaaaaa-0000-0000-0000-000000000001', name: 'Hannah Handler', role: 'Handler' },
    { id: 'bbbbbbbb-0000-0000-0000-000000000002', name: 'Sam Supervisor', role: 'Supervisor' },
    { id: 'cccccccc-0000-0000-0000-000000000003', name: 'Mia Manager', role: 'Manager' },
  ];

  private readonly _current = signal<MockUser>(this.restore());
  readonly currentUser = this._current.asReadonly();
  readonly role = computed(() => this._current().role);

  switchUser(userId: string): void {
    const user = this.users.find((u) => u.id === userId);
    if (user) {
      this._current.set(user);
      localStorage.setItem(AuthService.StorageKey, user.id);
    }
  }

  /** True if the current user's role is at least the required role (Handler < Supervisor < Manager). */
  hasRole(required: UserRole): boolean {
    const order: Record<UserRole, number> = { Handler: 0, Supervisor: 1, Manager: 2 };
    return order[this._current().role] >= order[required];
  }

  /** A stand-in JWT (the assessment only requires that a Bearer token is attached). */
  token(): string {
    const u = this._current();
    return btoa(JSON.stringify({ sub: u.id, name: u.name, role: u.role }));
  }

  private restore(): MockUser {
    const id = localStorage.getItem(AuthService.StorageKey);
    return this.users.find((u) => u.id === id) ?? this.users[0];
  }
}
