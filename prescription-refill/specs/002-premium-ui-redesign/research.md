# Research: Premium Clinical UI Redesign

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-20

Each entry: Decision / Rationale / Alternatives considered. R0 is the audit of the current UI
(inspected 2026-10-20; no source changed).

## R0 UI audit: why the current UI looks basic

Ranked by impact on clinical clarity, then on perceived quality.

| # | Problem | Location | Recommended improvement | UX benefit |
|---|---------|----------|------------------------|------------|
| 1 | On narrow screens the patient/prescription details come **before** the safety checks and decision; safety signals sit below the fold at 360px. Wide layout is two equal columns with details first. | `review/refill-review.page.html` (`.layout`: patient panel first), `refill-review.page.scss` | Reorder: safety + decision first in DOM on mobile; at ≥1024px a 7/5 grid with a sticky decision column on the right. | Blockers seen before anything else; fewer missed safety signals. |
| 2 | Blockers, warnings, and success alerts differ only by color and a 4px left border; no icons. Warning heading is a parenthetical sentence. The info alert has no text color. | `shared/ui/alert.component.ts/.scss`, `review/evaluation-panel.component.html` | Alerts get a per-kind icon, a bold title row, and a tinted surface; blockers get a stronger "Approval blocked" banner with a count; warnings get the heading "Warnings — do not block approval". | Meaning no longer relies on color alone (WCAG 1.4.1); blockers vs warnings are unmistakable. |
| 3 | The staleness notice and evaluation time sit **above** the results, so the first line of "Safety checks" is muted boilerplate. | `evaluation-panel.component.html` | Move results first; put "Evaluated … · may be out of date …" and Refresh in a footer row of the panel. | The first thing read is the outcome. |
| 4 | Design tokens are minimal: one radius, no shadows, no motion, one font weight, a generic `#1d4ed8` link blue, heavy `#cbd2d9` borders, `h1` only 22px. | `src/styles/_variables.scss`, `src/styles.scss` | Full token set (R2): slate neutrals, teal accent, 3 radii, 2 shadows, type scale to 28px, weights 400/500/600/700, motion tokens. | Coherent, premium feel; clear hierarchy. |
| 5 | The amber demo banner uses the **warning** palette on every screen, diluting the meaning of clinical warnings. | `app.component.html/.scss` (`.demo-notice`) | Keep the same text and `role="note"`; restyle as a neutral/info strip with an info icon. | Warning color reserved for clinical warnings. |
| 6 | Queue rows: only the name/medication block is the link; the time and badges are outside it. No chevron or hover elevation; rows look like form fields. | `queue/refill-queue.page.html/.scss` | Whole row clickable (stretched-link pattern, one `<a>` per row); hover/focus elevation; chevron; aligned grid columns: patient+medication · time · badges. | Larger target, faster scanning, clear affordance. |
| 7 | No queue context: no description, no counts, no search. | `queue/refill-queue.page.html` | Page header with description; summary chips "N pending · N blocked · N with warnings" from the server list; client-side search. | Workload visible at a glance; quick finding. |
| 8 | Loading states are a single muted text line. | `refill-queue.page.html`, `refill-review.page.html` | Skeleton rows/panels with visually hidden "Loading …" status text (keeps tests). | Less layout jump; perceived speed. |
| 9 | Empty state is a dashed box with one sentence; no distinction between "no requests" and "no matches". | `shared/ui/empty-state.component.ts` | Icon + title + description + optional action slot (e.g. "Clear search"). | Explains what to do next. |
| 10 | Allergies render as one comma-joined string; other active prescriptions are long inline sentences. | `review/patient-panel.component.html` | Allergy chips (danger-outline, with icon); compact table-like list of active prescriptions. | Allergies are scannable; less reading. |
| 11 | Submitting shows only disabled buttons; no progress on the pressed action; no character guidance toward 1,000. | `review/decision-form.component.html` | Spinner + "Approving…/Denying…" on the pressed button; `n / 1000` counter (display only, not validation). | Clear feedback; fewer server validation round-trips. |
| 12 | Focus uses `outline: none` + `box-shadow`, which disappears in Windows forced-colors (high contrast) mode. | `src/styles.scss` (`:focus-visible`) | `outline: 2px solid` accent + `outline-offset: 2px`. | Focus always visible. |
| 13 | Decided view is a plain facts list; the transition from pending to read-only is abrupt and visually similar to the pending layout. | `review/decided-view.component.ts` | Outcome banner (approved/denied icon + color), note in a quoted block, decision time; "Read-only" label. | Final state is obvious. |
| 14 | Connection indicator is plain muted text. | `shared/ui/connection-indicator/connection-indicator.component.ts` | Pill with icon: amber "Reconnecting…", red-outline "Live updates unavailable — use Refresh". Still empty while connected (tested). | Visible, calm signal. |
| 15 | Header is a bare text link; no product mark. | `app.component.html/.scss` | Product mark (inline SVG) + name, max-width container, subtle bottom border and shadow. | Product identity. |

