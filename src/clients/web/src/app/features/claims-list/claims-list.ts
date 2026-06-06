import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { ClaimStatus } from '../../core/models/enums';
import { CauseOfLossCode, ClaimSummary, PagedResult } from '../../core/models/models';
import { ClaimsApiService } from '../../core/services/claims-api.service';
import { AuthService, MockUser } from '../../core/services/auth.service';
import { ReferenceApiService } from '../../core/services/reference-api.service';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { StatusBadge } from '../../shared/components/status-badge/status-badge';

const ALL_STATUSES: ClaimStatus[] = [
  'Draft',
  'Open',
  'UnderInvestigation',
  'PendingPayment',
  'Closed',
  'Reopened',
  'Withdrawn',
];

@Component({
  selector: 'app-claims-list',
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    Skeleton,
    StatusBadge,
  ],
  templateUrl: './claims-list.html',
  styleUrl: './claims-list.scss',
})
export class ClaimsList {
  private readonly claimsApi = inject(ClaimsApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  protected readonly statuses = ALL_STATUSES;
  protected readonly handlers: MockUser[] = this.auth.users;
  protected readonly handlerCtrl = this.fb.control<string | MockUser | null>('');
  protected readonly filteredHandlers = signal<MockUser[]>(this.handlers);
  protected readonly causes = signal<CauseOfLossCode[]>([]);
  protected readonly page = signal<PagedResult<ClaimSummary> | null>(null);
  protected readonly loading = signal(false);

  protected readonly displayedColumns = [
    'claimNumber',
    'policyNumber',
    'clientName',
    'lossDate',
    'causeOfLossCode',
    'assignedHandler',
    'status',
    'totalReserves',
  ];

  protected readonly filterForm = this.fb.group({
    search: this.fb.control(''),
    status: this.fb.control<ClaimStatus[]>([]),
    causeOfLossCode: this.fb.control<string | null>(null),
    assignedHandlerId: this.fb.control<string | null>(null),
    dateFrom: this.fb.control<Date | null>(null),
    dateTo: this.fb.control<Date | null>(null),
  });

  /** Resolves an assigned-handler id to a display name (mock users); falls back to a dash. */
  protected handlerName(id?: string): string {
    if (!id) return '—';
    return this.handlers.find((u) => u.id === id)?.name ?? id;
  }

  /** Renders the selected handler's name in the autocomplete input. */
  protected displayHandler = (value: string | MockUser | null): string =>
    value && typeof value === 'object' ? value.name : (value ?? '');

  protected clearHandler(): void {
    this.handlerCtrl.setValue('');
  }

  private pageIndex = 0;
  private pageSize = 20;

  constructor() {
    this.referenceApi.causeOfLossCodes().subscribe((codes) => this.causes.set(codes));

    this.filterForm.valueChanges
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe(() => {
        this.pageIndex = 0;
        this.load();
      });

    // Assigned-handler "text search": filter the known mock handlers as the user types; selecting one
    // sets assignedHandlerId (drives the query); clearing the text removes the filter (FRS §11.1).
    this.handlerCtrl.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      if (value && typeof value === 'object') {
        this.filterForm.controls.assignedHandlerId.setValue(value.id);
        this.filteredHandlers.set(this.handlers);
      } else {
        const q = (value ?? '').toString().trim().toLowerCase();
        this.filteredHandlers.set(
          q ? this.handlers.filter((h) => h.name.toLowerCase().includes(q)) : this.handlers,
        );
        if (q === '') this.filterForm.controls.assignedHandlerId.setValue(null);
      }
    });

    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.load();
  }

  protected openClaim(id: string): void {
    void this.router.navigate(['/claims', id]);
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      search: '',
      status: [],
      causeOfLossCode: null,
      assignedHandlerId: null,
      dateFrom: null,
      dateTo: null,
    });
    this.handlerCtrl.setValue('', { emitEvent: false });
    this.filteredHandlers.set(this.handlers);
  }

  private load(): void {
    const value = this.filterForm.getRawValue();
    this.loading.set(true);
    this.claimsApi
      .list({
        search: value.search || undefined,
        status: value.status?.length ? value.status : undefined,
        causeOfLossCode: value.causeOfLossCode || undefined,
        assignedHandlerId: value.assignedHandlerId || undefined,
        dateFrom: value.dateFrom ? value.dateFrom.toISOString() : undefined,
        dateTo: value.dateTo ? value.dateTo.toISOString() : undefined,
        page: this.pageIndex + 1,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.page.set(result);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
