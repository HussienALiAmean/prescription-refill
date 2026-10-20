import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiProblem } from '../../../shared/http/problem-details';
import { detail, list } from '../testing/fixtures';
import { RefillRequestsApi } from './refill-requests.api';

describe('RefillRequestsApi', () => {
  let api: RefillRequestsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(RefillRequestsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists the pending queue', () => {
    let result: unknown;
    api.list().subscribe((value) => (result = value));

    const request = http.expectOne('/api/refill-requests');
    expect(request.request.method).toBe('GET');
    request.flush(list());
    expect(result).toEqual(list());
  });

  it('requests a page with status, page, size, and trimmed search', () => {
    api.list('Approved', { page: 2, pageSize: 25, search: '  okafor ' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/refill-requests');
    expect(request.request.params.keys().sort()).toEqual(['page', 'pageSize', 'search', 'status']);
    expect(request.request.params.get('status')).toBe('Approved');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');
    expect(request.request.params.get('search')).toBe('okafor');
    request.flush(list());
  });

  it('omits the Pending status and a blank search from a page request', () => {
    api.list('Pending', { page: 1, pageSize: 10, search: '   ' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/refill-requests');
    expect(request.request.params.keys().sort()).toEqual(['page', 'pageSize']);
    request.flush(list());
  });

  it('gets one request', () => {
    let result: unknown;
    api.get(5).subscribe((value) => (result = value));

    http.expectOne('/api/refill-requests/5').flush(detail());
    expect(result).toEqual(detail());
  });

  it('posts approve and deny with the note exactly as typed', () => {
    api.approve(5, '  as typed  ').subscribe();
    api.deny(5, '').subscribe();

    const approve = http.expectOne('/api/refill-requests/5/approve');
    expect(approve.request.method).toBe('POST');
    expect(approve.request.body).toEqual({ note: '  as typed  ' });
    approve.flush(detail());

    const deny = http.expectOne('/api/refill-requests/5/deny');
    expect(deny.request.body).toEqual({ note: '' });
    deny.flush(detail());
  });

  it('surfaces ProblemDetails codes as ApiProblem errors', () => {
    let problem: ApiProblem | undefined;
    api.approve(5, 'note').subscribe({ error: (error: ApiProblem) => (problem = error) });

    http
      .expectOne('/api/refill-requests/5/approve')
      .flush({ code: 'concurrency_conflict', requestId: 5, title: 'Changed' }, { status: 409, statusText: 'Conflict' });

    expect(problem?.code).toBe('concurrency_conflict');
    expect(problem?.requestId).toBe(5);
  });
});
