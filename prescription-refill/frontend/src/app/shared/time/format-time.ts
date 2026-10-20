// One shared format for every instant (FR-024; research R12): `d MMM yyyy, HH:mm (UTC±hh:mm)`.

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

const pad = (value: number) => String(value).padStart(2, '0');

/**
 * Formats an ISO 8601 instant in local time. `offsetMinutes` is the local offset east of UTC;
 * by default it is the browser's offset at that instant.
 */
export function formatInstant(iso: string, offsetMinutes?: number): string {
  const instant = parseInstant(iso);
  const offset = offsetMinutes ?? -instant.getTimezoneOffset();
  const local = new Date(instant.getTime() + offset * 60_000);

  const sign = offset < 0 ? '-' : '+';
  const absolute = Math.abs(offset);
  const zone = `UTC${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`;

  return (
    `${local.getUTCDate()} ${MONTHS[local.getUTCMonth()]} ${local.getUTCFullYear()}, ` +
    `${pad(local.getUTCHours())}:${pad(local.getUTCMinutes())} (${zone})`
  );
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
export function formatDateOnly(date: string): string {
  const [year, month, day] = date.split('-').map(Number);
  return `${day} ${MONTHS[month - 1]} ${year}`;
}
