# Research: Creative Clinical UI ("Ambient Atelier")

Sources: the current frontend (`styles/_variables.scss`, `styles.scss`, `app.component.*`, queue and
review components, shared UI), the 002 UI contract (`specs/002-premium-ui-redesign/contracts/ui-contract.md`),
and calculations recorded below. No open questions remain; spec FR-012 uses the recommended default
(selected status count only).

## R1 Composition: dark canvas, light sheet

- **Decision**: The page background becomes a midnight-navy canvas that carries the ambient light.
  The header (brand, live-update indicator) and the fictional-data notice sit on the canvas in light
  text. All routed content renders on a single light "sheet" (Clinical Mist) with large rounded top
  corners and a soft shadow, floating inside the canvas; cards on the sheet stay opaque white. At
  ≤ 480 px the sheet spans the full width with a smaller radius, so gutters don't waste space.
- **Rationale**: Gives the brief's "dark navigation shell + light content" an editorial composition
  without inventing navigation: the app has two routes, so a sidebar would hold one fake item. The
  ambient light is most visible on the dark canvas and stays faint on the sheet, so clinical text is
  never on a translucent or gradient surface (spec FR-007).
- **Alternatives**: Left navigation rail (one real destination; rejected as fake chrome). A dark
  masthead band behind page titles (titles wrap past the band on narrow screens, creating mixed
  contrast; rejected). Full dark theme (out of scope, spec Assumptions).

## R2 Palette and contrast (calculated, WCAG 2.1 relative luminance)

| Color | On white | On mist `#F4F7FC` | On navy `#0B172A` | Role decided |
|-------|---------:|------------------:|------------------:|--------------|
| Midnight navy `#0B172A` | 17.95 | 16.71 | — | canvas, header; primary text color stays slate `#0F172A` |
| Clinical mist `#F4F7FC` | — | — | 16.71 | sheet background; text on canvas |
| Clinical teal `#0F8989` | 4.23 | 3.94 | 4.24 | **decorative and non-text only** (glow, brand mark, focus halo); fails 4.5 for text |
| Teal text `#0F766E` (existing) | 5.47 | 5.10 | — | links, primary buttons, focus outline (kept) |
| Creative indigo `#6366F1` | 4.47 | — | 4.02 | **decorative only** (ambient light, accents) |
| Indigo text `#4F46E5` (new) | 6.29 | — | — | secondary accent text (eyebrows, counts) |
| Teal on canvas `#5EEAD4` | — | — | 12.13 | accent text/icons on the canvas |
| Indigo on canvas `#A5B4FC` | — | — | 9.00 | accent on the canvas |
| Slate `#CBD5E1` / `#94A3B8` | — | — | 12.09 / 7.00 | body / secondary text on the canvas |
| Success `#22C55E` | 2.28 | — | — | decorative tint only; status text keeps `#067647` (5.4) |
| Warning `#F59E0B` | 2.15 | — | — | decorative tint only; status text keeps `#93370D` (7.2) |
| Danger `#EF4444` | 3.76 | — | — | icon/border accent only (≥ 3:1); text keeps `#B42318` (6.0) |

- **Decision**: Keep every 002 text token that already meets AA; add canvas tokens and the brief's
  hues as decorative tokens. Status meaning is always label + icon + color (spec FR-014).
- **Rationale**: Spec FR-005/FR-006 require validation over assumption; five of the seven brief
  colors fail as small text on light surfaces.

## R3 Token architecture

- **Decision**: Stay with the project convention: SCSS variables in `styles/_variables.scss`,
  renamed only where meaning changes, plus a new canvas group (`$canvas-*`) and decorative group
  (`$glow-*`). Runtime values that change with the pointer are CSS custom properties
  (`--pointer-x`, `--pointer-y` on the shell; `--glow-x`, `--glow-y` on a glowing element).
- **Rationale**: Every component already imports `variables`; switching all styles to CSS custom
  properties would touch ~25 files for no user-visible gain (constitution I: proportional).
- **Alternatives**: Full CSS-custom-property theme (only useful for runtime theming, which is out of
  scope).

## R4 Ambient light mechanism

- **Decision**: One standalone attribute directive, `appAmbientLight`, placed once on the app shell
  host. It:
  1. Enables itself only when `matchMedia('(hover: hover) and (pointer: fine)')` matches and
     `(prefers-reduced-motion: reduce)` does not; it listens to both media queries and switches
     on/off live. When enabled it sets `data-ambient="on"` on the host; CSS shows the pointer-driven
     light only under that attribute, otherwise a fixed static composition.
  2. Registers **one** passive `pointermove` and one `pointerleave` listener on the host, outside
     Angular's zone (`NgZone.runOutsideAngular`; the app uses zone.js), so pointer movement never
     triggers change detection.
  3. Stores the last event and schedules at most one `requestAnimationFrame` per movement burst.
     The frame writes two coordinate sets, each in the space of the layer that uses it:
     - `--pointer-x/--pointer-y` on the host = the event's **viewport** coordinates
       (`clientX`, `clientY`), because the canvas light layer is `position: fixed` to the viewport.
       This keeps the light under the pointer at any scroll position; page or host offsets are never
       mixed in.
     - `--glow-x/--glow-y` on the element under the pointer, found by event delegation
       (`target.closest('[data-glow]')`) = coordinates **relative to that element**
       (`clientX − rect.left`, `clientY − rect.top`, with `rect` from `getBoundingClientRect()` read
       in the same frame), because the glow is drawn inside the element.
     No frame is scheduled without movement, so nothing runs while the pointer is still (spec
     FR-020). Scrolling with a still pointer does not move the light relative to the viewport, which
     is correct for a fixed layer; the row glow is recomputed on the next pointer move.
  4. Removes listeners, media listeners, and any pending frame via `DestroyRef`.
