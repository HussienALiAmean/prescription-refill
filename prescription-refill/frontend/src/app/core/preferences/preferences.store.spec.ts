import { TestBed } from '@angular/core/testing';
import { memoryStorage } from '../i18n/testing';
import { LANGUAGE_KEY, PREFERENCES_STORAGE, PreferencesStore, THEME_KEY } from './preferences.store';

describe('PreferencesStore', () => {
  function store(storage: Storage | null): PreferencesStore {
    TestBed.configureTestingModule({ providers: [{ provide: PREFERENCES_STORAGE, useValue: storage }] });
    return TestBed.inject(PreferencesStore);
  }

  it('defaults to Arabic and Light without saved values (spec 005 FR-009, FR-027)', () => {
    const preferences = store(memoryStorage());

    expect(preferences.language()).toBe('ar');
    expect(preferences.theme()).toBe('light');
  });

  it('restores valid saved values', () => {
    const preferences = store(memoryStorage({ [LANGUAGE_KEY]: 'en', [THEME_KEY]: 'dark' }));

    expect(preferences.language()).toBe('en');
    expect(preferences.theme()).toBe('dark');
  });

  it('ignores invalid values without rewriting them', () => {
    const storage = memoryStorage({ [LANGUAGE_KEY]: 'fr', [THEME_KEY]: 'sepia' });
    const preferences = store(storage);

    expect(preferences.language()).toBe('ar');
    expect(preferences.theme()).toBe('light');
    expect(storage.getItem(LANGUAGE_KEY)).toBe('fr');
    expect(storage.getItem(THEME_KEY)).toBe('sepia');
  });

  it('falls back to the defaults when storage throws or is unavailable', () => {
    const throwing = memoryStorage();
    throwing.getItem = () => {
      throw new Error('blocked');
    };
    throwing.setItem = () => {
      throw new Error('blocked');
    };

    const preferences = store(throwing);
    expect(preferences.language()).toBe('ar');
    expect(() => preferences.setTheme('dark')).not.toThrow();

    TestBed.resetTestingModule();
    const none = store(null);
    expect(none.theme()).toBe('light');
    expect(() => none.setLanguage('en')).not.toThrow();
  });

  it('writes each preference to its own key only', () => {
    const storage = memoryStorage({ [THEME_KEY]: 'dark' });
    const preferences = store(storage);

    preferences.setLanguage('en');
    expect(storage.getItem(LANGUAGE_KEY)).toBe('en');
    expect(storage.getItem(THEME_KEY)).toBe('dark');

    preferences.setTheme('light');
    expect(storage.getItem(THEME_KEY)).toBe('light');
    expect(storage.getItem(LANGUAGE_KEY)).toBe('en');
  });
});
