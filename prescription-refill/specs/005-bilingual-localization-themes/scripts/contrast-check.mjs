// WCAG 2.1 contrast check for both theme token sets (spec 005 SC-006, tasks T071).
// Usage: node specs/005-bilingual-localization-themes/scripts/contrast-check.mjs [path/to/_themes.scss]
// No packages. Reads the `tokens:<theme>:start/end` blocks of frontend/src/styles/_themes.scss,
// where each color is one `--token: #rrggbb;` line, and fails on any pair below its minimum or on
// a token missing from either set.

import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const file = process.argv[2] ?? resolve(here, '../../../frontend/src/styles/_themes.scss');
const source = readFileSync(file, 'utf8');

const TEXT = 4.5;
const NON_TEXT = 3;

// [foreground, background, minimum, what]. Text pairs need 4.5:1; icons, input borders, and focus 3:1.
const PAIRS = [
  ['color-text', 'color-bg', TEXT, 'body text on the sheet'],
  ['color-text', 'color-surface', TEXT, 'body text on cards'],
  ['color-text', 'color-surface-subtle', TEXT, 'text on subtle panels'],
  ['color-text', 'color-surface-sunken', TEXT, 'text in sunken controls'],
  ['color-text-muted', 'color-surface', TEXT, 'secondary text on cards'],
  ['color-text-muted', 'color-surface-subtle', TEXT, 'secondary text on subtle panels'],
  ['color-text-muted', 'color-surface-sunken', TEXT, 'status filter labels'],
  ['color-text-muted', 'color-bg', TEXT, 'secondary text on the sheet'],
  ['color-text-subtle', 'color-surface', TEXT, 'hints on cards'],
  ['color-text-subtle', 'color-bg', TEXT, 'hints on the sheet'],
  ['color-accent', 'color-surface', TEXT, 'links and accent text on cards'],
  ['color-accent', 'color-bg', TEXT, 'links on the sheet'],
  ['color-accent', 'color-accent-bg', TEXT, 'accent badge text'],
  ['color-accent-hover', 'color-surface', TEXT, 'link hover'],
  ['color-on-accent', 'color-accent', TEXT, 'primary button text'],
  ['color-on-accent', 'color-accent-hover', TEXT, 'primary button hover'],
  ['color-accent-2', 'color-surface', TEXT, 'selected status filter'],
  ['color-danger', 'color-surface', TEXT, 'deny button text'],
  ['color-danger', 'color-danger-bg', TEXT, 'danger alert and badge'],
  ['color-danger-hover', 'color-danger-bg', TEXT, 'deny button hover'],
  ['color-warning', 'color-warning-bg', TEXT, 'warning alert and badge'],
  ['color-success', 'color-success-bg', TEXT, 'success alert and badge'],
  ['color-info', 'color-info-bg', TEXT, 'info alert and badge'],
  ['color-text', 'color-danger-bg', TEXT, 'body text in a danger alert'],
  ['color-text', 'color-warning-bg', TEXT, 'body text in a warning alert'],
  ['color-text', 'color-success-bg', TEXT, 'body text in a success alert'],
  ['color-text', 'color-info-bg', TEXT, 'body text in an info alert'],
  ['color-border-strong', 'color-surface', NON_TEXT, 'input and control borders'],
  // Alert edges carry the state color; *-border tokens are decorative (004 _variables.scss), since
  // meaning is always in the icon and label too, so they are not checked.
  ['color-danger', 'color-surface', NON_TEXT, 'danger alert edge'],
  ['color-warning', 'color-surface', NON_TEXT, 'warning alert edge'],
  ['color-success', 'color-surface', NON_TEXT, 'success alert edge'],
  ['color-info', 'color-surface', NON_TEXT, 'info alert edge'],
  ['focus-color', 'color-surface', NON_TEXT, 'focus outline on cards'],
  ['focus-color', 'color-bg', NON_TEXT, 'focus outline on the sheet'],
  ['select-arrow', 'color-surface', NON_TEXT, 'select arrow'],
];

function block(theme) {
  const match = source.match(new RegExp(`/\\* tokens:${theme}:start \\*/([\\s\\S]*?)/\\* tokens:${theme}:end \\*/`));
  if (!match) {
    throw new Error(`${file}: no tokens:${theme}:start/end block`);
  }
  const tokens = new Map();
  for (const line of match[1].split('\n').map((l) => l.trim()).filter(Boolean)) {
    const token = line.match(/^--([a-z0-9-]+):\s*(#[0-9a-fA-F]{6});$/);
    if (!token) {
      throw new Error(`${file} (${theme}): expected "--token: #rrggbb;" but found "${line}"`);
    }
    tokens.set(token[1], token[2].toLowerCase());
  }
  return tokens;
}

function luminance(hex) {
  const [r, g, b] = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255);
  const linear = (c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
  return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b);
}

function ratio(a, b) {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
}

let failures = 0;
for (const theme of ['light', 'dark']) {
  const tokens = block(theme);
  console.log(`\n${theme.toUpperCase()}`);
  for (const [fg, bg, minimum, what] of PAIRS) {
    if (!tokens.has(fg) || !tokens.has(bg)) {
      console.log(`  FAIL  missing token ${tokens.has(fg) ? bg : fg} (${what})`);
      failures++;
      continue;
    }
    const value = ratio(tokens.get(fg), tokens.get(bg));
    const ok = value >= minimum;
    if (!ok) {
      failures++;
    }
    console.log(`  ${ok ? 'pass' : 'FAIL'}  ${value.toFixed(2).padStart(5)}:1  ≥ ${minimum}  ${fg} on ${bg}  (${what})`);
  }
}

console.log(failures === 0 ? '\nAll pairs meet WCAG 2.1 AA.' : `\n${failures} pair(s) below WCAG 2.1 AA.`);
process.exit(failures === 0 ? 0 : 1);
