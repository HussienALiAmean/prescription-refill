import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IconComponent, IconName } from './icon.component';

export type AlertKind = 'info' | 'warning' | 'danger' | 'success';

const ICONS: Record<AlertKind, IconName> = {
  danger: 'alert-octagon',
  warning: 'alert-triangle',
  success: 'check-circle',
  info: 'info',
};

/**
 * Message box. Danger alerts use role="alert" (announced immediately); others use role="status".
 * Each kind has its own icon so meaning never depends on color alone.
 */
@Component({
  selector: 'app-alert',
  imports: [IconComponent],
  template: `
    <div class="alert" [class]="'alert alert-' + kind()" [attr.role]="role()">
      <app-icon class="alert-icon" [name]="icon()" [size]="18" />
      <div class="alert-body">
        @if (heading()) {
          <strong class="alert-heading">{{ heading() }}</strong>
        }
        <div class="alert-content"><ng-content /></div>
      </div>
    </div>
  `,
  styleUrl: './alert.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertComponent {
  readonly kind = input<AlertKind>('info');
  readonly heading = input<string>();

  protected readonly role = computed(() => (this.kind() === 'danger' ? 'alert' : 'status'));
  protected readonly icon = computed(() => ICONS[this.kind()]);
}