**What is already good and is kept**: native labeled controls, `role="alert"/"status"` usage,
`aria-live` notice region, `aria-describedby` wiring on the note and disabled Approve, OnPush
signals, HTTP only in `RefillRequestsApi`, SignalR only in `RefillEventsService`, shared time
formatting (`app-instant`).

## R1 Component library

- **Decision**: No component library (keeps 001 R10).
- **Rationale**: Two screens; existing shared components cover the needs; a library would add
  bundle weight, a theming layer to override, and would change DOM/selectors that tests rely on.
- **Alternatives**: Angular Material 19 (compatible but heavy theming, changes button/select
  DOM, conflicts with test selectors); PrimeNG (same, plus visual identity to override);
  Tailwind (extra build config, utility classes conflict with the SCSS-token rule).

## R2 Design tokens

- **Decision**: Extend `src/styles/_variables.scss` (SCSS variables, same `@use 'variables' as *`
  pattern) with:
  - Neutrals (slate): bg `#f5f7fa`, surface `#ffffff`, subtle surface `#f8fafc`, border
    `#e2e8f0`, strong border (inputs) `#94a3b8` (≥3:1 on white), text `#0f172a`, secondary text
    `#475569` (7.5:1), tertiary `#64748b` (4.8:1).
  - Accent (teal): `#0f766e` (5.5:1 on white), hover `#115e59`, tint `#f0fdfa`.
  - Semantic: danger `#b42318` / bg `#fef3f2` / border `#fecdca`; warning `#93370d` / bg
    `#fffaeb` / border `#fedf89`; success `#067647` / bg `#ecfdf3` / border `#abefc6`; info
    `#175cd3` / bg `#eff8ff` / border `#b2ddff`. All text colors ≥4.5:1 on their backgrounds.
  - Type scale 12/14/16/18/22/28px; weights 400/500/600/700; tabular numerals for times/counts.
  - Spacing: keep `$space-1…6`, add `$space-7: 3rem`, `$space-8: 4rem`.
  - Radii: sm 6px (inputs, badges), md 10px (cards, alerts), pill 999px.
  - Shadows: `xs` for cards, `md` for hover elevation; no heavy drop shadows.
  - Motion: 120ms / 200ms, `cubic-bezier(0.2, 0, 0, 1)`; global
    `prefers-reduced-motion: reduce` sets durations to 0.
  - Breakpoints: sm 480px, md 768px, lg 1024px; content max-width 1200px.
- **Rationale**: Smallest change to the existing pattern; every existing variable name kept
  (values updated) so untouched files keep compiling.
- **Alternatives**: CSS custom properties (useful only for runtime theming; light-only scope
  makes it unnecessary); a token JSON pipeline (overkill).

## R3 Icons

- **Decision**: One `app-icon` component rendering ~14 inline SVG paths (24×24, 1.75 stroke,
  `currentColor`), adapted from Lucide (ISC licence, attribution in the component file).
  `aria-hidden="true"` by default.
- **Rationale**: Consistent set, no dependency, no network request, tree-shaken as code.
- **Alternatives**: `lucide-angular` package (new dependency for ~14 icons); icon font
  (accessibility and flash-of-missing-icon issues).

## R4 Fonts

- **Decision**: Keep the system font stack, ordered `Inter, 'Segoe UI Variable', 'Segoe UI',
  system-ui, …` (Inter used only if installed); enable `font-feature-settings: 'tnum'` on
  times and counts.
- **Rationale**: No external request, no new asset; Segoe UI Variable on Windows and SF on macOS
  look premium.
- **Alternatives**: Self-hosted Inter (~100 kB per weight; deferred, can be added later
  without changing tokens); Google Fonts (external request, privacy).

## R5 Queue search

- **Decision**: A `query` signal on `RefillQueuePage`; `visibleItems = computed(...)` filters
  `list().items` by case-insensitive substring on `patientName` and `medicationName`. Kept
  across reloads and status changes (same component instance). Search input is a native
  `<input type="search">` with a visible label.
- **Rationale**: The list is unpaginated (001 Assumptions); the API has no search parameter;
  filtering display text is not rule evaluation (constitution II).
- **Alternatives**: Server search (contract change, out of scope); URL `q` parameter (nice to
  have; deferred to keep the router contract unchanged).

## R6 Queue counts

- **Decision**: For the Pending view: total = `items.length`; blocked = items with
  `canApprove === false`; warnings = items with `hasWarnings === true`. Counts reflect the full
  loaded list (not the search result); the search shows its own "n of N" result line.
- **Rationale**: Counts come straight from server flags (spec FR-003); no invented metrics.
- **Alternatives**: Average wait time / SLA indicators (fabricated, rejected).

## R7 Status filter control (revised 2026-10-20, author decision)

- **Decision**: A segmented switcher built from native radio inputs in a `fieldset` whose
  `legend` is "Show" (visually hidden), each radio wrapped in a styled `label` segment. The
  component keeps its API (`value` input, `changed` output) and gains an optional `count`
  input rendered only on the selected segment. The page keeps navigating via the `status`
  query parameter, so filter semantics are unchanged.
- **Rationale**: Native radios give one tab stop, arrow-key movement, and checked state to
  assistive technology with no custom keyboard code; it is the least complex accessible
  segmented control. Counts are shown only where exact (R6).
