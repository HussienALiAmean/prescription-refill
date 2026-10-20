import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RequestStatus } from '../data/models';

export const REQUEST_STATUSES: readonly RequestStatus[] = ['Pending', 'Approved', 'Denied'];

/**
 * Segmented status switcher for the request list (FR-018; 002 research R7). Native radios give
 * one tab stop, arrow-key movement, and the checked state to assistive technology. A count is
 * shown only on the selected status, because only its records are loaded (002 research R6).
 */
@Component({
  selector: 'app-status-filter',
  template: `
    <fieldset class="segmented">
      <legend class="visually-hidden">Show</legend>
      @for (status of statuses; track status) {
        <label class="segment" [class.is-selected]="status === value()">
          <input
            type="radio"
            name="status-filter"
            class="visually-hidden"
            [value]="status"
            [checked]="status === value()"
            (change)="changed.emit(status)"
          />
          <span>{{ status }}</span>
          @if (status === value() && count() !== null) {
            <span class="count tabular">
              {{ count() }}<span class="visually-hidden"> requests</span>
            </span>
          }
        </label>
      }
    </fieldset>
  `,
  styles: `
    @use 'variables' as *;
    .segmented {
      display: inline-flex;
      margin: 0;
      padding: 3px;
      min-width: 0;
      border: 1px solid $color-border;
      border-radius: $radius-md;
      background: $color-surface-sunken;
    }
    .segment {
      position: relative;
      display: inline-flex;
      align-items: center;
      gap: $space-2;
      margin: 0;
      min-height: 2.25rem;
      padding: $space-1 $space-3;
      border-radius: $radius-sm;
      font-size: $font-size-sm;
      font-weight: $font-weight-medium;
      color: $color-text-muted;
      cursor: pointer;
      white-space: nowrap;
      transition:
        background-color $duration-fast $easing-standard,
        color $duration-fast $easing-standard,
        box-shadow $duration-fast $easing-standard;
      &:hover {
        color: $color-text;
      }
      &.is-selected {
        background: $color-surface;
        color: $color-accent-2;
        font-weight: $font-weight-semibold;
        box-shadow: 0 0 0 1px rgba($glow-indigo, 0.28), $shadow-sm;
      }
      @media (max-width: ($breakpoint-md - 1px)) {
        min-height: $target-min;
        min-width: $target-min;
      }
      &:has(input:focus-visible) {
        outline: $focus-outline;
        outline-offset: 0;
      }
    }
    .count {
      min-width: 1.375rem;
      padding: 0 0.375rem;
      border-radius: $radius-pill;
      background: rgba($glow-indigo, 0.12);
      color: $color-accent-2;
      font-size: $font-size-xs;
      font-weight: $font-weight-bold;
      line-height: 1.375rem;
      text-align: center;
    }
    @media (forced-colors: active) {
      .segment.is-selected {
        outline: 2px solid Highlight;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusFilterComponent {
  readonly value = input<RequestStatus>('Pending');
  /** Exact number of records loaded for the selected status, or null when not known. */
  readonly count = input<number | null>(null);
  readonly changed = output<RequestStatus>();

  protected readonly statuses = REQUEST_STATUSES;
}
