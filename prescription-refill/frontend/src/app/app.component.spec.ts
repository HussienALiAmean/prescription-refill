import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';
import { provideFakeRefillEvents } from './core/realtime/testing/fake-refill-events';
import { AmbientLightDirective } from './shared/ambient/ambient-light.directive';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), provideFakeRefillEvents()],
    }).compileComponents();
  });

  it('shows the persistent fictional-data and no-sign-in notice (FR-019, FR-020)', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const notice = (fixture.nativeElement as HTMLElement).querySelector('[role="note"]');

    expect(notice?.textContent).toContain('Fictional demo data — not clinical guidance');
    expect(notice?.textContent).toContain('No sign-in');
    expect(notice?.textContent).toContain('not for real patient data');
  });

  // 004 US3: one shared ambient light on the shell; its layer is decorative only.
  it('hosts the ambient light on the shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    expect(fixture.debugElement.injector.get(AmbientLightDirective, null)).not.toBeNull();
  });

  it('marks the decorative layer as hidden from assistive technology and non-interactive', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const layer = (fixture.nativeElement as HTMLElement).querySelector('.ambient-layer') as HTMLElement;

    expect(layer.getAttribute('aria-hidden')).toBe('true');
    expect(getComputedStyle(layer).pointerEvents).toBe('none');
  });
});
