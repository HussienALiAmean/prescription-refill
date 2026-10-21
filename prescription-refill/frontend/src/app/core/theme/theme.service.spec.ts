import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { I18nService } from '../i18n/i18n.service';
import { memoryStorage } from '../i18n/testing';
import { LANGUAGE_KEY, PREFERENCES_STORAGE, THEME_KEY } from '../preferences/preferences.store';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let storage: Storage;
  let meta: HTMLMetaElement;

  beforeEach(() => {
    meta = document.createElement('meta');
    meta.name = 'color-scheme';
    meta.content = 'light';
    document.head.appendChild(meta);
  });

  afterEach(() => {
    meta.remove();
    const root = TestBed.inject(DOCUMENT).documentElement;
    delete root.dataset['theme'];
    root.lang = 'en';
    root.dir = 'ltr';
  });

  function create(initial: Record<string, string> = {}): ThemeService {
    storage = memoryStorage(initial);
    TestBed.configureTestingModule({ providers: [{ provide: PREFERENCES_STORAGE, useValue: storage }] });
    return TestBed.inject(ThemeService);
  }

  const root = () => TestBed.inject(DOCUMENT).documentElement;

  it('starts in Light and sets the document theme (spec 005 FR-027)', () => {
    const themes = create();

    expect(themes.theme()).toBe('light');
    expect(root().dataset['theme']).toBe('light');
    expect(meta.content).toBe('light');
  });

  it('restores Dark and treats an invalid value as Light', () => {
    expect(create({ [THEME_KEY]: 'dark' }).theme()).toBe('dark');
    expect(root().dataset['theme']).toBe('dark');
    expect(meta.content).toBe('dark');

    TestBed.resetTestingModule();
    expect(create({ [THEME_KEY]: 'neon' }).theme()).toBe('light');
  });

  it('falls back to Light when storage throws', () => {
    const throwing = memoryStorage();
    throwing.getItem = () => {
      throw new Error('blocked');
    };
    TestBed.configureTestingModule({ providers: [{ provide: PREFERENCES_STORAGE, useValue: throwing }] });

    expect(TestBed.inject(ThemeService).theme()).toBe('light');
  });

  it('changing the theme never touches the language, and the reverse (spec 005 FR-009, FR-027)', () => {
    const themes = create({ [LANGUAGE_KEY]: 'en' });
    const i18n = TestBed.inject(I18nService);

    themes.setTheme('dark');
    expect(storage.getItem(THEME_KEY)).toBe('dark');
    expect(storage.getItem(LANGUAGE_KEY)).toBe('en');
    expect(i18n.language()).toBe('en');
    expect(root().lang).toBe('en');
    expect(root().dir).toBe('ltr');

    i18n.setLanguage('ar');
    expect(themes.theme()).toBe('dark');
    expect(storage.getItem(THEME_KEY)).toBe('dark');
    expect(root().dataset['theme']).toBe('dark');
  });
});
