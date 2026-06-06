import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClaimDocument } from '../models/models';

@Injectable({ providedIn: 'root' })
export class DocumentsApiService {
  private readonly http = inject(HttpClient);
  private base(claimId: string) {
    return `${environment.apiBaseUrl}/claims/${claimId}/documents`;
  }

  list(claimId: string): Observable<ClaimDocument[]> {
    return this.http.get<ClaimDocument[]>(this.base(claimId));
  }

  /**
   * Uploads a document, emitting HTTP progress events so the UI can show an upload progress bar.
   * Subscribe and inspect the events (UploadProgress / Response) via HttpEventType.
   */
  upload(
    claimId: string,
    file: File,
    documentType: string,
    notes?: string,
  ): Observable<HttpEvent<string>> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('documentType', documentType);
    if (notes) form.append('notes', notes);
    return this.http.post<string>(this.base(claimId), form, {
      reportProgress: true,
      observe: 'events',
    });
  }
}
