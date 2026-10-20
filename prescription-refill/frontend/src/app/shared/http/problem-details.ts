import { HttpErrorResponse } from '@angular/common/http';

/** Error codes from contracts/http-api.md, plus client-side `network_error`. */
export type ProblemCode =
  | 'validation_failed'
  | 'not_found'
  | 'request_already_decided'
  | 'pending_request_exists'
  | 'concurrency_conflict'
  | 'approval_blocked'
  | 'unexpected_error'
  | 'network_error';

export interface ApiProblem {
  status: number;
  code: ProblemCode;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  requestId?: number;
  requestStatus?: string;
  /** Present on `approval_blocked`; typed by the feature that uses it. */
  evaluation?: unknown;
}

/** Normalizes any HTTP failure to an {@link ApiProblem}. Clients branch on `code` (contract). */
export function toApiProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, code: 'unexpected_error' };
  }

  if (error.status === 0) {
    return { status: 0, code: 'network_error', title: 'The server could not be reached.' };
  }

  const body = typeof error.error === 'object' && error.error !== null ? (error.error as Record<string, unknown>) : {};
  const code = typeof body['code'] === 'string' ? (body['code'] as ProblemCode) : 'unexpected_error';

  return {
    status: error.status,
    code,
    title: body['title'] as string | undefined,
    detail: body['detail'] as string | undefined,
    errors: body['errors'] as Record<string, string[]> | undefined,
    requestId: body['requestId'] as number | undefined,
    requestStatus: body['requestStatus'] as string | undefined,
    evaluation: body['evaluation'],
  };
}
