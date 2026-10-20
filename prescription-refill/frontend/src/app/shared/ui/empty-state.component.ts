import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IconComponent } from './icon.component';

/** Empty result with an optional heading, icon, and projected action (e.g. "Clear search"). */
@Component({
  selector: 'app-empty-state',
  imports: [IconComponent],
  template: `
    <div class="empty">
      <span class="empty-icon"><app-icon [name]="icon()" [size]="24" /></span>
      @if (heading()) {
        <h2 class="empty-heading">{{ heading() }}</h2>
      }
      <p class="empty-state" role="status">{{ message() }}</p>
      <div class="empty-actions"><ng-content /></div>
    </div>
  `,
  styles: `
    @use 'variables' as *;
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: $space-7 $space-4;
      text-align: center;
      background: $color-surface;
      border: 1px solid $color-border;
      border-radius: $radius-md;
    }
    .empty-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 3rem;
      height: 3rem;
      margin-bottom: $space-3;
      border-radius: $radius-pill;
      background: linear-gradient(135deg, rgba($glow-teal, 0.12), rgba($glow-indigo, 0.16));
      box-shadow: 0 0 0 1px rgba($glow-indigo, 0.16) inset;
      color: $color-accent-2;
    }
    .empty-heading {
      margin: 0 0 $space-1;
      font-size: $font-size-base;
    }
    .empty-state {
      margin: 0;
      max-width: 28rem;
      color: $color-text-muted;
      font-size: $font-size-sm;
      overflow-wrap: anywhere;
    }
    .empty-actions:not(:empty) {
      margin-top: $space-4;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  readonly message = input.required<string>();
  readonly heading = input<string>();
  readonly icon = input('inbox');
}
