import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AlertComponent } from './alert.component';
import { EmptyStateComponent } from './empty-state.component';
import { provideI18nTesting } from '../../core/i18n/testing';

@Component({
  imports: [AlertComponent, EmptyStateComponent],
  template: `
    <app-alert kind="danger" heading="Blocked">Danger text</app-alert>
    <app-alert kind="warning" heading="Warn">Warning text</app-alert>
    <app-empty-state heading="Nothing yet" message="No pending requests.">
      <button type="button" class="action">Clear search</button>
    </app-empty-state>
  `,
})
class HostComponent {}

describe('shared UI (redesign)', () => {
  // English, so these assertions keep their meaning with the Arabic default (spec 005 SC-010).
  beforeEach(() => TestBed.configureTestingModule({ providers: provideI18nTesting() }));

  it('gives each alert kind its own decorative icon', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const alerts = (fixture.nativeElement as HTMLElement).querySelectorAll('.alert');

    const icon = (alert: Element) => alert.querySelector('app-icon');
    expect(icon(alerts[0])?.getAttribute('aria-hidden')).toBe('true');
    expect(icon(alerts[0])?.innerHTML).not.toEqual(icon(alerts[1])?.innerHTML ?? '');
  });

  it('renders an empty state heading and a projected action', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('app-empty-state h2')?.textContent).toContain('Nothing yet');
    expect(el.querySelector('.empty-state')?.textContent).toContain('No pending requests.');
    expect(el.querySelector('app-empty-state button.action')?.textContent).toContain('Clear search');
  });
});
