import {
  AssetType,
  ClaimSeverity,
  ClaimStatus,
  PartyRole,
  PartyType,
  PerilCategory,
  PolicyStatus,
  PostingStatus,
  ReserveApprovalStatus,
  ReserveComponentStatus,
  ReserveComponentType,
  ReserveTransactionType,
  ValidationSeverity,
} from './enums';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ValidationIssue {
  code: string;
  message: string;
  severity: ValidationSeverity;
}

export interface ClaimSummary {
  id: string;
  claimNumber: string;
  policyNumber?: string;
  clientName?: string;
  lossDate: string;
  causeOfLossCode: string;
  status: ClaimStatus;
  totalReserves: number;
  assignedHandlerId?: string;
}

export interface LossEvent {
  lossDate: string;
  lossDescription: string;
  lossLocation?: string;
  causeOfLossCode: string;
  estimatedLossAmount?: number;
  reportDate: string;
  policeReportNumber?: string;
}

export interface ClaimParty {
  id: string;
  partyRole: PartyRole;
  partyType: PartyType;
  firstName?: string;
  lastName?: string;
  companyName?: string;
  email?: string;
  phone?: string;
  notes?: string;
  isActive: boolean;
}

export interface ClaimRiskObject {
  id: string;
  assetType: AssetType;
  assetDescription: string;
  damageDescription?: string;
  isPrimary: boolean;
  assetReference?: string;
}

export interface AuditLogEntry {
  id: string;
  eventType: string;
  description: string;
  oldValue?: string;
  newValue?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  correlationId?: string;
  createdAt: string;
  createdByUserId?: string;
}

export interface ReserveComponentSummary {
  id: string;
  component: ReserveComponentType;
  currentAmount: number;
  pendingAmount: number;
  status: ReserveComponentStatus;
}

export interface ClaimDocumentMetadata {
  id: string;
  documentType: string;
  documentName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  uploadedByUserId?: string;
  notes?: string;
}

export interface ClaimDocument extends ClaimDocumentMetadata {
  downloadUrl: string;
}

export interface ClaimDetail {
  id: string;
  claimNumber: string;
  policyId?: string;
  policyNumber?: string;
  clientName?: string;
  status: ClaimStatus;
  severity?: ClaimSeverity;
  reportedDate: string;
  assignedHandlerId?: string;
  closedAt?: string;
  closureReason?: string;
  notes?: string;
  managerOverrideApplied: boolean;
  lossEvent?: LossEvent;
  parties: ClaimParty[];
  riskObjects: ClaimRiskObject[];
  reserves: ReserveComponentSummary[];
  documents: ClaimDocumentMetadata[];
  allowedNextStatuses: ClaimStatus[];
  recentAudit: AuditLogEntry[];
}

export interface CreateClaimResult {
  claimId: string;
  claimNumber: string;
  warnings: ValidationIssue[];
}

export interface ReserveTransaction {
  id: string;
  reserveComponentId: string;
  component: ReserveComponentType;
  transactionType: ReserveTransactionType;
  amount: number;
  previousBalance: number;
  newBalance: number;
  approvalStatus: ReserveApprovalStatus;
  postingStatus: PostingStatus;
  changeReason: string;
  changeSequence: number;
  submittedByUserId?: string;
  approvedByUserId?: string;
  rejectedByUserId?: string;
  rejectionReason?: string;
  idempotencyKey: string;
  createdAt: string;
}

export interface ClaimReserves {
  components: ReserveComponentSummary[];
  history: ReserveTransaction[];
  totalApproved: number;
  totalPending: number;
}

export interface SubmitReserveResult {
  transactionId: string;
  reserveComponentId: string;
  approvalStatus: ReserveApprovalStatus;
  autoApproved: boolean;
}

export interface Policy {
  id: string;
  policyNumber: string;
  clientName: string;
  effectiveDate: string;
  expirationDate: string;
  status: PolicyStatus;
  coverageTypes: string[];
}

export interface CauseOfLossCode {
  id: string;
  code: string;
  name: string;
  perilCategory: PerilCategory;
  sortOrder: number;
}

export interface ClaimStatusInfo {
  status: ClaimStatus;
  allowedNextStatuses: ClaimStatus[];
}

// ---- Request payloads ------------------------------------------------------------------------

export interface CreateClaimPartyInput {
  partyRole: PartyRole;
  partyType: PartyType;
  firstName?: string | null;
  lastName?: string | null;
  companyName?: string | null;
  email?: string | null;
  phone?: string | null;
  notes?: string | null;
}

export interface CreateClaimRiskObjectInput {
  assetType: AssetType;
  assetDescription: string;
  damageDescription?: string | null;
  isPrimary: boolean;
  assetReference?: string | null;
}

export interface InitialReserveInput {
  component: ReserveComponentType;
  amount: number;
  changeReason: string;
}

export interface CreateClaimRequest {
  policyId?: string | null;
  lossDate: string;
  lossDescription: string;
  causeOfLossCode: string;
  lossLocation?: string | null;
  estimatedLossAmount?: number | null;
  policeReportNumber?: string | null;
  severity?: ClaimSeverity | null;
  assignedHandlerId?: string | null;
  notes?: string | null;
  parties: CreateClaimPartyInput[];
  riskObjects: CreateClaimRiskObjectInput[];
  initialReserve?: InitialReserveInput | null;
}

export interface ChangeStatusRequest {
  targetStatus: ClaimStatus;
  reason?: string | null;
  acknowledgeWarnings?: boolean;
}

export interface AddPartyRequest {
  partyRole: PartyRole;
  partyType: PartyType;
  firstName?: string | null;
  lastName?: string | null;
  companyName?: string | null;
  email?: string | null;
  phone?: string | null;
  notes?: string | null;
}

export interface SubmitReserveRequest {
  component: ReserveComponentType;
  amount: number;
  changeReason: string;
  transactionType?: ReserveTransactionType;
  applyManagerOverride?: boolean;
}

export interface ClaimListFilter {
  status?: ClaimStatus[];
  dateFrom?: string;
  dateTo?: string;
  assignedHandlerId?: string;
  causeOfLossCode?: string;
  policyId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
