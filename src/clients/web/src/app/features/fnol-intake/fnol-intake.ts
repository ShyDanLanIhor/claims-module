import { CurrencyPipe, DatePipe, SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperModule } from '@angular/material/stepper';
import { Router } from '@angular/router';
import { debounceTime, filter, switchMap } from 'rxjs';
import {
  ASSET_TYPES,
  ClaimSeverity,
  PARTY_ROLES,
  PARTY_TYPES,
  RESERVE_COMPONENTS,
} from '../../core/models/enums';
import { CauseOfLossCode, CreateClaimRequest, Policy } from '../../core/models/models';
import { authorityHintFor } from '../../core/models/reserve-authority';
import { ClaimsApiService } from '../../core/services/claims-api.service';
import { NotificationService } from '../../core/services/notification.service';
import { PoliciesApiService } from '../../core/services/policies-api.service';
import { ReferenceApiService } from '../../core/services/reference-api.service';
import {
  ConfirmDialog,
  ConfirmDialogData,
  ConfirmDialogResult,
} from '../../shared/components/confirm-dialog/confirm-dialog';

const SEVERITIES: ClaimSeverity[] = ['Minor', 'Standard', 'Critical', 'Catastrophic'];

@Component({
  selector: 'app-fnol-intake',
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    SlicePipe,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
  ],
  templateUrl: './fnol-intake.html',
  styleUrl: './fnol-intake.scss',
})
export class FnolIntake {
  private readonly fb = inject(FormBuilder);
  private readonly claimsApi = inject(ClaimsApiService);
  private readonly policiesApi = inject(PoliciesApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly partyRoles = PARTY_ROLES;
  protected readonly partyTypes = PARTY_TYPES;
  protected readonly assetTypes = ASSET_TYPES;
  protected readonly reserveComponents = RESERVE_COMPONENTS;
  protected readonly severities = SEVERITIES;

  protected readonly causes = signal<CauseOfLossCode[]>([]);
  protected readonly causeFilter = signal('');
  protected readonly filteredCauses = computed(() => {
    const q = this.causeFilter().trim().toLowerCase();
    const all = this.causes();
    return q ? all.filter((c) => c.code.toLowerCase().includes(q) || c.name.toLowerCase().includes(q)) : all;
  });
  protected readonly policyResults = signal<Policy[]>([]);
  protected readonly selectedPolicy = signal<Policy | null>(null);
  protected readonly submitting = signal(false);
  /** Server-side validation/business errors from the create call, shown at the top of the form (FRS §11.2). */
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly today = new Date();

  // ---- Step 1: policy & loss details ---------------------------------------------------------
  protected readonly step1 = this.fb.group({
    policySearch: this.fb.nonNullable.control(''),
    unknownPolicy: this.fb.nonNullable.control(false),
    lossDate: this.fb.control<Date | null>(null, Validators.required),
    causeOfLossCode: this.fb.nonNullable.control('', Validators.required),
    lossDescription: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(20)]),
    lossLocation: this.fb.nonNullable.control(''),
    estimatedLossAmount: this.fb.control<number | null>(null),
    policeReportNumber: this.fb.nonNullable.control(''),
    severity: this.fb.control<ClaimSeverity | null>(null),
  });

  // ---- Step 2: parties & risk objects --------------------------------------------------------
  protected readonly parties = this.fb.array<ReturnType<FnolIntake['createPartyGroup']>>([]);
  protected readonly riskObjects = this.fb.array<ReturnType<FnolIntake['createRiskObjectGroup']>>([]);
  protected readonly step2 = this.fb.group({ parties: this.parties, riskObjects: this.riskObjects });

  // ---- Step 3: initial reserve & review ------------------------------------------------------
  protected readonly step3 = this.fb.group({
    addReserve: this.fb.nonNullable.control(false),
    component: this.fb.nonNullable.control(RESERVE_COMPONENTS[0]),
    amount: this.fb.control<number | null>(null),
    changeReason: this.fb.nonNullable.control(''),
  });

  private readonly reserveAmount = signal<number | null>(null);
  protected readonly authorityHint = computed(() => authorityHintFor(this.reserveAmount()));

  protected readonly claimantCount = signal(0);

  /** Loss date mirrored as a signal so the in-force badge re-computes when the DATE changes — not only
   * when the policy changes. (Reading the form control directly inside computed() is not reactive.) */
  private readonly lossDateValue = signal<Date | null>(null);

  protected readonly inForce = computed(() => {
    const policy = this.selectedPolicy();
    const lossDate = this.lossDateValue();
    if (!policy || !lossDate) return null;
    const d = lossDate.toISOString().slice(0, 10);
    return d >= policy.effectiveDate.slice(0, 10) && d <= policy.expirationDate.slice(0, 10);
  });

  constructor() {
    this.referenceApi.causeOfLossCodes().subscribe((codes) => this.causes.set(codes));

    this.step1.controls.policySearch.valueChanges
      .pipe(
        debounceTime(250),
        filter((term): term is string => typeof term === 'string' && term.length >= 2),
        switchMap((term) => this.policiesApi.search(term)),
        takeUntilDestroyed(),
      )
      .subscribe((policies) => this.policyResults.set(policies));

    this.step3.controls.amount.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((value) => this.reserveAmount.set(value));

    this.step1.controls.lossDate.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((value) => this.lossDateValue.set(value));

    this.parties.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.recountClaimants());

    this.addParty(); // start with one claimant row
  }

  protected displayPolicy = (policy: Policy | string | null): string =>
    typeof policy === 'object' && policy ? `${policy.policyNumber} — ${policy.clientName}` : (policy ?? '');

  protected onPolicySelected(policy: Policy): void {
    this.selectedPolicy.set(policy);
    this.step1.controls.unknownPolicy.setValue(false);
  }

  protected createPartyGroup() {
    return this.fb.nonNullable.group({
      partyRole: this.fb.nonNullable.control(PARTY_ROLES[0]),
      partyType: this.fb.nonNullable.control(PARTY_TYPES[0]),
      firstName: this.fb.nonNullable.control(''),
      lastName: this.fb.nonNullable.control(''),
      companyName: this.fb.nonNullable.control(''),
      email: this.fb.nonNullable.control(''),
      phone: this.fb.nonNullable.control(''),
    });
  }

  protected createRiskObjectGroup() {
    return this.fb.nonNullable.group({
      assetType: this.fb.nonNullable.control(ASSET_TYPES[0]),
      assetDescription: this.fb.nonNullable.control('', Validators.required),
      damageDescription: this.fb.nonNullable.control(''),
      assetReference: this.fb.nonNullable.control(''),
      isPrimary: this.fb.nonNullable.control(false),
    });
  }

  protected addParty(): void {
    this.parties.push(this.createPartyGroup());
  }

  protected removeParty(index: number): void {
    this.parties.removeAt(index);
  }

  protected addRiskObject(): void {
    this.riskObjects.push(this.createRiskObjectGroup());
  }

  protected removeRiskObject(index: number): void {
    this.riskObjects.removeAt(index);
  }

  protected submit(): void {
    if (this.step1.invalid) {
      this.step1.markAllAsTouched();
      return;
    }

    const warnings = this.clientWarnings();
    if (warnings.length === 0) {
      this.create();
      return;
    }

    // FRS §11.2: confirm before creating when there are non-blocking validation warnings.
    this.dialog
      .open(ConfirmDialog, {
        data: {
          title: 'Create claim with warnings?',
          message: 'This claim has non-blocking validation warnings:',
          confirmText: 'Create anyway',
          checklist: warnings,
        } satisfies ConfirmDialogData,
        width: '460px',
      })
      .afterClosed()
      .subscribe((result: ConfirmDialogResult | null) => {
        if (result) this.create();
      });
  }

  /** Client-detectable non-blocking warnings, surfaced for confirmation before submit (FRS §5.4/§11.2). */
  private clientWarnings(): string[] {
    const warnings: string[] = [];
    if (!this.selectedPolicy() && !this.step1.controls.unknownPolicy.value)
      warnings.push('No policy is linked to this claim.');
    if (this.inForce() === false)
      warnings.push("Loss date is outside the selected policy's effective period.");
    return warnings;
  }

  private create(): void {
    const v1 = this.step1.getRawValue();
    const v3 = this.step3.getRawValue();

    const request: CreateClaimRequest = {
      policyId: this.selectedPolicy()?.id ?? null,
      lossDate: v1.lossDate!.toISOString(),
      lossDescription: v1.lossDescription,
      causeOfLossCode: v1.causeOfLossCode,
      lossLocation: v1.lossLocation || null,
      estimatedLossAmount: v1.estimatedLossAmount,
      severity: v1.severity,
      policeReportNumber: v1.policeReportNumber || null,
      parties: this.parties.controls.map((c) => {
        const p = c.getRawValue();
        return {
          partyRole: p.partyRole,
          partyType: p.partyType,
          firstName: p.firstName || null,
          lastName: p.lastName || null,
          companyName: p.companyName || null,
          email: p.email || null,
          phone: p.phone || null,
        };
      }),
      riskObjects: this.riskObjects.controls.map((c) => {
        const r = c.getRawValue();
        return {
          assetType: r.assetType,
          assetDescription: r.assetDescription,
          damageDescription: r.damageDescription || null,
          assetReference: r.assetReference || null,
          isPrimary: r.isPrimary,
        };
      }),
      initialReserve:
        v3.addReserve && v3.amount
          ? { component: v3.component, amount: v3.amount, changeReason: v3.changeReason || 'Initial reserve' }
          : null,
    };

    this.serverErrors.set([]);
    this.submitting.set(true);
    this.claimsApi.create(request).subscribe({
      next: (result) => {
        this.submitting.set(false);
        const warning = result.warnings.length ? ` (${result.warnings.length} warning(s))` : '';
        this.notifications.success(`Claim ${result.claimNumber} created${warning}.`);
        void this.router.navigate(['/claims', result.claimId]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.serverErrors.set(this.extractServerErrors(err)); // FRS §11.2: echo errors at the top of the form
        this.applyFieldErrors(err); // …and inline against the relevant Step 1 control
      },
    });
  }

  private extractServerErrors(err: HttpErrorResponse): string[] {
    const body = err.error as { errors?: Record<string, string[]>; title?: string } | null;
    if (body?.errors) return Object.values(body.errors).flat().filter(Boolean);
    return body?.title ? [body.title] : [`Request failed (${err.status}).`];
  }

  /**
   * Maps server-returned per-field validation errors (PascalCase keys, §10.4) onto the matching Step 1
   * control so they appear inline beside the field, in addition to the top-of-form banner (§11.2 Step 3).
   */
  private applyFieldErrors(err: HttpErrorResponse): void {
    const errors = (err.error as { errors?: Record<string, string[]> } | null)?.errors;
    if (!errors) return;
    const map: Record<string, AbstractControl | undefined> = {
      LossDate: this.step1.controls.lossDate,
      LossDescription: this.step1.controls.lossDescription,
      CauseOfLossCode: this.step1.controls.causeOfLossCode,
      LossLocation: this.step1.controls.lossLocation,
      EstimatedLossAmount: this.step1.controls.estimatedLossAmount,
    };
    for (const [key, messages] of Object.entries(errors)) {
      map[key]?.setErrors({ server: messages.join(' ') });
    }
  }

  /** One review line per entered party: "Role — Name" (FNOL Step 3 summary). */
  protected partySummaries(): string[] {
    return this.parties.controls.map((c) => {
      const p = c.getRawValue();
      const name =
        p.partyType === 'Company'
          ? p.companyName || '—'
          : `${p.firstName} ${p.lastName}`.trim() || '—';
      return `${p.partyRole} — ${name}`;
    });
  }

  /** One review line per entered risk object: "AssetType — Description" (FNOL Step 3 summary). */
  protected riskSummaries(): string[] {
    return this.riskObjects.controls.map((c) => {
      const r = c.getRawValue();
      return `${r.assetType} — ${r.assetDescription || '—'}`;
    });
  }

  private recountClaimants(): void {
    this.claimantCount.set(
      this.parties.controls.filter((c) => c.controls.partyRole.value === 'Claimant').length,
    );
  }
}
