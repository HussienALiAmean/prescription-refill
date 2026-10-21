import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { I18nService } from '../../../core/i18n/i18n.service';
import { InstantComponent } from '../../../shared/time/instant.component';
import { AlertComponent } from '../../../shared/ui/alert.component';
import { IconComponent } from '../../../shared/ui/icon.component';
import { BadgeTone, StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { BlockingReasonCode, Evaluation, WarningCode } from '../data/models';

/**
 * Shows the server's evaluation with its time and a staleness notice (FR-005, FR-023). It renders
 * server results only; it never computes rules. Labels are display names for server codes.
 */
@Component({
  selector: 'app-evaluation-panel',
  imports: [AlertComponent, IconComponent, InstantComponent, StatusBadgeComponent],
  templateUrl: './evaluation-panel.component.html',
  styleUrl: './review-panels.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EvaluationPanelComponent {
  protected readonly t = inject(I18nService).t;

  readonly evaluation = input.required<Evaluation>();
  readonly refreshing = input(false);
  readonly refresh = output<void>();

  protected readonly blocked = computed(() => this.evaluation().blockingReasons.length > 0);
  protected readonly clear = computed(
    () => this.evaluation().blockingReasons.length === 0 && this.evaluation().warnings.length === 0,
  );
  protected readonly summary = computed<{ label: string; tone: BadgeTone }>(() => {
    const { blockingReasons, warnings } = this.evaluation();
    // Named in text; the no-issues state is neutral, never a reassuring "safe" signal (004 FR-014, FR-015).
    if (blockingReasons.length > 0) {
      return { label: this.t().common.approvalBlocked, tone: 'danger' };
    }
    return warnings.length > 0
      ? { label: this.t().common.warnings, tone: 'warning' }
      : { label: this.t().common.noIssuesFound, tone: 'neutral' };
  });

  protected blockingLabel(code: BlockingReasonCode): string {
    return this.t().evaluation.blocking[code] ?? code;
  }

  protected warningLabel(code: WarningCode): string {
    return this.t().evaluation.warning[code] ?? code;
  }
}
