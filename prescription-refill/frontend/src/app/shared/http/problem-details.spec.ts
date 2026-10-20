import { HttpErrorResponse } from '@angular/common/http';
import { toApiProblem } from './problem-details';

describe('toApiProblem', () => {
  it('reads code and extension members from a ProblemDetails body', () => {
    const problem = toApiProblem(
      new HttpErrorResponse({
        status: 409,
        error: {
          title: 'The request has already been decided.',
          status: 409,
          code: 'request_already_decided',
          requestId: 6,
          requestStatus: 'Denied',
        },
      }),
    );

    expect(problem.status).toBe(409);
    expect(problem.code).toBe('request_already_decided');
    expect(problem.title).toBe('The request has already been decided.');
    expect(problem.requestId).toBe(6);
    expect(problem.requestStatus).toBe('Denied');
  });

  it('keeps field errors and the evaluation', () => {
    const evaluation = { canApprove: false };
    const problem = toApiProblem(
      new HttpErrorResponse({
        status: 400,
        error: { code: 'validation_failed', errors: { note: ['A decision note is required.'] }, evaluation },
      }),
    );

    expect(problem.errors).toEqual({ note: ['A decision note is required.'] });
    expect(problem.evaluation).toEqual(evaluation);
  });

  it('maps a network failure to network_error', () => {
    const problem = toApiProblem(new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }));
    expect(problem.code).toBe('network_error');
  });

  it('maps a response without a code to unexpected_error', () => {
    const problem = toApiProblem(new HttpErrorResponse({ status: 502, error: 'Bad gateway' }));
    expect(problem.code).toBe('unexpected_error');
    expect(problem.status).toBe(502);
  });
});
