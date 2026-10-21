import { Provider } from '@angular/core';
import { LANGUAGE_KEY, PREFERENCES_STORAGE, THEME_KEY, Theme } from '../preferences/preferences.store';
import { Language } from './language';

/** A Storage held in memory, so specs never touch the browser's real preferences. */
export function memoryStorage(initial: Record<string, string> = {}): Storage {
  const values = new Map(Object.entries(initial));
  return {
    get length() {
      return values.size;
    },
    clear: () => values.clear(),
    getItem: (key) => values.get(key) ?? null,
    key: (index) => [...values.keys()][index] ?? null,
    removeItem: (key) => void values.delete(key),
    setItem: (key, value) => void values.set(key, String(value)),
  };
}

/**
 * Starts specs in a fixed language and theme. Existing specs use English so their assertions keep
 * their meaning now that Arabic is the default (spec 005 SC-010).
 */
export function provideI18nTesting(language: Language = 'en', theme: Theme = 'light'): Provider[] {
  return [{ provide: PREFERENCES_STORAGE, useValue: memoryStorage({ [LANGUAGE_KEY]: language, [THEME_KEY]: theme }) }];
}
