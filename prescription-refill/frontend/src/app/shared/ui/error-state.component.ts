import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { AlertComponent } from './alert.component';
import { IconComponent } from './icon.component';

/** Load failure with a retry action (constitution Principle VII). */
@Component({
  selector: 'app-error-state',
  imports: [AlertComponent, IconComponent],
  template: `
    <app-alert kind="danger" [heading]="heading()">
      <p>{{ message() }}</p>
      <button type="button" class="btn btn-sm" (click)="retry.emit()">
        <app-icon name="refresh" [size]="16" />
        Try again
      </button>
    </app-alert>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorStateComponent {
  readonly heading = input('Something went wrong');
  readonly message = input.required<string>();
  readonly retry = output<void>();
}
