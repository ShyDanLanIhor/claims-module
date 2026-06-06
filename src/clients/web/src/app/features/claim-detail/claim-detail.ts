import { HttpEventType } from '@angular/common/http';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  ClaimStatus,
  PARTY_ROLES,
  PARTY_TYPES,
  PartyRole,
  PartyType,
  RESERVE_COMPONENTS,
} from '../../core/models/enums';
import {
  AuditLogEntry,
  ClaimDetail as ClaimDetailModel,
  ClaimDocument,
  ClaimParty,
  ClaimReserves,
  ReserveTransaction,
} from '../../core/models/models';
import { authorityHintFor } from '../../core/models/reserve-authority';
import { ClaimsApiService } from '../../core/services/claims-api.service';
import { AuthService } from '../../core/services/auth.service';
import { DocumentsApiService } from '../../core/services/documents-api.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReferenceApiService } from '../../core/services/reference-api.service';
import { ReservesApiService } from '../../core/services/reserves-api.service';
import {
  ConfirmDialog,
  ConfirmDialogData,
  ConfirmDialogResult,
} from '../../shared/components/confirm-dialog/confirm-dialog';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { StatusBadge } from '../../shared/components/status-badge/status-badge';

@Component({
  selector: 'app-claim-detail',
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    MatTabsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatMenuModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    MatDividerModule,
    MatProgressBarModule,
    Skeleton,
    StatusBadge,
  ],
  templateUrl: './claim-detail.html',
  styleUrl: './claim-detail.scss',
})
export class ClaimDetail {
  readonly id = input.required<string>();

