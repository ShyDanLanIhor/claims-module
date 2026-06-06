import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Policy } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PoliciesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/policies`;

  search(query: string): Observable<Policy[]> {
    const params = new HttpParams().set('q', query);
    return this.http.get<Policy[]>(`${this.baseUrl}/search`, { params });
  }

  coverage(policyId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/${policyId}/coverage`);
  }
}
