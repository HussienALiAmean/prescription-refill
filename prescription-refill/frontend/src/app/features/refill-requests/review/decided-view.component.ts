import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18nService } from '../../../core/i18n/i18n.service';
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
          <h2 id="decision-heading">{{ t().decided.heading }}</h2>
          <p class="outcome-label">{{ t().status[request().status] }}</p>
        </div>
        <app-status-badge [label]="t().decided.readOnly" tone="neutral" />
      </div>

      <dl class="facts facts-single">
        @if (request().decidedAt; as decidedAt) {
          <div>
            <dt>{{ t().decided.decided }}</dt>
            <dd class="tabular"><app-instant [value]="decidedAt" /></dd>
          </div>
        }
        <div>
          <dt>{{ t().decided.note }}</dt>
          <!-- Shown exactly as recorded, in its own direction (spec 005 FR-004, FR-015). -->
          <dd><blockquote class="note" dir="auto">{{ request().decisionNote }}</blockquote></dd>
        </div>
      </dl>

      <p class="read-only">
        <app-icon name="lock" [size]="14" />
        <span>{{ t().decided.readOnlyNotice }}</span>
      </p>
    </section>
  `,
  styleUrl: './review-panels.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DecidedViewComponent {
  protected readonly t = inject(I18nService).t;

  readonly request = input.required<RefillRequestDetail>();

  protected readonly approved = computed(() => this.request().status === 'Approved');
}
