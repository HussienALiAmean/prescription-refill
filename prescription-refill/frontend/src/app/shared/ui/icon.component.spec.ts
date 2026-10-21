import { TestBed } from '@angular/core/testing';
import { IconComponent } from './icon.component';
import { provideI18nTesting } from '../../core/i18n/testing';

describe('IconComponent', () => {
  // English, so these assertions keep their meaning with the Arabic default (spec 005 SC-010).
  beforeEach(() => TestBed.configureTestingModule({ providers: provideI18nTesting() }));

  function render(name: string) {
    const fixture = TestBed.createComponent(IconComponent);
    fixture.componentRef.setInput('name', name);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders a decorative svg for a known icon', () => {
    const el = render('alert-octagon');

    expect(el.getAttribute('aria-hidden')).toBe('true');
    expect(el.querySelector('svg')).not.toBeNull();
    expect(el.querySelectorAll('path').length).toBeGreaterThan(0);
  });

  it('renders nothing for an unknown icon', () => {
    expect(render('no-such-icon').querySelector('svg')).toBeNull();
  });
  // spec 005 FR-014: only directional icons mirror in right-to-left layouts.
  it('marks back and chevron icons as directional and mirrors them under rtl only', () => {
    const container = document.createElement('div');
    document.body.appendChild(container);
    try {
      for (const [name, directional] of [['arrow-left', true], ['chevron-right', true], ['alert-triangle', false], ['check-circle', false]] as const) {
        const el = render(name);
        container.appendChild(el);
        expect(el.classList.contains('icon-directional')).withContext(name).toBe(directional);

        container.dir = 'rtl';
        const mirrored = getComputedStyle(el.querySelector('svg')!).transform;
        container.dir = 'ltr';
        const plain = getComputedStyle(el.querySelector('svg')!).transform;

        expect(plain).withContext(name).toBe('none');
        expect(mirrored).withContext(name).toBe(directional ? 'matrix(-1, 0, 0, 1, 0, 0)' : 'none');
      }
    } finally {
      container.remove();
    }
  });
});
