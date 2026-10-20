# Data Model: Creative Clinical UI

**No application data changes.** No API, DTO, entity, or client model changes; the screens display
the same request list, detail, evaluation, patient, prescription, and decision data as before
(spec Key Entities). This document records the two presentation models the redesign introduces.

## Design tokens (`frontend/src/styles/_variables.scss`)

Contrast values: [research.md R2](research.md#r2-palette-and-contrast-calculated-wcag-21-relative-luminance).

| Group | Token(s) | Value | Use and constraint |
|-------|----------|-------|--------------------|
| Canvas | `$canvas-bg` | `#0B172A` | page background behind the sheet; never behind clinical data |
| | `$canvas-text` / `$canvas-text-muted` | `#F4F7FC` / `#CBD5E1` | header, notice text on the canvas (16.7 / 12.1:1) |
| | `$canvas-accent` | `#5EEAD4` | icons and accent text on the canvas (12.1:1) |
| | `$canvas-border` | `rgba(203, 213, 225, 0.16)` | decorative dividers on the canvas |
| Sheet | `$color-bg` | `#F4F7FC` (was `#F4F6F9`) | routed content background |
| | `$color-surface` | `#FFFFFF` | cards: opaque, all clinical data |
| Text | `$color-text`, `-muted`, `-subtle` | unchanged | ≥ 5.0:1 on sheet and cards |
| Accent | `$color-accent` | `#0F766E` (unchanged) | links, primary button, focus outline |
| | `$color-accent-2` | `#4F46E5` (new) | eyebrows, secondary emphasis text (6.3:1) |
| Decorative | `$glow-teal`, `$glow-indigo` | `#0F8989`, `#6366F1` | light layers, brand mark, glows only (fail AA as text) |
| Status | danger / warning / success / info groups | unchanged text colors | text ≥ 4.5:1; brief's bright hues only as tints |
| Motion | `$duration-fast/base/slow` | 180 / 240 / 300 ms | all interactive transitions |
| | `$easing-standard`, `$easing-emphasized` | see research R8 | |
| Shape | `$radius-sheet` | `24px` (16px ≤ 480px) | sheet top corners |
| Sizing | `$target-min` | `44px` | minimum interactive target on narrow screens (spec FR-028) |
| | `$row-min-height` | `56px` | queue row height at ≥ 768 px (research R7) |
| Ambient | `$ambient-light-size` | `900px` | teal background light diameter (`radial-gradient` size) |
| | `$ambient-light-size-secondary` | `700px` | indigo background light diameter |
| | `$glow-radius` | `180px` | row glow radius |
| Elevation | `$shadow-sheet` | soft, navy-tinted | sheet over canvas |

## Ambient light runtime state

Owned by the `appAmbientLight` directive (research R4); no Angular state, no change detection.

| Item | Where | Values | Set when |
|------|-------|--------|----------|
| `data-ambient` | shell host attribute | `"on"` or absent | pointer is fine + hover-capable and reduced motion is off; updated live on media changes |
| `--pointer-x`, `--pointer-y` | shell host style | px in **viewport** coordinates (`clientX`, `clientY`); used by the `position: fixed` canvas light, so it stays under the pointer at any scroll position | once per animation frame during pointer movement |
| `--glow-x`, `--glow-y` | the `[data-glow]` element under the pointer | px **relative to that element** (`clientX − rect.left`, `clientY − rect.top`) | same frame, only for that element |
| pending frame | directive field | id or null | set on first move of a burst, cleared when the frame runs or on destroy |

States: **static** (no attribute; fixed light positions in CSS) ⇄ **ambient** (attribute present;
light follows pointer). On `pointerleave` the light eases back to its resting position.
