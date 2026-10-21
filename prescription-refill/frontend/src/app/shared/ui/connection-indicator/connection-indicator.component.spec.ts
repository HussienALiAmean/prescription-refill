import { TestBed } from '@angular/core/testing';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';
import { ConnectionIndicatorComponent } from './connection-indicator.component';
import { provideI18nTesting } from '../../../core/i18n/testing';

describe('ConnectionIndicatorComponent', () => {
  let events: FakeRefillEvents;

  function render() {
    const fixture = TestBed.createComponent(ConnectionIndicatorComponent);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => {
    events = new FakeRefillEvents();
    TestBed.configureTestingModule({ providers: [...provideI18nTesting(), provideFakeRefillEvents(events)] });
  });

  it('shows nothing while connected', () => {
    expect(render().el.textContent?.trim()).toBe('');
  });

  it('tells the user to use Refresh while live updates are unavailable', () => {
    events.connectionState.set('disconnected');

    const { el } = render();

    expect(el.querySelector('[role="status"]')?.textContent).toContain('Live updates unavailable');
    expect(el.textContent).toContain('Refresh');
  });

  it('shows reconnecting', () => {
    events.connectionState.set('reconnecting');

    expect(render().el.textContent).toContain('Reconnecting');
  });
});
