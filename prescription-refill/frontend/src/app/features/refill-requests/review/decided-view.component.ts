import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { InstantComponent } from '../../../shared/time/instant.component';
import { IconComponent } from '../../../shared/ui/icon.component';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';
import { RefillRequestDetail } from '../data/models';

/** Read-only view of a decided request (FR-015, FR-018). No decision or edit controls. */
@Component({
  selector: 'app-decided-view',
  imports: [IconComponent, InstantComponent, StatusBadgeComponent],
  template: `
    <section class="card panel decided" [class.is-approved]="approved()" aria-labelledby="decision-heading">
      <div class="outcome">
        <span class="outcome-icon"><app-icon [name]="approved() ? 'check-circle' : 'x-circle'" [size]="22" /></span>
        <div class="outcome-text">
          <h2 id="decision-heading">Decision</h2>
          <p class="outcome-label">{{ request().status }}</p>
        </div>
        <app-status-badge label="Read-only" tone="neutral" />
      </div>

      <dl class="facts facts-single">
        @if (request().decidedAt; as decidedAt) {
          <div>
            <dt>Decided</dt>
            <dd class="tabular"><app-instant [value]="decidedAt" /></dd>
          </div>
        }
        <div>
          <dt>Note</dt>
          <dd><blockquote class="note">{{ request().decisionNote }}</blockquote></dd>
        </div>
      </dl>

      <p class="read-only">
        <app-icon name="lock" [size]="14" />
        <span>This request has been decided and is read-only.</span>
      </p>
    </section>
  `,
  styleUrl: './review-panels.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DecidedViewComponent {
  readonly request = input.required<RefillRequestDetail>();

  protected readonly approved = computed(() => this.request().status === 'Approved');
}
