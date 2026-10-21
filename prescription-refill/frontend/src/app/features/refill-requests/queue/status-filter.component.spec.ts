import { TestBed } from '@angular/core/testing';
import { RequestStatus } from '../data/models';
import { StatusFilterComponent } from './status-filter.component';
import { provideI18nTesting } from '../../../core/i18n/testing';

// Rewritten for the segmented switcher (002 research R7, intentional change). The previous
// assertions (labeled "Show", three options in order, current selection, change emitted) are
// kept against radios; count display is new.
describe('StatusFilterComponent', () => {
  // English, so these assertions keep their meaning with the Arabic default (spec 005 SC-010).
  beforeEach(() => TestBed.configureTestingModule({ providers: provideI18nTesting() }));

  function render(value: RequestStatus, count: number | null = null) {
    const fixture = TestBed.createComponent(StatusFilterComponent);
    fixture.componentRef.setInput('value', value);
    fixture.componentRef.setInput('count', count);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  const radios = (el: HTMLElement) => Array.from(el.querySelectorAll<HTMLInputElement>('input[type="radio"]'));

  it('is a radio group named "Show" with the current status checked, emitting changes', () => {
    const { fixture, el } = render('Pending');
    let changed: RequestStatus | undefined;
    fixture.componentInstance.changed.subscribe((value) => (changed = value));

    expect(el.querySelector('fieldset legend')?.textContent).toContain('Show');
    expect(radios(el).map((r) => r.value)).toEqual(['Pending', 'Approved', 'Denied']);
    expect(new Set(radios(el).map((r) => r.name)).size).toBe(1);
    expect(radios(el).find((r) => r.checked)?.value).toBe('Pending');

    radios(el)[2].click();

    expect(changed).toBe('Denied');
  });

  it('labels every option with its visible status text', () => {
    const { el } = render('Approved');

    for (const radio of radios(el)) {
      expect(radio.closest('label')?.textContent).toContain(radio.value);
    }
    expect(radios(el).find((r) => r.checked)?.value).toBe('Approved');
  });

  it('shows a count only on the selected status, and only when it is known', () => {
    const withCount = render('Approved', 7).el;
    const counts = withCount.querySelectorAll('.count');

    expect(counts.length).toBe(1);
    expect(counts[0].closest('label')?.textContent).toContain('Approved');
    expect(counts[0].textContent).toContain('7');

    expect(render('Approved', null).el.querySelector('.count')).toBeNull();
  });
});
