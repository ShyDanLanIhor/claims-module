import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ClaimsApiService } from './claims-api.service';

describe('ClaimsApiService', () => {
  const base = `${environment.apiBaseUrl}/claims`;
  let service: ClaimsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ClaimsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() sends paging, repeated status and search as query params', () => {
    service.list({ page: 2, pageSize: 50, status: ['Open', 'Closed'], search: 'CLM-2026' }).subscribe();

    const req = http.expectOne((r) => r.url === base);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.getAll('status')).toEqual(['Open', 'Closed']);
    expect(req.request.params.get('search')).toBe('CLM-2026');
    req.flush({ items: [], total: 0, page: 2, pageSize: 50 });
  });

  it('list() applies defaults and omits absent optional filters', () => {
    service.list({}).subscribe();

    const req = http.expectOne((r) => r.url === base);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('assignedHandlerId')).toBe(false);
    req.flush({ items: [], total: 0, page: 1, pageSize: 20 });
  });

  it('changeStatus() PUTs the body to the status sub-resource', () => {
    service.changeStatus('abc', { targetStatus: 'Open' }).subscribe();

    const req = http.expectOne(`${base}/abc/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ targetStatus: 'Open' });
    req.flush(null);
  });

  it('removeParty() issues a DELETE to the party sub-resource', () => {
    service.removeParty('abc', 'p1').subscribe();

    const req = http.expectOne(`${base}/abc/parties/p1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
