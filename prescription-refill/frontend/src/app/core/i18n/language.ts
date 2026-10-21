/** Supported interface languages (spec 005 FR-007). Arabic is the default. */
export type Language = 'ar' | 'en';

export type Direction = 'rtl' | 'ltr';

export const LANGUAGES: readonly Language[] = ['ar', 'en'];

export const DEFAULT_LANGUAGE: Language = 'ar';

export function isLanguage(value: unknown): value is Language {
  return value === 'ar' || value === 'en';
}

export function directionOf(language: Language): Direction {
  return language === 'ar' ? 'rtl' : 'ltr';
}
