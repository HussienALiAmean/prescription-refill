import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18nService } from '../../core/i18n/i18n.service';
import { formatInstant, utcTitle } from './format-time';

/**
 * Renders an API instant in local time with the exact UTC value on hover (FR-024), with month
 * names in the active language (spec 005 FR-016).
 */
@Component({
  selector: 'app-instant',
  template: `<time [attr.datetime]="value()" [title]="title()">{{ text() }}</time>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstantComponent {
  private readonly i18n = inject(I18nService);

  readonly value = input.required<string>();

  protected readonly text = computed(() => formatInstant(this.value(), undefined, this.i18n.language()));
  protected readonly title = computed(() => utcTitle(this.value()));
}
