import { TestBed } from '@angular/core/testing';
import { PreferenceSwitchComponent } from './preference-switch.component';

describe('PreferenceSwitchComponent', () => {
  function render(value = 'ar') {
    const fixture = TestBed.createComponent(PreferenceSwitchComponent<string>);
    fixture.componentRef.setInput('legend', 'Language');
    fixture.componentRef.setInput('name', 'language');
    fixture.componentRef.setInput('testId', 'language-switch');
    fixture.componentRef.setInput('options', [
      { value: 'ar', label: 'العربية', lang: 'ar' },
      { value: 'en', label: 'English', lang: 'en' },
    ]);
    fixture.componentRef.setInput('value', value);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('is a named radio group with one checked option (spec 005 FR-008)', () => {
    const { el } = render('en');

    const group = el.querySelector('fieldset[data-testid="language-switch"]')!;
    expect(group.querySelector('legend')?.textContent?.trim()).toBe('Language');
    const radios = Array.from(group.querySelectorAll<HTMLInputElement>('input[type="radio"]'));
    expect(radios.map((r) => r.dataset['value'])).toEqual(['ar', 'en']);
    expect(radios.map((r) => r.checked)).toEqual([false, true]);
    expect(new Set(radios.map((r) => r.name)).size).toBe(1);
  });

  it('names each language in that language', () => {
    const { el } = render();

    const labels = Array.from(el.querySelectorAll('label'));
    expect(labels.map((l) => [l.getAttribute('lang'), l.textContent?.trim()])).toEqual([
      ['ar', 'العربية'],
      ['en', 'English'],
    ]);
  });

  it('reports the chosen option', () => {
    const { fixture, el } = render();
    const chosen: string[] = [];
    fixture.componentInstance.changed.subscribe((value) => chosen.push(value));

    const english = el.querySelector<HTMLInputElement>('input[data-value="en"]')!;
    english.checked = true;
    english.dispatchEvent(new Event('change'));

    expect(chosen).toEqual(['en']);
  });
});