- **Elements that glow** opt in with a `data-glow` attribute: queue rows only (the only
  interactive cards; review panels are not clickable, spec FR-018).
- **Rendering**: background light = a decorative `.ambient-layer` element, `position: fixed;
  inset: 0`, with two `radial-gradient`s (teal, indigo; sizes `$ambient-light-size` and
  `$ambient-light-size-secondary`, low alpha) centered at the viewport coordinates,
  `pointer-events: none`, behind content; position eases with a 300 ms transition so it drifts
  rather than snaps. Row glow = `::after` radial gradient of radius `$glow-radius` at
  `--glow-x/--glow-y`, `pointer-events: none`, opacity 0 → 1 on hover (240 ms). Both hidden in
  `forced-colors: active` and when printing. Sizes are tokens (R3), never raw values.
- **Rationale**: One listener for the whole app instead of per-card handlers; no continuous loop;
  no dependency (spec FR-019–FR-023).
- **Alternatives**: `@HostListener` on every row (N listeners, runs change detection; rejected);
  CSS-only `:hover` gradients without pointer position (no "following" light); a canvas/WebGL layer
  (heavy, continuous rendering; rejected).

## R5 Safety-state presentation

- **Decision**: Keep the evaluation panel's structure and the hooks tests rely on (`.alert-danger`
  "Approval blocked", `.alert-warning` "do not block approval", `.alert-success` "No blocking rules
  apply"), and change presentation:
  - Blocking: danger alert with an "Approval blocked" heading, octagon icon, a solid left rule, and
    "Prevents approval" label.
  - Warnings: heading becomes "Warnings — clinical review required. They do not block approval."
    (still contains the tested phrase), triangle icon, amber rule; never collapsible or muted.
  - No issues: the safety panel styles its `.alert-success` as a **neutral, factual** state (mist
    surface, slate text, check icon, no bright green); wording adds "Review the details before
    deciding." so it never reads as "approval is safe" (spec FR-015).
  - The panel header badge shows the same label as text.
- **Rationale**: Meets FR-014/FR-015 without breaking the 002 contract; the green `success` kind
  stays for genuine success notices ("Request approved.").
- **Alternatives**: A new `clear` alert kind (changes a tested class; no benefit over scoped
  styling).

## R6 Review layout and mobile

- **Decision**: Keep the DOM order (safety → decision → details). Wide (≥ 1024 px): two columns,
  decision column sticky below the header, as in 002. Narrow (< 1024 px): single column, **no sticky
  elements** (the 002 header is sticky; on narrow screens it becomes static so it cannot cover the
  note field or warnings, spec FR-016).
- **Rationale**: Safety-first reading order already tested; removing stickiness on small screens
  addresses the brief's mobile rule.

## R7 Queue density

- **Decision**: Row height from ~72 px to `$row-min-height` (56 px) at desktop (padding `$space-3`, one-line
  patient + medication, time and badges right-aligned in fixed columns); stacked two-line layout
  below 768 px. Hover: faint mist tint + glow (pointer devices). Focus: 2 px teal outline around the
  whole row via `:focus-within` on the stretched link, plus a 3 px teal left bar, so focus is
  obvious without color-only reliance (outline shape).
- **Rationale**: SC-007 (at least as many rows per 1280 × 800 view); visible keyboard focus
  (FR-010).

## R8 Motion system

- **Decision**: `$duration-fast: 180ms`, `$duration-base: 240ms`, `$duration-slow: 300ms`,
  `$easing-standard: cubic-bezier(0.2, 0, 0, 1)`, `$easing-emphasized: cubic-bezier(0.3, 0, 0, 1)`.
  Hover lift on rows/buttons: `translateY(-1px)` max. One entrance animation: the sheet fades/rises
  4 px on first paint (300 ms); no per-row animation. The existing global reduced-motion block
  removes transitions and the entrance; the light stops following input.
- **Rationale**: Spec FR-024–FR-026.

## R9 Typography

- **Decision**: Keep the Inter-first system stack (no web font, 002 security decision). Scale:
  page title 1.5 rem/600 (was 1.75 rem), section heading 1.0625 rem/600, card title 0.9375 rem/600,
  body 0.9375 rem, metadata 0.8125 rem, eyebrow 0.75 rem caps in indigo text `#4F46E5`.
- **Rationale**: Smaller title gains vertical space for clinical data (FR-008, SC-007) while hierarchy
  stays clear through weight, color, and spacing.

## R10 Visual verification tooling

- **Decision**: Reuse the 002 approach: the locally installed Edge, headless, driven over the
  DevTools protocol by a script kept outside the repository (Node 22's built-in `WebSocket`, no
  dependency). It sets exact viewport widths (360, 767, 768, 1280) and 200% zoom, emulates
  `prefers-reduced-motion` and `hover: none`, dispatches real `mouseMoved` events to move the
  pointer, reads back `--pointer-x/--pointer-y` and `--glow-x/--glow-y`, reports horizontal
  overflow, and saves screenshots. Runs against the real API with a throwaway seeded database on a
  free port (not the developer's running instance; lesson from 003 T038).
- **Rationale**: Spec SC-004–SC-006, SC-008 require real-browser evidence; this proved reliable in
  002.

## R11 Test impact

- **Decision**: No existing assertion is removed or weakened. Expected changes: none to test files
  for hooks and text (all kept). New specs: `ambient-light.directive.spec.ts` (enabled/disabled by
  media queries, one listener, custom properties written once per frame, no frame without movement,
  cleanup on destroy, `data-glow` delegation); review spec additions for the neutral no-issues
  wording and the clinical-review warning heading; app shell spec for the directive's presence.
- **Rationale**: Spec SC-001.
