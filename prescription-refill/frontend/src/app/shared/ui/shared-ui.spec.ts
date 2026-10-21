import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AlertComponent } from './alert.component';
import { EmptyStateComponent } from './empty-state.component';
import { ErrorStateComponent } from './error-state.component';
import { StatusBadgeComponent } from './status-badge.component';
import { provideI18nTesting } from '../../core/i18n/testing';

@Component({
  imports: [AlertComponent, EmptyStateComponent, ErrorStateComponent, StatusBadgeComponent],
  template: `
    <app-alert kind="danger" heading="Blocked">Danger text</app-alert>
    <app-alert kind="warning">Warning text</app-alert>
    <app-empty-state message="Nothing here" />
    <app-error-state message="Load failed" (retry)="retries = retries + 1" />
    <app-status-badge label="Pending" tone="warning" />
  `,
})
class HostComponent {
  retries = 0;
}

describe('shared UI', () => {
  // English, so these assertions keep their meaning with the Arabic default (spec 005 SC-010).
  beforeEach(() => TestBed.configureTestingModule({ providers: provideI18nTesting() }));

  it('renders alerts with roles, empty and error states, and badges', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;

    const alerts = el.querySelectorAll('.alert');
    expect(alerts[0].getAttribute('role')).toBe('alert');
    expect(alerts[0].textContent).toContain('Blocked');
    expect(alerts[1].getAttribute('role')).toBe('status');
    expect(el.querySelector('.empty-state')?.textContent).toContain('Nothing here');
    expect(el.querySelector('.badge')?.textContent).toContain('Pending');

    (el.querySelector('app-error-state button') as HTMLButtonElement).click();
    expect(fixture.componentInstance.retries).toBe(1);
  });
});
