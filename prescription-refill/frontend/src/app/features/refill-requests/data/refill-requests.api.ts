import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { toApiProblem } from '../../../shared/http/problem-details';
import { ListQuery, RefillRequestDetail, RefillRequestList, RequestStatus } from './models';

/**
 * The only code that calls the refill request API (constitution Principle VII). Failures are
 * re-thrown as `ApiProblem` so callers branch on the contract's `code`.
 */
@Injectable({ providedIn: 'root' })
export class RefillRequestsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/refill-requests';

  /**
   * Pending is the API default, so it is requested without a status parameter. Without `query`
   * the complete list is returned; with it, one page (003 contracts/http-api.md).
   */
  list(status: RequestStatus = 'Pending', query?: ListQuery): Observable<RefillRequestList> {
    const params: Record<string, string | number> = status === 'Pending' ? {} : { status };
    if (query) {
      params['page'] = query.page;
      params['pageSize'] = query.pageSize;
      const search = query.search?.trim();
      if (search) {
        params['search'] = search;
      }
    }
    return this.http.get<RefillRequestList>(this.baseUrl, { params }).pipe(catchError(fail));
  }

  get(id: number): Observable<RefillRequestDetail> {
    return this.http.get<RefillRequestDetail>(`${this.baseUrl}/${id}`).pipe(catchError(fail));
  }

  /** The note is sent exactly as typed; the server owns the note rule (FR-010). */
  approve(id: number, note: string): Observable<RefillRequestDetail> {
    return this.http.post<RefillRequestDetail>(`${this.baseUrl}/${id}/approve`, { note }).pipe(catchError(fail));
  }

  deny(id: number, note: string): Observable<RefillRequestDetail> {
    return this.http.post<RefillRequestDetail>(`${this.baseUrl}/${id}/deny`, { note }).pipe(catchError(fail));
  }
}

const fail = (error: unknown) => throwError(() => toApiProblem(error));
