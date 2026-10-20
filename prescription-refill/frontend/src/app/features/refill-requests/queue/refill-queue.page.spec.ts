import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { ApiProblem } from '../../../shared/http/problem-details';
import { RefillRequestList } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { list, pagedList } from '../testing/fixtures';
import { RefillQueuePage } from './refill-queue.page';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';

describe('RefillQueuePage', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;
  let events: FakeRefillEvents;

  function render() {
    const fixture = TestBed.createComponent(RefillQueuePage);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    events = new FakeRefillEvents();
    TestBed.configureTestingModule({
      imports: [RefillQueuePage],
      providers: [provideRouter([]), { provide: RefillRequestsApi, useValue: api }, provideFakeRefillEvents(events)],
    });
  });

  it('shows a loading state until the queue arrives', () => {
    const pending = new Subject<RefillRequestList>();
    api.list.and.returnValue(pending);

    const { fixture, el } = render();
    expect(el.textContent).toContain('Loading');

    pending.next(list());
    fixture.detectChanges();
    expect(el.textContent).not.toContain('Loading');
  });

  it('lists pending requests with blocked and warning badges and links to the review', () => {
    api.list.and.returnValue(of(list()));

    const { el } = render();

    const rows = el.querySelectorAll('li.queue-item');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Avery Lindqvist');
    expect(rows[0].textContent).toContain('Avelorin');
    expect(rows[1].textContent).toContain('Approval blocked');
    expect(rows[1].textContent).toContain('Warnings');
    expect(rows[0].querySelector('a')?.getAttribute('href')).toBe('/requests/1');
  });

  it('shows the evaluation time for the list', () => {
    api.list.and.returnValue(of(list()));

    const { el } = render();

    expect(el.querySelector('time')?.getAttribute('datetime')).toBe(list().evaluatedAt);
  });

  it('shows an empty state when nothing is pending', () => {
    api.list.and.returnValue(of(list({ items: [] })));

    const { el } = render();

    expect(el.querySelector('.empty-state')?.textContent).toContain('No pending requests');
  });

  describe('status filter (US3)', () => {
    const approvedList = () =>
      list({
        items: [
          {
            id: 9,
            status: 'Approved',
            requestedAt: '2026-10-17T07:30:00+00:00',
            decidedAt: '2026-10-18T07:30:00+00:00',
            patientName: 'Avery Lindqvist',
            medicationName: 'Avelorin',
            canApprove: null,
            hasWarnings: null,
          },
        ],
      });

    function renderWithStatus(status: string) {
      const fixture = TestBed.createComponent(RefillQueuePage);
      fixture.componentRef.setInput('status', status);
      fixture.detectChanges();
      return { fixture, el: fixture.nativeElement as HTMLElement };
    }

    it('loads the list for the status in the query parameter', () => {
      api.list.and.returnValue(of(approvedList()));

      const { el } = renderWithStatus('Approved');

      expect(api.list).toHaveBeenCalledWith('Approved', jasmine.objectContaining({ page: 1, pageSize: 10 }));
      expect(el.querySelector('h1')?.textContent).toContain('Approved requests');
      const item = el.querySelector('li.queue-item') as HTMLElement;
      expect(item.textContent).toContain('Decided');
      expect(item.textContent).toContain('Approved');
      expect(item.querySelector('a')?.getAttribute('href')).toBe('/requests/9');
    });

    it('reloads when the filter changes', () => {
      api.list.and.returnValues(of(list()), of(list({ items: [] })));
      const { fixture, el } = renderWithStatus('Pending');

      fixture.componentRef.setInput('status', 'Denied');
      fixture.detectChanges();

      const [status, query] = api.list.calls.mostRecent().args;
      expect(status).toBe('Denied');
      expect(query).toEqual(jasmine.objectContaining({ page: 1, pageSize: 10 }));
      expect(el.querySelector('.empty-state')?.textContent).toContain('No denied requests');
    });

    it('treats a missing or unknown status as Pending', () => {
      api.list.and.returnValue(of(list()));

      renderWithStatus('nonsense');

      expect(api.list).toHaveBeenCalledWith('Pending', jasmine.objectContaining({ page: 1, pageSize: 10 }));
    });
  });

  describe('live updates (US4)', () => {
    it('reloads the list from the API on any RequestUpdated hint', () => {
      api.list.and.returnValue(of(list()));
      const { fixture } = render();

      events.updates.next({ requestId: 42, change: 'created' });
      fixture.detectChanges();

      expect(api.list).toHaveBeenCalledTimes(2);
    });

    /** Search 'x', 25 per page, page 2: the state a reload must keep (003 FR-015). */
    function toPage2Of25WithSearch(fixture: ComponentFixture<RefillQueuePage>, el: HTMLElement) {
      const input = el.querySelector('input[type="search"]') as HTMLInputElement;
      input.value = 'x';
      input.dispatchEvent(new Event('input'));
      tick(300);
      fixture.detectChanges();
      const select = el.querySelector('app-pagination select') as HTMLSelectElement;
      select.value = '25';
      select.dispatchEvent(new Event('change'));
      fixture.detectChanges();
      (Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Next')) as HTMLButtonElement).click();
      fixture.detectChanges();
      expect(api.list.calls.mostRecent().args[1]).toEqual({ page: 2, pageSize: 25, search: 'x' });
    }

    it('a RequestUpdated hint reloads the current page with the current search and page size', fakeAsync(() => {
      api.list.and.returnValue(of(pagedList({ matchingCount: 60, statusTotal: 60 })));
      const { fixture, el } = render();
      toPage2Of25WithSearch(fixture, el);
      const calls = api.list.calls.count();

      events.updates.next({ requestId: 42, change: 'decided' });
      fixture.detectChanges();

      expect(api.list.calls.count()).toBe(calls + 1);
      expect(api.list.calls.mostRecent().args).toEqual(['Pending', { page: 2, pageSize: 25, search: 'x' }]);
    }));

    it('reconnect and becoming visible reload the current page', fakeAsync(() => {
      api.list.and.returnValue(of(pagedList({ matchingCount: 60, statusTotal: 60 })));
      const { fixture, el } = render();
      toPage2Of25WithSearch(fixture, el);
      const calls = api.list.calls.count();

      events.reconnects.next();
      document.dispatchEvent(new Event('visibilitychange'));

      const reloads = api.list.calls.allArgs().slice(calls);
      expect(reloads).toEqual([
        ['Pending', { page: 2, pageSize: 25, search: 'x' }],
        ['Pending', { page: 2, pageSize: 25, search: 'x' }],
      ]);
    }));

    it('a hint that empties the last page loads the new last page', () => {
      api.list.and.returnValues(
        of(pagedList({ matchingCount: 11, statusTotal: 11 })),
        of(pagedList({ items: [list().items[0]], page: 2, matchingCount: 11, statusTotal: 11 })),
        of(pagedList({ items: [], page: 2, matchingCount: 10, statusTotal: 10 })),
        of(pagedList({ page: 1, matchingCount: 10, statusTotal: 10 })),
      );
      const { fixture, el } = render();
      (Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Next')) as HTMLButtonElement).click();
      fixture.detectChanges();

      events.updates.next({ requestId: 1, change: 'decided' });
      fixture.detectChanges();

      expect(api.list.calls.allArgs().map(([, query]) => query?.page)).toEqual([1, 2, 2, 1]);
      expect(el.textContent).toContain('Page 1 of 1');
    });

    it('reloads after a reconnect and when the tab becomes visible again', () => {
      api.list.and.returnValue(of(list()));
      render();

      events.reconnects.next();
      document.dispatchEvent(new Event('visibilitychange'));

      expect(api.list).toHaveBeenCalledTimes(3);
    });
  });

  it('shows only the newest response when an older one arrives late (003 FR-016)', () => {
    const older = new Subject<RefillRequestList>();
    const newer = new Subject<RefillRequestList>();
    api.list.and.returnValues(older, newer);
    const { fixture, el } = render();

    // Refresh while the first load is still unanswered; the first answer then arrives last.
    (Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Refresh')) as HTMLButtonElement).click();
    newer.next(pagedList({ items: [list().items[1]], matchingCount: 1, statusTotal: 1 }));
    older.next(pagedList());
    fixture.detectChanges();

    const rows = el.querySelectorAll('li.queue-item');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('Taylor Nguyen');
  });

  it('shows an error state with retry', () => {
    api.list.and.returnValues(
      throwError(() => ({ status: 0, code: 'network_error' }) as ApiProblem),
      of(list()),
    );

    const { fixture, el } = render();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');

    (el.querySelector('app-error-state button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalledTimes(2);
    expect(el.querySelectorAll('li.queue-item').length).toBe(2);
  });
});
