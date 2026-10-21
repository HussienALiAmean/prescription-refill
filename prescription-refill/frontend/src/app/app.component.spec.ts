import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';
import { provideFakeRefillEvents } from './core/realtime/testing/fake-refill-events';
import { AmbientLightDirective } from './shared/ambient/ambient-light.directive';
import { I18nService } from './core/i18n/i18n.service';
import { provideI18nTesting } from './core/i18n/testing';
import { ThemeService } from './core/theme/theme.service';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [...provideI18nTesting(), provideRouter([]), provideFakeRefillEvents()],
    }).compileComponents();
  });

  afterEach(() => {
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
    delete document.documentElement.dataset['theme'];
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
  // spec 005 FR-008, FR-026, FR-035 (ui-contract "Header controls").
  it('offers language and theme controls in the header, each language named in itself', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

    const language = header.querySelector('[data-testid="language-switch"]')!;
    expect(language.querySelector('legend')?.textContent?.trim()).toBe('Language');
    expect(Array.from(language.querySelectorAll('label')).map((l) => [l.getAttribute('lang'), l.textContent?.trim()])).toEqual([
      ['ar', 'العربية'],
      ['en', 'English'],
    ]);
    expect(language.querySelector<HTMLInputElement>('input[data-value="en"]')!.checked).toBeTrue();

    const theme = header.querySelector('[data-testid="theme-switch"]')!;
    expect(theme.querySelector('legend')?.textContent?.trim()).toBe('Theme');
    expect(Array.from(theme.querySelectorAll('input')).map((i) => i.dataset['value'])).toEqual(['light', 'dark']);
    expect(theme.querySelector<HTMLInputElement>('input[data-value="light"]')!.checked).toBeTrue();
  });

  it('switches language and theme independently and announces each in the new language', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const choose = (testId: string, value: string) => {
      const input = el.querySelector<HTMLInputElement>(`[data-testid="${testId}"] input[data-value="${value}"]`)!;
      input.checked = true;
      input.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    };
    const announcement = () => el.querySelector('[data-testid="preference-announcement"]')?.textContent?.trim();

    choose('theme-switch', 'dark');
    expect(TestBed.inject(ThemeService).theme()).toBe('dark');
    expect(TestBed.inject(I18nService).language()).toBe('en');
    expect(announcement()).toBe('Dark theme on');

    choose('language-switch', 'ar');
    expect(TestBed.inject(I18nService).language()).toBe('ar');
    expect(TestBed.inject(ThemeService).theme()).toBe('dark');
    expect(announcement()).toBe('تم التبديل إلى العربية');
    expect(el.querySelector('[role="note"]')?.textContent).toContain('بيانات تجريبية خيالية');
    expect(el.querySelector('[data-testid="theme-switch"] legend')?.textContent?.trim()).toBe('المظهر');
  });
});
