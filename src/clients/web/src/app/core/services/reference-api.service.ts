import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PerilCategory } from '../models/enums';
import { CauseOfLossCode, ClaimStatusInfo } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReferenceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reference`;

  causeOfLossCodes(perilCategory?: PerilCategory): Observable<CauseOfLossCode[]> {
    let params = new HttpParams();
    if (perilCategory) params = params.set('perilCategory', perilCategory);
    return this.http.get<CauseOfLossCode[]>(`${this.baseUrl}/cause-of-loss-codes`, { params });
  }

  claimStatuses(): Observable<ClaimStatusInfo[]> {
    return this.http.get<ClaimStatusInfo[]>(`${this.baseUrl}/claim-statuses`);
  }

  /** Accepted document categories for the upload picker (single source of truth lives on the backend). */
  documentTypes(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/document-types`);
  }
}
