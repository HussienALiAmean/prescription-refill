import { ar } from '../../core/i18n/ar';
import { en } from '../../core/i18n/en';
import { Language } from '../../core/i18n/language';

// One shared format for every instant (FR-024; research R12): `d MMM yyyy, HH:mm (UTC±hh:mm)`.
// Arabic keeps the format with Arabic month names, the Arabic comma, and Western digits; the time
// and offset sit in a left-to-right isolate so `UTC+03:00` keeps its order (spec 005 FR-015,
// FR-016, research R14). Hand-written rather than Intl, whose Arabic output varies by browser.
const MONTHS: Record<Language, readonly string[]> = { en: en.time.months, ar: ar.time.months };

/** Unicode LEFT-TO-RIGHT ISOLATE and POP DIRECTIONAL ISOLATE. */
export const LRI = '\u2066';
export const PDI = '\u2069';

const pad = (value: number) => String(value).padStart(2, '0');

/**
 * Formats an ISO 8601 instant in local time. `offsetMinutes` is the local offset east of UTC;
 * by default it is the browser's offset at that instant.
 */
export function formatInstant(iso: string, offsetMinutes?: number, language: Language = 'en'): string {
  const instant = parseInstant(iso);
  const offset = offsetMinutes ?? -instant.getTimezoneOffset();
  const local = new Date(instant.getTime() + offset * 60_000);

  const sign = offset < 0 ? '-' : '+';
  const absolute = Math.abs(offset);
  const zone = `UTC${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`;

  const date = `${local.getUTCDate()} ${MONTHS[language][local.getUTCMonth()]} ${local.getUTCFullYear()}`;
  const time = `${pad(local.getUTCHours())}:${pad(local.getUTCMinutes())} (${zone})`;
  return language === 'ar' ? `${date}، ${LRI}${time}${PDI}` : `${date}, ${time}`;
}

/**
 * Parses an API instant. The API sends up to 7 fractional digits (.NET ticks); JavaScript dates
 * are only guaranteed to parse 3, so extra digits are dropped (display precision is minutes).
 */
export function parseInstant(iso: string): Date {
  return new Date(iso.replace(/(\.\d{3})\d+/, '$1'));
}

/** The exact UTC value from the API, for a `title` attribute or details. */
export function utcTitle(iso: string): string {
  return `${iso} (UTC)`;
}

/** Formats a `yyyy-MM-dd` date (for example a date of birth) without time-zone conversion. */
export function formatDateOnly(date: string, language: Language = 'en'): string {
  const [year, month, day] = date.split('-').map(Number);
  return `${day} ${MONTHS[language][month - 1]} ${year}`;
}
