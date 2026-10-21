import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';
import { ListQuery, RefillRequestList, RefillRequestSummary, RequestStatus } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { list, pagedList } from '../testing/fixtures';
import { RefillQueuePage } from './refill-queue.page';
import { provideI18nTesting } from '../../../core/i18n/testing';

/** `count` pending summaries in server order; names are unique so pages can be compared. */
function items(count: number): RefillRequestSummary[] {
  return Array.from({ length: count }, (_, i) => ({
    id: i + 1,
    status: 'Pending' as const,
    requestedAt: `2026-10-${String(1 + (i % 28)).padStart(2, '0')}T07:30:00+00:00`,
    decidedAt: null,
    patientName: `Patient ${String(i + 1).padStart(2, '0')}`,
    medicationName: i % 2 === 0 ? 'Avelorin' : 'Brimexa',
    canApprove: i % 3 !== 0,
    hasWarnings: i % 4 === 0,
  }));
}

/**
 * Search, counts, and paging run on the server (003). These specs fake the server's list
 * endpoint: it filters and slices like the API, so the specs prove the screen requests the right
 * page and renders the server's numbers rather than computing its own.
 */
describe('RefillQueuePage (server-side counts, search, pagination)', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;
  let events: FakeRefillEvents;
  let fixture: ComponentFixture<RefillQueuePage>;
  let el: HTMLElement;

  function render() {
    fixture = TestBed.createComponent(RefillQueuePage);
    fixture.detectChanges();
    el = fixture.nativeElement;
  }

  /** Serves `all` the way the API does; `extra` overrides fields of every response. */
  function serve(all: RefillRequestSummary[], extra: Partial<RefillRequestList> = {}) {
    api.list.and.callFake((_status?: RequestStatus, query?: ListQuery) => {
      const needle = query?.search?.trim().toLowerCase() ?? '';
      const matching = needle
        ? all.filter((i) => i.patientName.toLowerCase().includes(needle) || i.medicationName.toLowerCase().includes(needle))
        : all;
      const page = query?.page ?? 1;
      const size = query?.pageSize ?? Math.max(1, matching.length);
      return of(
        pagedList({
          items: matching.slice((page - 1) * size, page * size),
          page,
          pageSize: size,
          matchingCount: matching.length,
          statusTotal: all.length,
          ...extra,
        }),
      );
    });
  }

  const lastQuery = () => api.list.calls.mostRecent().args[1];
  const rowNames = () => Array.from(el.querySelectorAll('li.queue-item .patient')).map((n) => n.textContent!.trim());
  const text = () => el.textContent!.replace(/\s+/g, ' ');
  const button = (label: string) =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes(label)) as HTMLButtonElement;
  const search = (value: string) => {
    const input = el.querySelector('input[type="search"]') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  const setPageSize = (size: number) => {
    const select = el.querySelector('app-pagination select') as HTMLSelectElement;
    select.value = String(size);
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  };
  const next = () => {
    button('Next').click();
    fixture.detectChanges();
  };
  /** Walks every page with Next and returns all row names seen. */
  const collectAllPages = () => {
    const seen = [...rowNames()];
    while (button('Next') && !button('Next').disabled) {
      next();
      seen.push(...rowNames());
    }
    return seen;
  };

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    events = new FakeRefillEvents();
    TestBed.configureTestingModule({
      imports: [RefillQueuePage],
      providers: [...provideI18nTesting(), provideRouter([]), { provide: RefillRequestsApi, useValue: api }, provideFakeRefillEvents(events)],
    });
  });

  it('shows the server summary and status total, not counts from the item flags', () => {
    // The two items' flags would give 1 / 1 / 1 and a total of 2.
    serve(list().items, { statusTotal: 23, summary: { blocked: 7, warnings: 5, clear: 3 } });
    render();

    const summary = el.querySelector('.summary')!.textContent!.replace(/\s+/g, ' ');
    expect(summary).toContain('7 approval blocked');
    expect(summary).toContain('5 with warnings');
    expect(summary).toContain('3 no issues found');
    expect(el.querySelector('app-status-filter .count')?.textContent).toContain('23');
  });

  it('shows no summary when the server sends none', () => {
    serve(list().items, { summary: undefined });
    render();

    expect(el.querySelector('.summary')).toBeNull();
  });

  it('keeps the list evaluation time as the first time on the page', () => {
    serve(list().items);
    render();

    expect(el.querySelector('time')?.getAttribute('datetime')).toBe(list().evaluatedAt);
  });

  it('sends the trimmed search to the server after 300 ms and renders its matches in order', fakeAsync(() => {
    serve(items(6));
    render();

    search('  Brimexa ');
    expect(api.list).toHaveBeenCalledTimes(1);
    tick(300);
    fixture.detectChanges();

    expect(lastQuery()).toEqual({ page: 1, pageSize: 10, search: 'Brimexa' });
    expect(rowNames()).toEqual(['Patient 02', 'Patient 04', 'Patient 06']);
    expect(text()).toContain('3 of 6 requests match');
  }));

  it('typing quickly sends only the final text', fakeAsync(() => {
    serve(items(6));
    render();

    search('p');
    tick(100);
    search('pa');
    tick(100);
    search('patient 05');
    tick(300);
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalledTimes(2);
    expect(lastQuery()?.search).toBe('patient 05');
    expect(rowNames()).toEqual(['Patient 05']);
  }));

  it('shows a distinct empty state for no matches, with a working Clear search', fakeAsync(() => {
    serve(list().items);
    render();

    search('zzz');
    tick(300);
    fixture.detectChanges();

    expect(el.querySelectorAll('li.queue-item').length).toBe(0);
    expect(el.querySelector('.empty-state')?.textContent).toContain('No requests match “zzz”');
    expect(el.textContent).not.toContain('No pending requests');

    button('Clear search').click();
    fixture.detectChanges();

    expect(lastQuery()?.search).toBeUndefined();
    expect(el.querySelectorAll('li.queue-item').length).toBe(2);
  }));

  it('keeps the search text when the list reloads from a live update', fakeAsync(() => {
    serve(list().items);
    render();
    search('taylor');
    tick(300);

    events.updates.next({ requestId: 6, change: 'created' });
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalledTimes(3);
    expect(lastQuery()?.search).toBe('taylor');
    expect(rowNames()).toEqual(['Taylor Nguyen']);
  }));

  for (const total of [0, 1, 10, 11, 26]) {
    it(`shows every one of ${total} records exactly once across server pages, in server order`, () => {
      serve(items(total));
      render();

      const expected = items(total).map((item) => item.patientName);
      expect(collectAllPages()).toEqual(expected);
      expect(api.list.calls.allArgs().every(([, query]) => query?.pageSize === 10)).toBeTrue();
      expect(el.querySelectorAll('li.queue-item').length).toBeLessThanOrEqual(10);
      if (total === 0) {
        expect(el.querySelector('app-pagination nav')).toBeNull();
      }
    });
  }

  it('requests each page from the server and renders the returned page', () => {
    serve(items(23));
    render();

    next();

    expect(lastQuery()).toEqual({ page: 2, pageSize: 10, search: undefined });
    expect(rowNames()[0]).toBe('Patient 11');
    expect(text()).toContain('Showing 11–20 of 23');
  });

  it('pages through the server search result, not the whole list', fakeAsync(() => {
    serve(items(26));
    render();

    search('avelorin');
    tick(300);
    fixture.detectChanges();

    const expected = items(26)
      .filter((item) => item.medicationName === 'Avelorin')
      .map((item) => item.patientName);
    expect(text()).toContain('Showing 1–10 of 13');
    expect(collectAllPages()).toEqual(expected);
  }));

  it('returns to page 1 when the search, page size, or status changes', fakeAsync(() => {
    serve(items(26));
    render();
    const toPage2 = () => {
      next();
      expect(text()).toContain('Page 2 of');
    };

    toPage2();
    search('patient');
    tick(300);
    fixture.detectChanges();
    expect(lastQuery()?.page).toBe(1);
    expect(text()).toContain('Page 1 of 3');

    toPage2();
    setPageSize(25);
    expect(lastQuery()).toEqual(jasmine.objectContaining({ page: 1, pageSize: 25 }));
    expect(text()).toContain('Page 1 of 2');
    expect(el.querySelectorAll('li.queue-item').length).toBe(25);

    toPage2();
    fixture.componentRef.setInput('status', 'Approved');
    fixture.detectChanges();
    expect(lastQuery()?.page).toBe(1);
    expect(text()).toContain('Page 1 of');
  }));

  it('requests the last existing page when the server returns an empty page beyond the end', () => {
    serve(items(26));
    render();
    next();
    next();
    expect(text()).toContain('Page 3 of 3');

    // Requests were decided elsewhere: page 3 no longer exists.
    serve(items(12));
    events.reconnects.next();
    fixture.detectChanges();

    expect(api.list.calls.allArgs().slice(-2).map(([, query]) => query?.page)).toEqual([3, 2]);
    expect(text()).toContain('Page 2 of 2');
    expect(rowNames()).toEqual(['Patient 11', 'Patient 12']);
  });

  // 004 US2/US3: rows opt into the shared pointer glow; the row link stays the row's focus target.
  it('marks each row as a glow target', () => {
    serve(items(3));
    render();

    const rows = Array.from(el.querySelectorAll('li.queue-item'));
    expect(rows.length).toBe(3);
    expect(rows.every((row) => row.hasAttribute('data-glow'))).toBeTrue();
  });

  it('keeps the row link as the first focusable element of each row', () => {
    serve(items(3));
    render();

    for (const row of Array.from(el.querySelectorAll('li.queue-item'))) {
      const firstFocusable = row.querySelector('a[href], button, input, select, textarea, [tabindex]');
      expect(firstFocusable?.matches('a[href^="/requests/"]')).withContext(row.textContent ?? '').toBeTrue();
    }
  });

  it('keeps rows visible and shows progress during a background reload', () => {
    serve(list().items);
    render();

    // A reload that has not answered yet.
    const reload = new Subject<RefillRequestList>();
    api.list.and.returnValue(reload);
    button('Refresh').click();
    fixture.detectChanges();

    expect(el.querySelectorAll('li.queue-item').length).toBe(2);
    expect(el.textContent).not.toContain('Loading');
    expect(button('Refresh').disabled).toBeTrue();
    expect(button('Refresh').querySelector('.spinner')).not.toBeNull();

    reload.next(pagedList());
    fixture.detectChanges();
    expect(button('Refresh').disabled).toBeFalse();
  });
});
