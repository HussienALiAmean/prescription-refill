import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IconComponent, IconName } from './icon.component';

export type BadgeTone = 'neutral' | 'success' | 'danger' | 'warning' | 'info' | 'accent';

// Shape cues so a badge never depends on color alone (004 FR-014); other tones use a dot.
const ICONS: Partial<Record<BadgeTone, IconName>> = {
  danger: 'alert-octagon',
  warning: 'alert-triangle',
  success: 'check-circle',
};

/** Small status label. The text carries the meaning; the icon or dot and color reinforce it. */
@Component({
  selector: 'app-status-badge',
  imports: [IconComponent],
  template: `<span class="badge" [class]="'badge badge-' + tone()"
    >@if (icon(); as name) {<app-icon class="badge-icon" [name]="name" [size]="12" />} @else {<span class="dot" aria-hidden="true"></span>}{{ label() }}</span
  >`,
  styles: `
    @use 'variables' as *;
    :host {
      display: inline-flex;
    }
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.125rem $space-2;
      font-size: $font-size-xs;
      font-weight: $font-weight-semibold;
      line-height: 1.5;
      border-radius: $radius-pill;
      border: 1px solid $color-border;
      white-space: nowrap;
    }
    .badge-icon {
      display: inline-flex;
      flex: none;
    }
    .dot {
      width: 0.375rem;
      height: 0.375rem;
      border-radius: $radius-pill;
      background: currentColor;
      flex: none;
    }
    .badge-neutral { background: $color-surface-sunken; color: $color-text-muted; border-color: $color-border; }
    .badge-success { background: $color-success-bg; color: $color-success; border-color: $color-success-border; }
    .badge-danger { background: $color-danger-bg; color: $color-danger; border-color: $color-danger-border; }
    .badge-warning { background: $color-warning-bg; color: $color-warning; border-color: $color-warning-border; }
    .badge-info { background: $color-info-bg; color: $color-info; border-color: $color-info-border; }
    .badge-accent { background: $color-accent-bg; color: $color-accent; border-color: $color-accent-border; }
    @media (forced-colors: active) {
      .badge { border-color: CanvasText; }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input<BadgeTone>('neutral');

  protected readonly icon = computed(() => ICONS[this.tone()]);
}
