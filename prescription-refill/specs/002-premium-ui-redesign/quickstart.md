# Quickstart: Validate the Premium Clinical UI Redesign

**Feature**: [spec.md](spec.md) | **Date**: 2026-10-20

## Prerequisites

As in the repository [README](../../README.md): .NET SDK, SQL Server (local), Node 22 / npm 10,
Edge or Chrome for headless tests.

## Baseline (2026-10-20T18:29:53+03:00, before any UI change)

- `npm run test:ci`: 56/56 passed. `npm run build`: initial 350.94 kB (93.25 kB transfer).
- Before screenshots: `screenshots/before/` (exact 360px and 1280px via the DevTools-protocol
  script, research R14; no horizontal overflow).

## 1. Automated checks

```powershell
cd frontend
npm ci
npm run test:ci        # every existing spec must pass unchanged; new specs added
npm run build          # production build within existing budgets
cd ..
dotnet test            # backend untouched; must still pass (report "not run" if SQL Server unavailable)
```

Scope check: only `frontend/src/**` (presentation files), `specs/002-premium-ui-redesign/**`,
and docs (`SPEC.md`, `PROMPTS.md`, `README.md` screenshots/notes) may change. No change under
`src/` (backend), `tests/` (backend), or `frontend/src/app/**/data/`, `core/realtime/`.

## 2. Run the app

Start the API and `npm start` as in the README; open http://localhost:4200.

## 3. Visual and interaction audit (360px and 1280px)

Use the browser device toolbar at 360×800 and 1280×800. For each item record pass/fail.

**Queue**

- [ ] Header shows title, description, counts (Pending), search, Show filter, Refresh.
- [ ] Counts match the seeded data (blocked = rows with "Approval blocked").
- [ ] Search "a" narrows rows; a nonsense search shows the "no matches" empty state with Clear.
- [ ] Switch to Approved/Denied: decided styling, decided time, no safety badges.
- [ ] Throttle network: skeleton rows on first load; Refresh shows progress, rows stay.
- [ ] Stop the API: retryable error state; Try again works after restart.
- [ ] Whole row is clickable; keyboard Tab focuses each row link with a visible ring.

**Review** (open each seeded scenario from 001 data-model "Seed Scenarios")

- [ ] Blocked: "Approval blocked" banner is the most prominent element; Approve disabled with
      explanation; Deny enabled.
- [ ] Warning only: warning styling and icon differ from blocked; Approve enabled.
- [ ] Clear: compact success confirmation.
- [ ] Blocker + warning: both shown, blocker first.
- [ ] Evaluation time + staleness notice + Refresh visible together.
- [ ] Allergies shown as separate items.
- [ ] Approve/Deny show progress while submitting; success alert, then read-only summary.
- [ ] Empty note → inline error under the field; note kept.
- [ ] Decide in a second tab → first tab shows "decided elsewhere" and read-only summary.
- [ ] At 360px: safety section appears before patient details; no horizontal scroll.
- [ ] At 1280px: decision column stays visible (sticky) while scrolling details.

**Shell and accessibility**

- [ ] Demo notice visible on both screens, neutral (not warning) styling.
- [ ] Stop the hub: "Live updates unavailable — use Refresh" pill; restart: pill disappears.
- [ ] OS reduced motion on: no shimmer or transitions.
- [ ] 200% zoom: content reflows, no loss.
- [ ] Windows high-contrast mode: focus outline still visible.
- [ ] Contrast spot-check of tokens (research R2) with a contrast checker.

## Results (2026-10-20)

All results come from commands actually run against the user's running API (:5080, local SQL
Server) and dev server (:4200).

| Check | Result |
|-------|--------|
| `npm run test:ci` | 89/89 passed (baseline 56/56) |
| `dotnet test PrescriptionRefill.slnx` | Domain 47/47, integration 61/61 (SQL Server) |
| `npm run build` | OK, no warnings; initial 413.32 kB / 104.38 kB transfer (baseline 350.94 / 93.25) |
| Scope check (files newer than baseline) | Only `frontend/src/**` presentation files and `frontend/angular.json`; nothing in `src/`, `tests/`, `core/`, `data/`, `shared/http/` |
| Screenshots `screenshots/after/` | 16 pages at 360 and 1280, plus forced-colors (`-hc`) and reduced-motion (`-rm`) variants; 768/1024 checked as interim; no horizontal overflow anywhere |
| Token contrast | 21 pairs computed; all ≥ 4.5:1 text, ≥ 3:1 UI (strong border 3.16:1, lowest) |

### Refinement pass (from reviewing screenshots)

1. Decided review at 360px overflowed to 459px: `grid-area` on the details column created an
   implicit grid column on narrow screens → areas applied only at ≥1024px.
2. Ghost "Back to requests" button was underlined → `.btn { text-decoration: none }`.
3. Desktop status badge floated far right of the title → moved next to the request number.
4. Allergy chips used the danger (blocking) palette → warning palette, so red means blocked.
5. "Last filled" wrapped in a narrow column → wider third column.
6. Mobile Pending summary stacked as three full-width pills → three compact tiles.
7. "Refill requests" eyebrow duplicated the brand → "Work queue" / "History".
8. Summary counts read as "1approval blocked" (no space) for assistive technology; caught by a
   new test → fixed markup.

### Intentional test and config changes

- `queue/status-filter.component.spec.ts` rewritten for the segmented switcher (author
  decision; research R7): same behaviors asserted (group named "Show", three options in order,
  current selection, change emitted) plus count-only-on-selected.
- `frontend/angular.json`: `anyComponentStyle` warning budget 4 kB → 6 kB (error stays 8 kB);
  the queue page and review panel stylesheets are 5.1 and 4.9 kB.

### Files changed

- Tokens and base: `frontend/src/styles/_variables.scss`, `frontend/src/styles.scss`.
- Shell: `frontend/src/app/app.component.{html,scss,ts}`.
- Shared UI: `shared/ui/icon.component.ts` (new), `pagination.component.ts` (new),
  `alert.component.{ts,scss}`, `status-badge.component.ts`, `empty-state.component.ts`,
  `error-state.component.ts`, `connection-indicator/connection-indicator.component.ts`.
- Queue: `refill-queue.page.{html,scss,ts}`, `status-filter.component.ts`.
- Review: `refill-review.page.{html,scss,ts}` (adds `pendingDecision` signal only),
  `evaluation-panel.component.{html,ts}`, `decision-form.component.{html,scss,ts}`,
  `decided-view.component.ts`, `patient-panel.component.{html,ts}`, `review-panels.scss`.
- New specs: `icon.component.spec.ts`, `pagination.component.spec.ts`,
  `shared-ui-redesign.spec.ts`, `refill-queue.redesign.spec.ts`,
  `refill-review.redesign.spec.ts`.

### Known limitations

- Server-side pagination is not implemented; the API returns the full list per status. Proposed
  separate change: optional `page`/`pageSize` query parameters and a `total` field (research
  R13).
- Approved/Denied tabs show no count until selected (no extra requests for counts).
- The live-update "unavailable"/"reconnecting" pills were verified by component specs, not by a
  live screenshot, because that requires stopping the user's running API (T038 left open).
- Inter is used only if installed; otherwise Segoe UI Variable / system fonts (no web font).
- A manual timing of SC-003/SC-004 with a clinician was not performed.
- 200% zoom was not rendered separately; 1280px at 200% is a ~640px CSS viewport, which falls
  between the 360px and 768px widths that were checked without overflow.
- Keyboard focus order was checked by reading the DOM order and the `:focus-visible` rules, not
  by a recorded keyboard walkthrough; a manual Tab pass is still recommended.
