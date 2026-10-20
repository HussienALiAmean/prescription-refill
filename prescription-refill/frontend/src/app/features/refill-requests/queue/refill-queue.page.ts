import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { Observable, Subject, catchError, debounceTime, map, of, switchMap } from 'rxjs';
import { reloadOnChanges } from '../../../core/realtime/reload-triggers';
import { ApiProblem } from '../../../shared/http/problem-details';
import { InstantComponent } from '../../../shared/time/instant.component';
import { EmptyStateComponent } from '../../../shared/ui/empty-state.component';
import { ErrorStateComponent } from '../../../shared/ui/error-state.component';
import { IconComponent } from '../../../shared/ui/icon.component';
import { PaginationComponent } from '../../../shared/ui/pagination.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { RefillRequestList, RequestStatus } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { REQUEST_STATUSES, StatusFilterComponent } from './status-filter.component';

type LoadResult = { ok: true; list: RefillRequestList } | { ok: false; problem: ApiProblem };

type QueueState =
  | { kind: 'loading' }
  | { kind: 'error'; problem: ApiProblem }
  | { kind: 'loaded'; list: RefillRequestList };

const HEADINGS: Record<RequestStatus, string> = {
  Pending: 'Pending refill requests',
  Approved: 'Approved requests',
  Denied: 'Denied requests',
};

const DESCRIPTIONS: Record<RequestStatus, string> = {
  Pending: 'Requests awaiting a decision, oldest first. Open one to see its safety checks and decide.',
  Approved: 'Approved requests, most recently decided first. Decisions are final and read-only.',
  Denied: 'Denied requests, most recently decided first. Decisions are final and read-only.',
};

export const PAGE_SIZES = [10, 25, 50] as const;

/**
 * Request list (FR-018): the Pending queue by default, or decided requests via the `status`
 * query parameter. Decided requests open read-only. Search, counts, and paging run on the server
 * (003 research R9): the page requests one page at a time and shows the server's totals; it
 * never filters, counts, or slices the list itself.
 */
@Component({
  selector: 'app-refill-queue-page',
  imports: [
    RouterLink,
    InstantComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    IconComponent,
    PaginationComponent,
    StatusBadgeComponent,
    StatusFilterComponent,
  ],
  templateUrl: './refill-queue.page.html',
  styleUrl: './refill-queue.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RefillQueuePage {
  private readonly api = inject(RefillRequestsApi);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly resultsHeading = viewChild<ElementRef<HTMLElement>>('resultsHeading');

  /** Bound from the `status` query parameter; anything unknown means Pending. */
  readonly status = input<string | undefined>();

  protected readonly selected = computed<RequestStatus>(() => {
    const value = this.status();
    return REQUEST_STATUSES.find((s) => s === value) ?? 'Pending';
  });
  protected readonly heading = computed(() => HEADINGS[this.selected()]);
  protected readonly description = computed(() => DESCRIPTIONS[this.selected()]);
  protected readonly emptyMessage = computed(() => `No ${this.selected().toLowerCase()} requests.`);

  protected readonly state = signal<QueueState>({ kind: 'loading' });
  protected readonly reloading = signal(false);
  protected readonly list = computed(() => {
    const state = this.state();
    return state.kind === 'loaded' ? state.list : null;
  });

  protected readonly query = signal('');

  /** Matching requests for the current search; the whole status when there is no search. */
  protected readonly matchingCount = computed(() => {
    const list = this.list();
    return list?.matchingCount ?? list?.items.length ?? 0;
  });

  /** Server totals for the loaded status (003 FR-010); the screen never counts items itself. */
  protected readonly counts = computed(() => {
    const list = this.list();
    return {
      total: list?.statusTotal ?? list?.items.length ?? 0,
      blocked: list?.summary?.blocked ?? 0,
      warnings: list?.summary?.warnings ?? 0,
      clear: list?.summary?.clear ?? 0,
    };
  });

  protected readonly pageSizes = PAGE_SIZES;
  protected readonly page = signal(1);
  protected readonly pageSize = signal<number>(PAGE_SIZES[0]);
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.matchingCount() / this.pageSize())));
  /** The requested page, clamped for display while a beyond-the-end reload is corrected. */
  protected readonly currentPage = computed(() => Math.min(this.page(), this.pageCount()));

  /** Every load goes through here, so only the newest request's response is shown (003 FR-016). */
  private readonly loads = new Subject<void>();
  private readonly searches = new Subject<void>();

  constructor() {
    // Load whenever the selected status changes (including the first render).
    effect(() => {
      this.selected();
      untracked(() => {
        this.page.set(1);
        this.load(true);
      });
    });

    this.loads
      .pipe(
        switchMap(() => this.fetch()),
        takeUntilDestroyed(),
      )
      .subscribe((result) => this.show(result));

    // Search text is sent once typing pauses (003 research R9).
    this.searches.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => this.load());

    // Any created or decided request can change this list (US4); the current page, search, and
    // page size are kept (003 FR-015).
    reloadOnChanges(() => this.load());
  }

  load(reset = false): void {
    if (reset || this.state().kind !== 'loaded') {
      this.state.set({ kind: 'loading' });
    } else {
      this.reloading.set(true);
    }
    this.loads.next();
  }

  private fetch(): Observable<LoadResult> {
    return this.api
      .list(this.selected(), { page: this.page(), pageSize: this.pageSize(), search: this.query().trim() || undefined })
      .pipe(
        map((list): LoadResult => ({ ok: true, list })),
        catchError((problem: ApiProblem) => of<LoadResult>({ ok: false, problem })),
      );
  }

  private show(result: LoadResult): void {
    if (!result.ok) {
      this.reloading.set(false);
      this.state.set({ kind: 'error', problem: result.problem });
      return;
    }

    // The page no longer exists (requests were decided elsewhere): show the last one (003 FR-014).
    const { list } = result;
    const matching = list.matchingCount ?? list.items.length;
    const lastPage = Math.max(1, Math.ceil(matching / this.pageSize()));
    if (list.items.length === 0 && matching > 0 && this.page() > lastPage) {
      this.page.set(lastPage);
      this.loads.next();
      return;
    }

    this.reloading.set(false);
    this.state.set({ kind: 'loaded', list });
  }

  protected select(status: RequestStatus): void {
    this.router.navigate([], { queryParams: { status: status === 'Pending' ? null : status } });
  }

  protected setQuery(value: string): void {
    this.query.set(value);
    this.page.set(1);
    this.searches.next();
  }

  protected clearQuery(): void {
    this.query.set('');
    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    this.page.set(Math.min(Math.max(1, page), this.pageCount()));
    this.load();
    // Land keyboard and screen reader users at the start of the new page (FR-031).
    afterNextRender(() => this.resultsHeading()?.nativeElement.focus(), { injector: this.injector });
  }

  protected setPageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  protected tone(status: RequestStatus): 'success' | 'danger' | 'warning' {
    return status === 'Approved' ? 'success' : status === 'Denied' ? 'danger' : 'warning';
  }
}
