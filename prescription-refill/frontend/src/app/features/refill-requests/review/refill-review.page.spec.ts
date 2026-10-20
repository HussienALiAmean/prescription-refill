import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { ApiProblem } from '../../../shared/http/problem-details';
import { RefillRequestDetail } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { blockedEvaluation, decided, detail, evaluation } from '../testing/fixtures';
import { RefillReviewPage } from './refill-review.page';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';

describe('RefillReviewPage', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;
  let events: FakeRefillEvents;
  let fixture: ComponentFixture<RefillReviewPage>;
  let el: HTMLElement;

  const problem = (code: ApiProblem['code'], extra: Partial<ApiProblem> = {}) =>
    throwError(() => ({ status: 409, code, ...extra }) as ApiProblem);
  const approveButton = () => el.querySelector('button.btn-primary') as HTMLButtonElement;
  const denyButton = () => el.querySelector('button.btn-danger') as HTMLButtonElement;
  const refreshButton = () =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Refresh')) as HTMLButtonElement;

  function render(get: Observable<RefillRequestDetail> = of(detail())) {
    api.get.and.returnValue(get);
    fixture = TestBed.createComponent(RefillReviewPage);
    fixture.componentRef.setInput('id', '5');
    fixture.detectChanges();
    el = fixture.nativeElement;
  }

  function typeNote(text: string) {
    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = text;
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    events = new FakeRefillEvents();
    TestBed.configureTestingModule({
      imports: [RefillReviewPage],
      providers: [provideRouter([]), { provide: RefillRequestsApi, useValue: api }, provideFakeRefillEvents(events)],
    });
  });

  it('shows a loading state while the request loads', () => {
    render(new Subject<RefillRequestDetail>());

    expect(el.textContent).toContain('Loading');
  });

  it('shows not found for a missing request', () => {
    render(throwError(() => ({ status: 404, code: 'not_found' }) as ApiProblem));

    expect(el.textContent).toContain('not found');
  });

  it('shows a network error with retry', () => {
    api.get.and.returnValues(throwError(() => ({ status: 0, code: 'network_error' }) as ApiProblem), of(detail()));
    fixture = TestBed.createComponent(RefillReviewPage);
    fixture.componentRef.setInput('id', '5');
    fixture.detectChanges();
    el = fixture.nativeElement;

    (el.querySelector('app-error-state button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledTimes(2);
    expect(el.textContent).toContain('Sam Patel');
  });

  it('loads on open and shows the profile, warnings, and the evaluation time with a staleness notice', () => {
    render();

    expect(api.get).toHaveBeenCalledWith(5);
    expect(el.textContent).toContain('Sam Patel');
    expect(el.textContent).toContain('Glimora');
    expect(el.textContent).toContain('synthetic demo interaction');
    expect(el.textContent).toContain('may be out of date');
    expect(el.querySelector(`time[datetime="${detail().evaluation!.evaluatedAt}"]`)).not.toBeNull();
  });

  it('refreshes the evaluation from the server on demand', () => {
    render();
    api.get.and.returnValue(of(detail({ evaluation: blockedEvaluation() })));

    refreshButton().click();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledTimes(2);
    expect(el.textContent).toContain('Less than 80% of the 30-day supply');
  });

  it('disables Approve and keeps Deny when the latest evaluation blocks approval', () => {
    render(of(detail({ evaluation: blockedEvaluation() })));

    expect(approveButton().disabled).toBeTrue();
    expect(denyButton().disabled).toBeFalse();
  });

  it('sends the note exactly as typed and shows the decided read-only view on success', () => {
    render();
    api.approve.and.returnValue(of(decided('Approved')));

    typeNote('  Stable.  ');
    approveButton().click();
    fixture.detectChanges();

    expect(api.approve).toHaveBeenCalledWith(5, '  Stable.  ');
    expect(el.textContent).toContain('Request approved');
    expect(el.textContent).toContain('read-only');
    expect(el.querySelector('textarea')).toBeNull();
  });

  it('on 422 replaces the evaluation from the response, disables Approve, keeps Deny and the note', () => {
    render();
    api.approve.and.returnValue(
      throwError(() => ({ status: 422, code: 'approval_blocked', evaluation: blockedEvaluation() }) as ApiProblem),
    );

    typeNote('Please approve.');
    approveButton().click();
    fixture.detectChanges();

    expect(el.textContent).toContain('Less than 80% of the 30-day supply');
    expect(el.textContent).toContain('You can still deny');
    expect(approveButton().disabled).toBeTrue();
    expect(denyButton().disabled).toBeFalse();
    expect((el.querySelector('textarea') as HTMLTextAreaElement).value).toBe('Please approve.');
    expect(el.textContent).not.toContain('Request approved');
  });

  it('on a concurrency conflict shows no success, explains, reloads, and keeps the note', () => {
    render();
    api.approve.and.returnValue(problem('concurrency_conflict', { requestId: 5 }));
    api.get.and.returnValue(of(detail({ evaluation: evaluation() })));

    typeNote('My note.');
    approveButton().click();
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledTimes(2);
    expect(el.textContent).toContain('changed while your decision was being saved');
    expect(el.textContent).not.toContain('Request approved');
    expect((el.querySelector('textarea') as HTMLTextAreaElement).value).toBe('My note.');
  });

  it('on already decided reloads and shows the read-only decision', () => {
    render();
    api.deny.and.returnValue(problem('request_already_decided', { requestStatus: 'Approved' }));
    api.get.and.returnValue(of(decided('Approved')));

    typeNote('Deny.');
    denyButton().click();
    fixture.detectChanges();

    expect(el.textContent).toContain('already been decided');
    expect(el.textContent).toContain('read-only');
    expect(el.textContent).not.toContain('Request denied');
  });

  it('shows server validation errors inline', () => {
    render();
    api.deny.and.returnValue(
      throwError(() => ({ status: 400, code: 'validation_failed', errors: { note: ['A decision note is required.'] } }) as ApiProblem),
    );

    denyButton().click();
    fixture.detectChanges();

    expect(el.querySelector('.field-error')?.textContent).toContain('A decision note is required.');
  });

  describe('live updates (US4)', () => {
    it('reloads only for a hint about this request', () => {
      render();

      events.updates.next({ requestId: 99, change: 'decided' });
      expect(api.get).toHaveBeenCalledTimes(1);

      events.updates.next({ requestId: 5, change: 'decided' });
      expect(api.get).toHaveBeenCalledTimes(2);
    });

    it('shows that the request was decided elsewhere and keeps the typed note while pending', () => {
      render();
      typeNote('Draft note.');

      api.get.and.returnValue(of(detail()));
      events.updates.next({ requestId: 5, change: 'decided' });
      fixture.detectChanges();
      expect((el.querySelector('textarea') as HTMLTextAreaElement).value).toBe('Draft note.');

      api.get.and.returnValue(of(decided('Denied')));
      events.updates.next({ requestId: 5, change: 'decided' });
      fixture.detectChanges();
      expect(el.textContent).toContain('decided elsewhere');
      expect(el.textContent).toContain('read-only');
    });

    it('reloads after a reconnect and when the tab becomes visible again', () => {
      render();

      events.reconnects.next();
      document.dispatchEvent(new Event('visibilitychange'));

      expect(api.get).toHaveBeenCalledTimes(3);
    });
  });

  it('does not poll: no further requests without a trigger', fakeAsync(() => {
    render();

    tick(10 * 60 * 1000);

    expect(api.get).toHaveBeenCalledTimes(1);
  }));
});
