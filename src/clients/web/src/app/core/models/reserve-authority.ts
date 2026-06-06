/**
 * Reserve-authority thresholds and the real-time approval-tier hint (FRS §6.3 / BR-R-02). The backend
 * is authoritative; this only drives the FNOL and Add-Reserve UI indicators. Shared so both screens
 * stay in lockstep and a threshold change happens in exactly one place.
 */
export const AUTO_APPROVAL_LIMIT = 10_000;
export const SUPERVISOR_LIMIT = 100_000;

export interface AuthorityHint {
  icon: string;
  text: string;
  level: 'ok' | 'warn';
}

export function authorityHintFor(amount: number | null): AuthorityHint | null {
  if (amount === null || amount === 0) return null;

  const magnitude = Math.abs(amount);
  if (magnitude <= AUTO_APPROVAL_LIMIT) return { icon: 'check_circle', text: 'Auto-approved (≤ $10,000)', level: 'ok' };
  if (magnitude <= SUPERVISOR_LIMIT) return { icon: 'warning', text: 'Supervisor approval required', level: 'warn' };
  return { icon: 'warning', text: 'Manager approval required', level: 'warn' };
}