  private readonly claimsApi = inject(ClaimsApiService);
  private readonly reservesApi = inject(ReservesApiService);
  private readonly documentsApi = inject(DocumentsApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  protected readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly reserveComponents = RESERVE_COMPONENTS;
  protected readonly partyRoles = PARTY_ROLES;
  protected readonly partyTypes = PARTY_TYPES;
  protected readonly documentTypes = signal<string[]>([]);
  protected readonly selectedDocumentType = signal<string>('Other');

  /** Upload progress 0–100 while an upload is in flight; null when idle. */
  protected readonly uploadProgress = signal<number | null>(null);

  /** True while a mutating action is in flight, used to disable its button (FRS §11.4). */
  protected readonly actionPending = signal(false);

  protected readonly detail = signal<ClaimDetailModel | null>(null);
  protected readonly reserves = signal<ClaimReserves | null>(null);
  protected readonly documents = signal<ClaimDocument[]>([]);
  protected readonly audit = signal<AuditLogEntry[]>([]);

  protected readonly reserveColumns = [
    'createdAt',
    'component',
    'transactionType',
    'amount',
    'approvalStatus',
    'postingStatus',
    'submittedBy',
    'approvedBy',
    'actions',
  ];
  protected readonly auditColumns = ['createdAt', 'eventType', 'description', 'user', 'related'];

  /** Active tab index; lets an audit "related entity" link jump to the relevant tab (FRS §11.3). */
  protected readonly selectedTab = signal(0);

  private static readonly RELATED_TAB: Record<string, number> = {
    ClaimParty: 1,
    ReserveHistory: 2,
    ClaimDocument: 3,
  };

  protected relatedTabIndex(type: string | null | undefined): number | null {
    return type ? ClaimDetail.RELATED_TAB[type] ?? null : null;
  }

  protected goToRelated(type: string | null | undefined): void {
    const idx = this.relatedTabIndex(type);
    if (idx !== null) this.selectedTab.set(idx);
  }

  protected readonly addReserveForm = this.fb.group({
    component: this.fb.nonNullable.control(RESERVE_COMPONENTS[0]),
    amount: this.fb.control<number | null>(null, [Validators.required]),
    changeReason: this.fb.nonNullable.control('', Validators.required),
  });

  /** Editable claim notes (FRS §11.3); capped to match the backend validator. */
  protected readonly notesControl = this.fb.nonNullable.control('', Validators.maxLength(2000));

  /** Toggles the slide-in Add Reserve panel (FRS §11.3 Tab 3). */
  protected readonly showAddReserve = signal(false);
  protected readonly showAddParty = signal(false);
  protected readonly addPartyForm = this.fb.group({
    partyRole: this.fb.nonNullable.control<PartyRole>('Claimant'),
    partyType: this.fb.nonNullable.control<PartyType>('Person'),
    firstName: this.fb.nonNullable.control(''),
    lastName: this.fb.nonNullable.control(''),
    companyName: this.fb.nonNullable.control(''),
    email: this.fb.nonNullable.control(''),
    phone: this.fb.nonNullable.control(''),
  });

  protected readonly auditTotal = signal(0);
  protected readonly auditPageIndex = signal(0);
  protected readonly auditPageSize = signal(25);

  /** Real-time reserve-authority indicator for the Add Reserve form (FRS §11.3). */
  private readonly reserveAmount = signal<number | null>(null);
  protected readonly reserveAuthorityHint = computed(() => authorityHintFor(this.reserveAmount()));

  constructor() {
    this.referenceApi.documentTypes().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((types) => this.documentTypes.set(types));
    this.addReserveForm.controls.amount.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((value) => this.reserveAmount.set(value));
    // Load when the routed id changes — NOT inside an effect (which would also track the audit-page
    // signals read by loadAll and re-fire on pagination). FE-02.
    toObservable(this.id).pipe(takeUntilDestroyed()).subscribe((id) => this.loadAll(id));
  }

  protected canApproveReserves(): boolean {
    return this.auth.hasRole('Supervisor');
  }

  protected isOwnPending(txn: ReserveTransaction): boolean {
    return txn.approvalStatus === 'PendingApproval' && txn.submittedByUserId === this.auth.currentUser().id;
  }

  /** Resolves an assigned-handler id to a mock-user display name. */
  protected handlerName(id?: string): string {
    if (!id) return 'Unassigned';
    return this.auth.users.find((u) => u.id === id)?.name ?? id;
  }

  /** Resolves any user id to a mock-user display name ('—' when unset). */
  protected userName(id?: string): string {
    if (!id) return '—';
    return this.auth.users.find((u) => u.id === id)?.name ?? id;
  }

  protected copyClaimNumber(claimNumber: string): void {
    const copy = navigator.clipboard?.writeText(claimNumber);
    if (copy) {
      copy
        .then(() => this.notifications.success('Claim number copied.'))
        .catch(() => this.notifications.error('Could not copy claim number.'));
    } else {
      this.notifications.error('Clipboard not available.');
    }
  }

  protected transition(target: ClaimStatus): void {
    // CC-04: closing a claim that still has open reserves (CurrentAmount > 0) requires a justification
    // note. Surface (and require) the reason field in that case so the close can actually be confirmed —
    // otherwise the API rejects it 422 and the dialog gives the user no way to supply the justification.
    const hasOpenReserves = (this.reserves()?.components ?? []).some((c) => c.currentAmount > 0);
    const requireReason =
      target === 'Withdrawn' || target === 'Reopened' || (target === 'Closed' && hasOpenReserves);
    const data: ConfirmDialogData = {
      title: `Transition to ${target}`,
      message: `Change this claim's status to ${target}?`,
      confirmText: target,
      requireReason,
      reasonLabel: target === 'Closed' ? 'Justification for closing with open reserves' : 'Reason',
      checklist:
        target === 'Closed'
          ? ['No reserves pending approval', 'At least one claimant', 'Open reserves require a justification']
          : undefined,
    };

    this.dialog
      .open(ConfirmDialog, { data, width: '460px' })
      .afterClosed()
      .subscribe((result: ConfirmDialogResult | null) => {
        if (!result) return;
        this.applyTransition(target, result.reason, false);
      });
  }

  private applyTransition(target: ClaimStatus, reason: string | null | undefined, acknowledgeWarnings: boolean): void {
    this.claimsApi.changeStatus(this.id(), { targetStatus: target, reason, acknowledgeWarnings }).subscribe({
      next: () => {
        this.notifications.success(`Claim transitioned to ${target}.`);
        this.loadAll(this.id());
      },
      // BR-C-02: the API blocks Draft→Open while the loss date is outside the policy period until the
      // warning is acknowledged. Surface a confirmation and retry with the acknowledgement.
      error: (err: unknown) => {
        if (acknowledgeWarnings || !this.isPolicyPeriodWarning(err)) return;
        this.dialog
          .open(ConfirmDialog, {
            data: {
              title: 'Open despite warning?',
              message: 'The loss date is outside the policy effective period (BR-C-02).',
              confirmText: 'Acknowledge & open',
            } satisfies ConfirmDialogData,
            width: '460px',
          })
          .afterClosed()
          .subscribe((confirmed: ConfirmDialogResult | null) => {
            if (confirmed) this.applyTransition(target, reason, true);
          });
      },
    });
  }

  private isPolicyPeriodWarning(err: unknown): boolean {
    const e = err as { status?: number; error?: { errors?: Record<string, unknown> } } | null;
    return e?.status === 422 && e?.error?.errors?.['acknowledgeWarnings'] != null;
  }

  protected submitReserve(): void {
    if (this.addReserveForm.invalid) {
      this.addReserveForm.markAllAsTouched();
      return;
    }
    const value = this.addReserveForm.getRawValue();
    this.busy(
      this.reservesApi.submit(this.id(), {
        component: value.component,
        amount: value.amount!,
        changeReason: value.changeReason,
        applyManagerOverride: this.auth.hasRole('Manager'),
      }),
      (result) => {
        this.notifications.success(
          result.autoApproved ? 'Reserve auto-approved.' : 'Reserve submitted for approval.',
        );
        this.addReserveForm.reset({ component: RESERVE_COMPONENTS[0], amount: null, changeReason: '' });
        this.showAddReserve.set(false);
        this.reloadReserves();
      },
    );
  }

  protected approve(txn: ReserveTransaction): void {
    this.busy(this.reservesApi.approve(this.id(), txn.id, this.auth.hasRole('Manager')), () => {
      this.notifications.success('Reserve approved.');
      this.reloadReserves();
    });
  }

  protected reject(txn: ReserveTransaction): void {
    this.dialog
      .open(ConfirmDialog, {
        data: {
          title: 'Reject reserve',
          message: 'Provide a reason for rejecting this reserve.',
          confirmText: 'Reject',
          requireReason: true,
          reasonLabel: 'Rejection reason',
        } satisfies ConfirmDialogData,
        width: '460px',
      })
      .afterClosed()
      .subscribe((result: ConfirmDialogResult | null) => {
        if (!result?.reason) return;
        this.busy(this.reservesApi.reject(this.id(), txn.id, result.reason), () => {
          this.notifications.success('Reserve rejected.');
          this.reloadReserves();
        });
      });
  }

  protected retract(txn: ReserveTransaction): void {
    this.busy(this.reservesApi.retract(this.id(), txn.id), () => {
      this.notifications.success('Reserve retracted.');
      this.reloadReserves();
    });
  }

  /** A failed GL posting can be retried by a Supervisor/Manager (FRS §11.3). */
  protected canRetryGlPosting(txn: ReserveTransaction): boolean {
    return txn.postingStatus === 'Failed' && this.auth.hasRole('Supervisor');
  }

  protected retryGlPosting(txn: ReserveTransaction): void {
    this.busy(this.reservesApi.retryGlPosting(this.id(), txn.id), () => {
      this.notifications.success('GL posting retry queued.');
      this.reloadReserves();
    });
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadProgress.set(0);
    this.documentsApi.upload(this.id(), file, this.selectedDocumentType()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (e) => {
        if (e.type === HttpEventType.UploadProgress && e.total) {
          this.uploadProgress.set(Math.round((100 * e.loaded) / e.total));
        } else if (e.type === HttpEventType.Response) {
          this.uploadProgress.set(null);
          this.notifications.success(`Uploaded ${file.name}.`);
          input.value = '';
          this.documentsApi.list(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((docs) => this.documents.set(docs));
          // A successful upload writes a DOCUMENT_UPLOADED audit entry — refresh the audit log (§14).
          this.auditPageIndex.set(0);
          this.loadAudit(this.id());
        }
      },
      error: () => {
        this.uploadProgress.set(null);
        input.value = '';
      },
    });
  }

  protected download(doc: ClaimDocument): void {
    window.open(doc.downloadUrl, '_blank');
  }

  protected saveNotes(): void {
    const notes = this.notesControl.value.trim() || null;
    this.busy(this.claimsApi.updateNotes(this.id(), notes), () => {
      this.notifications.success('Notes saved.');
      this.detail.update((d) => (d ? { ...d, notes: notes ?? undefined } : d));
      this.notesControl.markAsPristine();
    });
  }

  protected addPartyRow(): void {
    const v = this.addPartyForm.getRawValue();
    this.busy(
      this.claimsApi.addParty(this.id(), {
        partyRole: v.partyRole,
        partyType: v.partyType,
        firstName: v.firstName || null,
        lastName: v.lastName || null,
        companyName: v.companyName || null,
        email: v.email || null,
        phone: v.phone || null,
        notes: null,
      }),
      () => {
        this.notifications.success('Party added.');
        this.showAddParty.set(false);
        this.addPartyForm.reset({
          partyRole: 'Claimant',
          partyType: 'Person',
          firstName: '',
          lastName: '',
          companyName: '',
          email: '',
          phone: '',
        });
        this.reloadDetail();
      },
    );
  }

  protected removeParty(party: ClaimParty): void {
    this.busy(this.claimsApi.removeParty(this.id(), party.id), () => {
      this.notifications.success('Party removed.');
      this.reloadDetail();
    });
  }

  /** The last active Claimant cannot be removed (the backend also enforces this with a 422). */
  protected canRemoveParty(party: ClaimParty): boolean {
    if (!party.isActive) return false;
    const activeClaimants =
      this.detail()?.parties.filter((p) => p.isActive && p.partyRole === 'Claimant').length ?? 0;
    return !(party.partyRole === 'Claimant' && activeClaimants <= 1);
  }

  protected onAuditPage(event: PageEvent): void {
    this.auditPageIndex.set(event.pageIndex);
    this.auditPageSize.set(event.pageSize);
    this.loadAudit(this.id());
  }

  /** Runs a mutating call with the action buttons disabled until it completes (FRS §11.4). */
  private busy<T>(source: Observable<T>, onSuccess: (value: T) => void): void {
    this.actionPending.set(true);
    source.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (value) => {
        this.actionPending.set(false);
        onSuccess(value);
      },
      error: () => this.actionPending.set(false),
    });
  }

  private loadAll(id: string): void {
    this.claimsApi.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((d) => {
      this.detail.set(d);
      this.notesControl.setValue(d.notes ?? '');
      this.notesControl.markAsPristine();
    });
    // Fetch reserves directly (not via reloadReserves, which also re-GETs the claim) so the claim
    // detail is fetched exactly once per load — no duplicate concurrent GET (FE-05).
    this.reservesApi.get(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((r) => this.reserves.set(r));
    this.documentsApi.list(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((docs) => this.documents.set(docs));
    this.auditPageIndex.set(0);
    this.loadAudit(id);
  }

  private loadAudit(id: string): void {
    this.claimsApi.getAudit(id, this.auditPageIndex() + 1, this.auditPageSize()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((page) => {
      this.audit.set(page.items);
      this.auditTotal.set(page.totalCount);
    });
  }

  private reloadDetail(): void {
    this.claimsApi.get(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((d) => this.detail.set(d));
    this.auditPageIndex.set(0);
    this.loadAudit(this.id());
  }

  private reloadReserves(): void {
    this.reservesApi.get(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((r) => this.reserves.set(r));
    this.claimsApi.get(this.id()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((d) => this.detail.set(d));
    // Every reserve action (create/approve/reject/retract/GL posting) appends audit entries, so refresh
    // the audit log too — otherwise Tab 5 shows a stale list until the page is reloaded (§14, §11.3).
    this.auditPageIndex.set(0);
    this.loadAudit(this.id());
  }
}
