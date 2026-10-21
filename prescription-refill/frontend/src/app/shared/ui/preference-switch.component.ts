import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IconComponent, IconName } from './icon.component';

export interface PreferenceOption<T extends string> {
  value: T;
  label: string;
  /** Language of the label, when it differs from the page (each language is named in itself). */
  lang?: string;
  icon?: IconName;
}

/**
 * Two-option preference control for the header (spec 005 FR-008, FR-026; ui-contract "Header
 * controls"). Native radios in a fieldset, as in the status filter (002 research R7), give one
 * tab stop, arrow-key movement, and the checked state to assistive technology. Selecting applies
 * at once; focus stays on the chosen option.
 */
@Component({
  selector: 'app-preference-switch',
  imports: [IconComponent],
  template: `
    <fieldset class="switch" [attr.data-testid]="testId()">
      <legend class="visually-hidden">{{ legend() }}</legend>
      @for (option of options(); track option.value) {
        <label class="option" [class.is-selected]="option.value === value()" [attr.lang]="option.lang ?? null">
          <input
            type="radio"
            class="visually-hidden"
            [name]="name()"
            [value]="option.value"
            [checked]="option.value === value()"
            [attr.data-value]="option.value"
            (change)="changed.emit(option.value)"
          />
          @if (option.icon) {
            <app-icon [name]="option.icon" [size]="14" />
          }
          <span>{{ option.label }}</span>
        </label>
      }
    </fieldset>
  `,
  styles: `
    @use 'variables' as *;
    .switch {
      display: inline-flex;
      margin: 0;
      padding: 2px;
      min-width: 0;
      border: 1px solid $canvas-border;
      border-radius: $radius-pill;
      background: $canvas-surface;
    }
    .option {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: $space-1;
      margin: 0;
      min-height: 2rem;
      padding: 0 $space-3;
      border-radius: $radius-pill;
      color: $canvas-text-muted;
      font-size: $font-size-xs;
      font-weight: $font-weight-semibold;
      cursor: pointer;
      white-space: nowrap;
      &:hover {
        color: $canvas-text;
      }
      &.is-selected {
        background: $canvas-accent;
        color: $canvas-bg; // 12.1:1 on $canvas-accent
      }
      // On the dark canvas the focus ring uses the canvas accent (12.1:1 on $canvas-bg).
      &:has(input:focus-visible) {
        outline: 2px solid $canvas-accent;
        outline-offset: 2px;
      }
      @media (max-width: ($breakpoint-md - 1px)) {
        min-height: $target-min;
        min-width: $target-min;
      }
    }
    @media (forced-colors: active) {
      .switch {
        border-color: CanvasText;
      }
      .option.is-selected {
        outline: 2px solid Highlight;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PreferenceSwitchComponent<T extends string = string> {
  /** Accessible name of the group, in the active language. */
  readonly legend = input.required<string>();
  readonly name = input.required<string>();
  readonly options = input.required<readonly PreferenceOption<T>[]>();
  readonly value = input.required<T>();
  readonly testId = input<string>();
  readonly changed = output<T>();
}