- **Test change (intentional)**: `status-filter.component.spec.ts` asserted a native
  `<select>`. It is rewritten to assert the same behavior on the new control: accessible group
  name "Show", option values `Pending, Approved, Denied` in order, `Pending` checked, choosing
  `Denied` emits `Denied`; plus new assertions that the count shows only on the selected
  option. Coverage is equal or stronger.
- **Alternatives**: `role="tablist"` (tabs imply tab panels and need roving-tabindex code);
  links with `aria-current` (three tab stops; acceptable but noisier for keyboard users);
  keeping the `<select>` (less scannable).

## R8 Whole-row link

- **Decision**: Keep one `<a>` per row wrapping patient + medication (accessible name stays
  short); extend its click area to the whole row with a `::after` overlay (stretched link);
  badges and time sit visually in the row but outside the link text.
- **Rationale**: Large target without a verbose link name; tests read `li.queue-item a[href]`.
- **Alternatives**: Wrap the whole row in `<a>` (long accessible names including badges);
  click handler on `<li>` (not keyboard-accessible).

## R9 Loading skeletons

- **Decision**: A `.skeleton` global utility (shimmer disabled under reduced motion) and
  skeleton markup in each page's loading case, each wrapped in an element with
  `role="status"` containing visually hidden "Loading requests…" / "Loading request…" text.
- **Rationale**: Tests assert `textContent` contains "Loading"; screen readers still hear it.
- **Alternatives**: Separate skeleton component per screen (more files for little gain).

## R10 Review layout

- **Decision**: DOM order: page header → notice region → `decision-column` (safety, decision or
  decided summary) → `details-column` (patient, prescription, other active prescriptions). At
  ≥1024px a two-column grid places details left (7fr) and the decision column right (5fr) via
  `grid-template-areas`, with the decision column `position: sticky; top` so actions stay
  visible while reading details.
- **Rationale**: Safety-first on mobile and screen readers; conventional reading layout on
  desktop (spec FR-018).
- **Alternatives**: Single column at all widths (wastes desktop space); tabs (hides details).

## R11 Connected-state indicator

- **Decision**: Nothing rendered while connected (existing test); styled pills for
  reconnecting and unavailable.
- **Rationale**: Preserves tested behavior; the indicator only interrupts when needed.
- **Alternatives**: Always-visible "Live" dot (would change a tested behavior).

## R13 Pagination

- **Decision**: Client-side pagination of the queue in `RefillQueuePage`: `page` and
  `pageSize` signals; `pagedItems = filtered.slice((page-1)*size, page*size)`; `pageCount =
  max(1, ceil(filtered.length / size))`; effective page clamped to `pageCount` so reloads that
  shrink the list never show an empty page. Reset to 1 on search, status, or page-size change.
  A shared presentational `app-pagination` component (inputs `page`, `pageSize`, `total`,
  `pageSizes`; outputs `pageChange`, `pageSizeChange`) renders "Showing x–y of N", "Page p of
  P", Previous/Next buttons, and a labeled page-size `<select>`, inside
  `<nav aria-label="Pagination">`. After a page change, focus moves to the results heading.
- **Rationale**: The list endpoint returns the complete collection ("Not paginated" in the 001
  contract), so client-side totals are exact. No contract change.
- **Limitation (documented)**: Collections can grow (API creation; decided requests
  accumulate), so the full list is transferred on every reload. Proposed separate API change:
  optional `page`/`pageSize` query parameters and a `total` field on `RefillRequestList`, with
  the same ordering. Not implemented here.
- **Alternatives**: Infinite scroll (no stable position, worse for keyboard users); server
  paging now (contract change, out of scope); page numbers list (unnecessary at this scale).
- **Superseded 2026-10-20**: server-side paging delivered by feature 003
  ([spec](../003-backend-pagination/spec.md)); the queue no longer slices or filters locally.

## R14 Visual verification

- **Decision**: Screenshots via a small Node script that drives headless Edge through the
  DevTools protocol (`Emulation.setDeviceMetricsOverride` for exact 360px/1280px,
  `Emulation.setEmulatedMedia` for reduced motion and forced colors), capturing full pages
  and reporting `scrollWidth > innerWidth` as horizontal overflow. The script lives outside the
  repo (no project dependency).
- **Rationale**: Headless Edge `--window-size` cannot go below ~500px wide, so `--screenshot`
  gives false 360px results (observed: viewport 496px).
- **Alternatives**: Puppeteer/Playwright (new dependency); manual DevTools (not repeatable).

## R12 Testing approach

- **Decision**: Keep every existing spec unchanged except `status-filter.component.spec.ts`
  (intentional behavior change, R7). Add specs for: icon rendering (`aria-hidden`), queue
  search and counts, pagination (component and page integration, SC-008 sizes), empty-state
  action slot, skeleton status text, decision-form progress label and counter, review DOM order
  (safety before details). Visual audit at 360px and 1280px with screenshots (R14).
- **Rationale**: Constitution V; spec FR-005.
- **Alternatives**: Screenshot regression tooling (new dependency; deferred).
