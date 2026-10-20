import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RefillEventsService } from '../../../core/realtime/refill-events.service';
import { IconComponent } from '../icon.component';

/** Tells the user when live updates are unavailable; everything else keeps working (US4). */
@Component({
  selector: 'app-connection-indicator',
  imports: [IconComponent],
  template: `
    @switch (events.connectionState()) {
      @case ('disconnected') {
        <p class="connection-indicator is-offline" role="status">
          <app-icon name="wifi-off" [size]="16" />
          <span>Live updates unavailable — use Refresh.</span>
        </p>
      }
      @case ('reconnecting') {
        <p class="connection-indicator is-reconnecting" role="status">
          <span class="spinner" aria-hidden="true"></span>
          <span>Reconnecting live updates…</span>
        </p>
      }
    }
  `,
  styles: `
    @use 'variables' as *;
    .connection-indicator {
      display: inline-flex;
      align-items: center;
      gap: $space-2;
      margin: 0;
      padding: $space-1 $space-3;
      font-size: $font-size-xs;
      font-weight: $font-weight-semibold;
      line-height: $line-height-tight;
      border: 1px solid transparent;
      border-radius: $radius-pill;
    }
    .is-offline {
      background: $color-danger-bg;
      border-color: $color-danger-border;
      color: $color-danger;
    }
    .is-reconnecting {
      background: $color-warning-bg;
      border-color: $color-warning-border;
      color: $color-warning;
    }
    .spinner {
      width: 0.75rem;
      height: 0.75rem;
    }
    @media (forced-colors: active) {
      .connection-indicator {
        border-color: CanvasText;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectionIndicatorComponent {
  protected readonly events = inject(RefillEventsService);
}
