import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClaimReserves, SubmitReserveRequest, SubmitReserveResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReservesApiService {
  private readonly http = inject(HttpClient);
  private base(claimId: string) {
    return `${environment.apiBaseUrl}/claims/${claimId}/reserves`;
  }

  get(claimId: string): Observable<ClaimReserves> {
    return this.http.get<ClaimReserves>(this.base(claimId));
  }

  submit(claimId: string, request: SubmitReserveRequest): Observable<SubmitReserveResult> {
    return this.http.post<SubmitReserveResult>(this.base(claimId), request);
  }

  approve(claimId: string, transactionId: string, applyManagerOverride = false): Observable<void> {
    return this.http.post<void>(`${this.base(claimId)}/${transactionId}/approve`, {
      applyManagerOverride,
    });
  }

  reject(claimId: string, transactionId: string, rejectionReason: string): Observable<void> {
    return this.http.post<void>(`${this.base(claimId)}/${transactionId}/reject`, { rejectionReason });
  }

  retract(claimId: string, transactionId: string): Observable<void> {
    return this.http.post<void>(`${this.base(claimId)}/${transactionId}/retract`, {});
  }

  retryGlPosting(claimId: string, transactionId: string): Observable<void> {
    return this.http.post<void>(`${this.base(claimId)}/${transactionId}/retry-gl-posting`, {});
  }
}
