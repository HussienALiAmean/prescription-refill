import { LRI, PDI, formatDateOnly, formatInstant, utcTitle } from './format-time';

describe('format-time (FR-024)', () => {
  const iso = '2026-10-20T11:05:12.3456789+00:00';

  it('formats an instant in a given local offset with the offset shown', () => {
    expect(formatInstant(iso, 180)).toBe('20 Oct 2026, 14:05 (UTC+03:00)');
  });

  it('formats negative and fractional-hour offsets', () => {
    expect(formatInstant(iso, -300)).toBe('20 Oct 2026, 06:05 (UTC-05:00)');
    expect(formatInstant(iso, 330)).toBe('20 Oct 2026, 16:35 (UTC+05:30)');
  });

  it('rolls over the date when the local offset crosses midnight', () => {
    expect(formatInstant('2026-10-20T22:30:00+00:00', 120)).toBe('21 Oct 2026, 00:30 (UTC+02:00)');
  });

  it('uses the browser offset for the instant when none is given', () => {
    const offset = -new Date(iso).getTimezoneOffset();
    expect(formatInstant(iso)).toBe(formatInstant(iso, offset));
  });

  it('keeps the exact ISO UTC value for the title', () => {
    expect(utcTitle(iso)).toBe('2026-10-20T11:05:12.3456789+00:00 (UTC)');
  });

  it('formats a date of birth without any time-zone shift', () => {
    expect(formatDateOnly('1984-03-12')).toBe('12 Mar 1984');
    expect(formatDateOnly('2001-01-01')).toBe('1 Jan 2001');
  });
  // spec 005 FR-016, research R14: Arabic month names, the Arabic comma, Western digits, and the
  // time and offset in a left-to-right isolate. English stays as above.
  it('formats an instant in Arabic with Western digits and an isolated time', () => {
    expect(formatInstant(iso, 180, 'ar')).toBe(`20 أكتوبر 2026، ${LRI}14:05 (UTC+03:00)${PDI}`);
    expect(formatInstant(iso, -300, 'ar')).toBe(`20 أكتوبر 2026، ${LRI}06:05 (UTC-05:00)${PDI}`);
    expect(formatInstant(iso, 180, 'ar')).not.toMatch(/[٠-٩۰-۹]/);
  });

  it('formats a date of birth with Arabic month names', () => {
    expect(formatDateOnly('1984-03-12', 'ar')).toBe('12 مارس 1984');
    expect(formatDateOnly('2001-12-01', 'ar')).toBe('1 ديسمبر 2001');
  });

  it('keeps English unchanged when asked explicitly', () => {
    expect(formatInstant(iso, 180, 'en')).toBe('20 Oct 2026, 14:05 (UTC+03:00)');
    expect(formatDateOnly('1984-03-12', 'en')).toBe('12 Mar 1984');
  });
});
