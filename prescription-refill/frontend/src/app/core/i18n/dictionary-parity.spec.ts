import { ar } from './ar';
import { en } from './en';

/** Values that are the same in both languages on purpose. */
const SAME_IN_BOTH = new Set(['UTC']);

type Entry = { path: string; value: unknown };

function entries(node: unknown, path = ''): Entry[] {
  if (node !== null && typeof node === 'object' && !Array.isArray(node)) {
    return Object.entries(node).flatMap(([key, value]) => entries(value, path ? `${path}.${key}` : key));
  }
  return [{ path, value: node }];
}

/** Sample arguments for each parameterized entry, so its output can be compared. */
const SAMPLE_ARGS: Record<string, unknown[]> = {
  'queue.matches': [3, 12],
  'queue.count': [12],
  'queue.empty': ['Pending'],
  'queue.noMatch': ['Avelorin'],
  'pagination.position': [2, 10],
  'review.eyebrow': [5],
  'patient.refills': [2],
};

function render(path: string, value: unknown): unknown {
  if (typeof value !== 'function') {
    return value;
  }
  const args = SAMPLE_ARGS[path];
  if (!args) {
    throw new Error(`No sample arguments for ${path}`);
  }
  return (value as (...args: unknown[]) => unknown)(...args);
}

describe('Arabic and English dictionaries (spec 005 FR-017, SC-004)', () => {
  const english = new Map(entries(en).map((e) => [e.path, e.value]));
  const arabic = new Map(entries(ar).map((e) => [e.path, e.value]));

  it('have exactly the same keys', () => {
    expect([...arabic.keys()].sort()).toEqual([...english.keys()].sort());
  });

  it('have no empty Arabic text and no Arabic value copied from English', () => {
    for (const [path, value] of arabic) {
      const text = render(path, value);
      const englishText = render(path, english.get(path));

      if (Array.isArray(text)) {
        expect(text.length).withContext(path).toBe((englishText as unknown[]).length);
        text.forEach((item, i) => expect(item).withContext(`${path}[${i}]`).not.toBe((englishText as unknown[])[i]));
        continue;
      }

      expect(typeof text).withContext(path).toBe('string');
      expect((text as string).trim().length).withContext(path).toBeGreaterThan(0);
      if (!SAME_IN_BOTH.has(text as string)) {
        expect(text).withContext(path).not.toBe(englishText);
      }
    }
  });

  it('use Western digits in Arabic counts (spec 005 FR-016)', () => {
    expect(ar.queue.count(12)).toBe('12 طلبًا');
    expect(ar.queue.count(1)).toBe('طلب واحد');
    expect(ar.queue.count(3)).toBe('3 طلبات');
    expect(ar.pagination.position(2, 10)).toBe('الصفحة 2 من 10');
    expect(ar.patient.refills(5)).toBe('5 إعادات صرف');
    expect(ar.queue.matches(2, 12)).not.toMatch(/[٠-٩۰-۹]/);
  });

  it('never call a request with no issues "safe" (004 FR-015)', () => {
    expect(ar.common.noIssuesFound).toBe('لم يُعثر على مشكلات');
    expect(JSON.stringify(entries(ar).map((e) => render(e.path, e.value)))).not.toContain('آمن');
  });
});
