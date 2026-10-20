import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { IconComponent } from '../../../shared/ui/icon.component';
import { DecisionKind } from '../data/models';

/**
 * Approve/deny controls. The note is sent exactly as typed — the note rule lives only on the
 * server (constitution Principle I) and its errors are shown here. Approve availability follows
 * the latest server evaluation as a convenience only (FR-023). The character counter is guidance
 * only; it never blocks submission.
 */
@Component({
  selector: 'app-decision-form',
  imports: [IconComponent],
  templateUrl: './decision-form.component.html',
  styleUrl: './decision-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DecisionFormComponent {
  readonly canApprove = input(true);
  readonly submitting = input(false);
  /** The decision currently in flight, if any, so the pressed button shows progress. */
  readonly pending = input<DecisionKind | null>(null);
  readonly errors = input<string[]>([]);
  readonly note = model('');
  readonly approve = output<void>();
  readonly deny = output<void>();

  /** Display guidance matching the server's documented limit (001 FR-010). */
  protected readonly noteLimit = 1000;
  protected readonly noteLength = computed(() => this.note().length);

  protected onInput(event: Event): void {
    this.note.set((event.target as HTMLTextAreaElement).value);
  }
}
