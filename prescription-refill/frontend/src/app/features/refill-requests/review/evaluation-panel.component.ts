import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { InstantComponent } from '../../../shared/time/instant.component';
import { AlertComponent } from '../../../shared/ui/alert.component';
import { IconComponent } from '../../../shared/ui/icon.component';
import { BadgeTone, StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { BlockingReasonCode, Evaluation, WarningCode } from '../data/models';

const BLOCKING_LABELS: Record<BlockingReasonCode, string> = {
  TOO_EARLY: 'Too early',
  NO_REFILLS_REMAINING: 'No refills remaining',
  INVALID_PRESCRIPTION_DATA: 'Invalid prescription data',
};

const WARNING_LABELS: Record<WarningCode, string> = {
  ALLERGY_CONFLICT: 'Allergy conflict',
  DRUG_INTERACTION: 'Drug interaction',
};

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
      return { label: 'Approval blocked', tone: 'danger' };
    }
    return warnings.length > 0 ? { label: 'Warnings', tone: 'warning' } : { label: 'No issues found', tone: 'neutral' };
  });

  protected blockingLabel(code: BlockingReasonCode): string {
    return BLOCKING_LABELS[code] ?? code;
  }

  protected warningLabel(code: WarningCode): string {
    return WARNING_LABELS[code] ?? code;
  }
}
