import { Injectable, InjectionToken, inject } from '@angular/core';
import { DEFAULT_LANGUAGE, Language, isLanguage } from '../i18n/language';

export type Theme = 'light' | 'dark';

export const DEFAULT_THEME: Theme = 'light';

export function isTheme(value: unknown): value is Theme {
  return value === 'light' || value === 'dark';
}

/** Storage keys, shared with the boot script in index.html (spec 005 research R11). */
export const LANGUAGE_KEY = 'prx.language';
export const THEME_KEY = 'prx.theme';

/**
 * Where preferences live; null when the browser blocks storage. Tests provide an in-memory
 * storage instead of the real one.
 */
export const PREFERENCES_STORAGE = new InjectionToken<Storage | null>('PREFERENCES_STORAGE', {
  providedIn: 'root',
  factory: () => {
    try {
      return globalThis.localStorage ?? null;
    } catch {
      return null;
    }
  },
});

/**
 * Browser-held language and theme preferences (spec 005 FR-009, FR-027). Each key is read and
 * written on its own, so one preference never changes the other. A missing, invalid, or
 * unreadable value yields the default and is left as it is until the user chooses.
 */
@Injectable({ providedIn: 'root' })
export class PreferencesStore {
  private readonly storage = inject(PREFERENCES_STORAGE);

  language(): Language {
    const value = this.read(LANGUAGE_KEY);
    return isLanguage(value) ? value : DEFAULT_LANGUAGE;
  }

  setLanguage(language: Language): void {
    this.write(LANGUAGE_KEY, language);
  }

  theme(): Theme {
    const value = this.read(THEME_KEY);
    return isTheme(value) ? value : DEFAULT_THEME;
  }

  setTheme(theme: Theme): void {
    this.write(THEME_KEY, theme);
  }

  private read(key: string): string | null {
    try {
      return this.storage?.getItem(key) ?? null;
    } catch {
      return null;
    }
  }

  private write(key: string, value: string): void {
    try {
      this.storage?.setItem(key, value);
    } catch {
      // Storage is full or blocked: the choice still applies for this page (spec edge case).
    }
  }
}
