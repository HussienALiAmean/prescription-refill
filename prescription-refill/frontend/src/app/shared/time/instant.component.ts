import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { formatInstant, utcTitle } from './format-time';

/** Renders an API instant in local time with the exact UTC value on hover (FR-024). */
@Component({
  selector: 'app-instant',
  template: `<time [attr.datetime]="value()" [title]="title()">{{ text() }}</time>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstantComponent {
  readonly value = input.required<string>();

  protected readonly text = computed(() => formatInstant(this.value()));
  protected readonly title = computed(() => utcTitle(this.value()));
}
