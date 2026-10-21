import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { I18nService } from '../../core/i18n/i18n.service';
import { AlertComponent } from './alert.component';
import { IconComponent } from './icon.component';

/** Load failure with a retry action (constitution Principle VII). */
@Component({
  selector: 'app-error-state',
  imports: [AlertComponent, IconComponent],
  template: `
    <app-alert kind="danger" [heading]="headingText()">
      <p>{{ message() }}</p>
      <button type="button" class="btn btn-sm" (click)="retry.emit()">
        <app-icon name="refresh" [size]="16" />
        {{ t().common.tryAgain }}
      </button>
    </app-alert>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorStateComponent {
  protected readonly t = inject(I18nService).t;

  /** Defaults to "Something went wrong" in the active language. */
  readonly heading = input<string>();
  protected readonly headingText = computed(() => this.heading() ?? this.t().common.somethingWentWrong);
  readonly message = input.required<string>();
  readonly retry = output<void>();
}
