import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { reloadOnChanges } from '../../../core/realtime/reload-triggers';
import { ApiProblem } from '../../../shared/http/problem-details';
import { InstantComponent } from '../../../shared/time/instant.component';
import { AlertComponent, AlertKind } from '../../../shared/ui/alert.component';
import { ErrorStateComponent } from '../../../shared/ui/error-state.component';
import { IconComponent } from '../../../shared/ui/icon.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { DecisionKind, Evaluation, RefillRequestDetail } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { DecidedViewComponent } from './decided-view.component';
import { DecisionFormComponent } from './decision-form.component';
import { EvaluationPanelComponent } from './evaluation-panel.component';
import { PatientPanelComponent } from './patient-panel.component';

type ReviewState =
  | { kind: 'loading' }
  | { kind: 'error'; problem: ApiProblem }
  | { kind: 'loaded'; request: RefillRequestDetail };

interface Notice {
  kind: AlertKind;
  text: string;
}

/**
 * Review and decide one request. Evaluation lifecycle (FR-023): fetched on open, on Refresh, from
 * a 422 response, and by reloading after a 409. No polling. Outcome handling follows the
 * contract's client-handling table (FR-011, FR-032).
 */
@Component({
  selector: 'app-refill-review-page',
  imports: [
    RouterLink,
    AlertComponent,
    ErrorStateComponent,
    IconComponent,
    InstantComponent,
    StatusBadgeComponent,
    PatientPanelComponent,
    EvaluationPanelComponent,
    DecisionFormComponent,
    DecidedViewComponent,
  ],
  templateUrl: './refill-review.page.html',
  styleUrl: './refill-review.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RefillReviewPage implements OnInit {
  private readonly api = inject(RefillRequestsApi);

  readonly id = input.required<number, unknown>({ transform: numberAttribute });

  protected readonly state = signal<ReviewState>({ kind: 'loading' });
  protected readonly notice = signal<Notice | null>(null);
  protected readonly note = signal('');
  protected readonly noteErrors = signal<string[]>([]);
  protected readonly submitting = signal(false);
  /** Which decision is in flight, so only the pressed button shows progress. */
  protected readonly pendingDecision = signal<DecisionKind | null>(null);
  protected readonly refreshing = signal(false);

  protected readonly request = computed(() => {
    const state = this.state();
    return state.kind === 'loaded' ? state.request : null;
  });
  protected readonly loadProblem = computed(() => {
    const state = this.state();
    return state.kind === 'error' ? state.problem : null;
  });

  constructor() {
    // Only hints about this request matter here (US4).
    reloadOnChanges(() => this.refresh(), (event) => event.requestId === this.id());
  }

  ngOnInit(): void {
    this.load();
  }

  /** Initial load or retry after a failed load. */
  load(): void {
    this.state.set({ kind: 'loading' });
    this.fetch();
  }

  /** Fresh server evaluation while keeping the current view on screen. */
  refresh(): void {
    this.refreshing.set(true);
    this.fetch();
  }

  decide(kind: DecisionKind): void {
    this.submitting.set(true);
    this.pendingDecision.set(kind);
    this.noteErrors.set([]);
    this.notice.set(null);

    const call = kind === 'approve' ? this.api.approve(this.id(), this.note()) : this.api.deny(this.id(), this.note());
    call.subscribe({
      next: (request) => {
        this.submitting.set(false);
        this.pendingDecision.set(null);
        this.note.set('');
        this.state.set({ kind: 'loaded', request });
        this.notice.set({ kind: 'success', text: kind === 'approve' ? 'Request approved.' : 'Request denied.' });
      },
      error: (problem: ApiProblem) => {
        this.submitting.set(false);
        this.pendingDecision.set(null);
        this.handleDecisionProblem(problem);
      },
    });
  }

  private handleDecisionProblem(problem: ApiProblem): void {
    switch (problem.code) {
      case 'approval_blocked': {
        const current = this.request();
        if (current && problem.evaluation) {
          this.state.set({ kind: 'loaded', request: { ...current, evaluation: problem.evaluation as Evaluation } });
        }
        this.notice.set({
          kind: 'warning',
          text: 'Approval is blocked based on current data. The safety checks below have been updated. You can still deny the request.',
        });
        return;
      }
      case 'concurrency_conflict':
        this.notice.set({
          kind: 'warning',
          text: 'This request changed while your decision was being saved. Nothing was saved. The latest data has been loaded; review it and decide again.',
        });
        this.refresh();
        return;
      case 'request_already_decided':
        this.notice.set({ kind: 'info', text: 'This request has already been decided. Nothing was changed.' });
        this.refresh();
        return;
      case 'validation_failed':
        this.noteErrors.set(problem.errors?.['note'] ?? [problem.title ?? 'The decision is not valid.']);
        return;
      case 'not_found':
        this.notice.set({ kind: 'danger', text: 'This request no longer exists.' });
        return;
      default:
        this.notice.set({
          kind: 'danger',
          text: 'The decision could not be confirmed. Refresh to see the current state before trying again.',
        });
    }
  }

  private fetch(): void {
    this.api.get(this.id()).subscribe({
      next: (request) => {
        const previous = this.request();
        this.refreshing.set(false);
        this.state.set({ kind: 'loaded', request });
        if (previous?.status === 'Pending' && request.status !== 'Pending' && !this.notice()) {
          this.notice.set({ kind: 'info', text: 'This request was decided elsewhere. It is now read-only.' });
        }
      },
      error: (problem: ApiProblem) => {
        this.refreshing.set(false);
        if (this.request() && problem.code !== 'not_found') {
          this.notice.set({ kind: 'danger', text: 'The latest data could not be loaded. Try Refresh again.' });
        } else {
          this.state.set({ kind: 'error', problem });
        }
      },
    });
  }
}
