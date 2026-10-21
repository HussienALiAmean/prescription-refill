import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { LANGUAGE_KEY, PREFERENCES_STORAGE, THEME_KEY } from '../preferences/preferences.store';
import { I18nService } from './i18n.service';
import { memoryStorage } from './testing';

describe('I18nService', () => {
  let storage: Storage;

  function create(initial: Record<string, string> = {}): I18nService {
    storage = memoryStorage(initial);
    TestBed.configureTestingModule({ providers: [{ provide: PREFERENCES_STORAGE, useValue: storage }] });
    return TestBed.inject(I18nService);
  }

  const root = () => TestBed.inject(DOCUMENT).documentElement;

  it('starts in Arabic, right-to-left, with an Arabic title (spec 005 FR-007, FR-013)', () => {
    const i18n = create();

    expect(i18n.language()).toBe('ar');
    expect(i18n.direction()).toBe('rtl');
    expect(root().lang).toBe('ar');
    expect(root().dir).toBe('rtl');
    expect(TestBed.inject(DOCUMENT).title).toBe('طلبات إعادة الصرف');
    expect(i18n.t().decision.approve).toBe('موافقة');
  });

  it('restores a saved English choice', () => {
    const i18n = create({ [LANGUAGE_KEY]: 'en' });

    expect(i18n.language()).toBe('en');
    expect(root().dir).toBe('ltr');
    expect(i18n.t().decision.approve).toBe('Approve');
  });

  it('switches language, saves it, and announces the change once', () => {
    const i18n = create({ [THEME_KEY]: 'dark' });
    const changes: string[] = [];
    i18n.languageChanged$.subscribe((language) => changes.push(language));

    i18n.setLanguage('en');
    i18n.setLanguage('en');

    expect(changes).toEqual(['en']);
    expect(root().lang).toBe('en');
    expect(root().dir).toBe('ltr');
    expect(TestBed.inject(DOCUMENT).title).toBe('Refill requests');
    expect(storage.getItem(LANGUAGE_KEY)).toBe('en');
    // The theme preference is untouched (spec 005 FR-009).
    expect(storage.getItem(THEME_KEY)).toBe('dark');
  });

  it('does not emit for the initial language', () => {
    const i18n = create({ [LANGUAGE_KEY]: 'en' });
    let emitted = false;
    i18n.languageChanged$.subscribe(() => (emitted = true));

    expect(emitted).toBeFalse();
  });

  afterEach(() => {
    // Leave the test page as Karma rendered it.
    root().lang = 'en';
    root().dir = 'ltr';
  });
});
