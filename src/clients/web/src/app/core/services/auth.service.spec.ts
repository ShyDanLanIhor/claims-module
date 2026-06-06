import { AuthService } from './auth.service';

describe('AuthService (mock auth, FRS §3)', () => {
  beforeEach(() => localStorage.clear());

  it('defaults to the Handler user', () => {
    const auth = new AuthService();
    expect(auth.role()).toBe('Handler');
    expect(auth.currentUser().name).toBe('Hannah Handler');
  });

  it('switchUser updates the current user and role', () => {
    const auth = new AuthService();
    const supervisor = auth.users.find((u) => u.role === 'Supervisor')!;

    auth.switchUser(supervisor.id);

    expect(auth.currentUser().id).toBe(supervisor.id);
    expect(auth.role()).toBe('Supervisor');
  });

  it('hasRole respects the Handler < Supervisor < Manager order', () => {
    const auth = new AuthService();
    auth.switchUser(auth.users.find((u) => u.role === 'Supervisor')!.id);

    expect(auth.hasRole('Handler')).toBe(true); // a Supervisor satisfies Handler-level gates
    expect(auth.hasRole('Supervisor')).toBe(true);
    expect(auth.hasRole('Manager')).toBe(false); // but not Manager-level (reserve approval > $100k)
  });

  it('token() carries the current identity for the Bearer interceptor', () => {
    const auth = new AuthService();
    const payload = JSON.parse(atob(auth.token()));
    expect(payload).toMatchObject({ name: 'Hannah Handler', role: 'Handler' });
  });
});
