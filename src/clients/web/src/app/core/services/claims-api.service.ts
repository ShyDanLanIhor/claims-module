import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddPartyRequest,
  AuditLogEntry,
  ChangeStatusRequest,
  ClaimDetail,
  ClaimListFilter,
  ClaimSummary,
  CreateClaimRequest,
  CreateClaimResult,
  PagedResult,
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class ClaimsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/claims`;

  list(filter: ClaimListFilter): Observable<PagedResult<ClaimSummary>> {
    let params = new HttpParams()
      .set('page', filter.page ?? 1)
      .set('pageSize', filter.pageSize ?? 20);

    for (const status of filter.status ?? []) {
      params = params.append('status', status);
    }
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo);
    if (filter.assignedHandlerId) params = params.set('assignedHandlerId', filter.assignedHandlerId);
    if (filter.causeOfLossCode) params = params.set('causeOfLossCode', filter.causeOfLossCode);
    if (filter.policyId) params = params.set('policyId', filter.policyId);
    if (filter.search) params = params.set('search', filter.search);

    return this.http.get<PagedResult<ClaimSummary>>(this.baseUrl, { params });
  }

  get(id: string): Observable<ClaimDetail> {
    return this.http.get<ClaimDetail>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateClaimRequest): Observable<CreateClaimResult> {
    return this.http.post<CreateClaimResult>(this.baseUrl, request);
  }

  changeStatus(id: string, request: ChangeStatusRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, request);
  }

  updateNotes(id: string, notes: string | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/notes`, { notes });
  }

  addParty(id: string, request: AddPartyRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/${id}/parties`, request);
  }

  removeParty(id: string, partyId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/parties/${partyId}`);
  }

  getAudit(id: string, page = 1, pageSize = 25): Observable<PagedResult<AuditLogEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<AuditLogEntry>>(`${this.baseUrl}/${id}/audit`, { params });
  }
}
