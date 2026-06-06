// String-literal unions mirroring the backend enums (serialised as strings by the API).

export type ClaimStatus =
  | 'Draft'
  | 'Open'
  | 'UnderInvestigation'
  | 'PendingPayment'
  | 'Closed'
  | 'Reopened'
  | 'Withdrawn';

export type ClaimSeverity = 'Catastrophic' | 'Critical' | 'Standard' | 'Minor';

export type PartyRole = 'Claimant' | 'Insured' | 'ThirdParty' | 'Witness' | 'Attorney';

export type PartyType = 'Person' | 'Company';

export type AssetType = 'Vehicle' | 'Property' | 'Person' | 'Equipment' | 'Other';

export type ReserveComponentType = 'Indemnity' | 'Expense' | 'ALAE' | 'SubrogationRecoverable';

export type ReserveComponentStatus = 'Active' | 'Closed';

export type ReserveApprovalStatus =
  | 'AutoApproved'
  | 'PendingApproval'
  | 'Approved'
  | 'Rejected'
  | 'Cancelled';

export type ReserveTransactionType = 'Add' | 'Adjust' | 'Reverse';

export type PostingStatus = 'Pending' | 'Posted' | 'Failed' | 'Cancelled';

export type PerilCategory =
  | 'Property'
  | 'Auto'
  | 'Liability'
  | 'Weather'
  | 'Equipment'
  | 'Crime'
  | 'General';

export type PolicyStatus = 'Active' | 'Expired' | 'Cancelled';

export type UserRole = 'Handler' | 'Supervisor' | 'Manager';

export type ValidationSeverity = 'Critical' | 'Warning';

// Document categories are NOT hardcoded here — they are fetched at runtime from
// GET /api/reference/document-types (single source of truth = Domain DocumentTypes on the backend).

export const PARTY_ROLES: PartyRole[] = ['Claimant', 'Insured', 'ThirdParty', 'Witness', 'Attorney'];
export const PARTY_TYPES: PartyType[] = ['Person', 'Company'];
export const ASSET_TYPES: AssetType[] = ['Vehicle', 'Property', 'Person', 'Equipment', 'Other'];
export const RESERVE_COMPONENTS: ReserveComponentType[] = [
  'Indemnity',
  'Expense',
  'ALAE',
  'SubrogationRecoverable',
];
