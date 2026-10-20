# Implementation Plan: Premium Clinical UI Redesign

**Branch**: `002-premium-ui-redesign` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-premium-ui-redesign/spec.md`

## Summary

Restyle and restructure the existing Angular frontend into a polished clinical SaaS interface
without changing behavior. The approach: extend the existing SCSS token file (slate neutrals,
teal accent, radii, shadows, motion), add one icon component, upgrade the existing shared
components (alert, badge, empty/error state, connection indicator), then rework the queue
(header with counts, client-side search, whole-row links, skeletons) and the review screen
(safety-first order, distinct blocker/warning treatment, sticky decision column, progress and
read-only states). Every DOM hook and text that existing tests rely on is kept
([contracts/ui-contract.md](contracts/ui-contract.md)). The audit is in
[research.md R0](research.md#r0-ui-audit-why-the-current-ui-looks-basic).

## Technical Context

**Language/Version**: TypeScript 5.6, Angular 19 (standalone components, signals, OnPush), SCSS

**Primary Dependencies**: Existing only (`@angular/*` 19, `@microsoft/signalr`, `rxjs`). No new
runtime or dev dependencies.

**Storage**: N/A (no data changes)

**Testing**: Karma + Jasmine (`npm run test:ci`, headless Edge/Chrome); backend `dotnet test`
run only to confirm no regression

**Target Platform**: Evergreen desktop and mobile browsers; light theme only

**Project Type**: Web application (Angular frontend + ASP.NET Core API); only the frontend
changes

**Performance Goals**: No perceptible change; initial bundle stays within the existing budget
(500 kB warning)

**Constraints**: No API/DTO/SignalR changes; HTTP only in `RefillRequestsApi`; SignalR only in
`RefillEventsService`; every existing test unchanged and passing; WCAG 2.1 AA; 360px and 1280px
without horizontal scrolling; reduced-motion respected

**Scale/Scope**: 2 screens, app shell, ~8 shared components; ~30 frontend files touched

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|-----------|-------|--------|
| I. Pragmatic Clean Architecture | No backend layer touched; only one new abstraction (`app-icon`) with immediate consumers; no component library or speculative services. | PASS |
| II. Deterministic Server-Side Rules | UI shows server evaluation only; counts and badges read server flags (`canApprove`, `hasWarnings`); search filters display text only; Approve availability still from `evaluation.canApprove`. | PASS |
| III. Workflow Integrity | Decision flow, messages, and outcome handling in `RefillReviewPage` unchanged; only markup/styling and a `pending` kind for progress. | PASS |
| IV. Persistence & Concurrency | Not touched. | PASS (N/A) |
| V. Test Evidence | All existing specs kept unchanged; new specs for search, counts, icons, progress, DOM order; unexecuted suites reported as not run. | PASS |
| VI. Security & Data Handling | No new data, logging, or network calls (no web fonts, no CDN icons); demo/no-sign-in notice text unchanged. | PASS |
| VII. Frontend Quality | Feature folders kept; HTTP in services; loading/empty/error/warning/blocked/success states all redesigned; keyboard + labels kept; 360/1280px; all spacing/type/color from shared tokens. | PASS |
| Delivery / Workflow | `SPEC.md`, `README.md`, `PROMPTS.md` updated at the end; increments reviewable. | PASS |

**Post-design re-check (after Phase 1)**: PASS. The one spec conflict found (an always-visible
"Live" indicator vs the tested empty connected state) was resolved in favor of the existing
test (research R11). The segmented status switcher (R7) is an author-approved intentional
change; its one unit test is rewritten with equivalent or stronger assertions. Client-side
pagination (R13) uses the full list the API already returns; no contract change.

## Project Structure

### Documentation (this feature)

```text
specs/002-premium-ui-redesign/
├── plan.md
├── research.md          # R0 audit + decisions R1–R12
├── data-model.md        # client view state only
├── quickstart.md        # automated checks + 360/1280 visual audit
├── contracts/
│   └── ui-contract.md   # DOM hooks/text the tests rely on
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
frontend/src/
├── index.html                         # unchanged (title may gain product name)
├── styles.scss                        # base, buttons, forms, cards, skeleton, focus, motion
├── styles/_variables.scss             # extended tokens (names kept, values updated)
└── app/
    ├── app.component.{html,scss}      # header with mark, neutral demo strip, page container
    ├── shared/ui/
    │   ├── icon.component.ts          # NEW: inline SVG set
    │   ├── alert.component.{ts,scss}  # icon per kind, title row
    │   ├── status-badge.component.ts  # tones + dot
    │   ├── empty-state.component.ts   # heading, icon, action slot
    │   ├── pagination.component.ts    # NEW: range, page x of y, prev/next, page size
    │   ├── error-state.component.ts   # restyle
    │   └── connection-indicator/…     # pill states
    └── features/refill-requests/
        ├── queue/
        │   ├── refill-queue.page.{html,scss,ts}   # header, counts, search, rows, skeleton
        │   └── status-filter.component{,.spec}.ts  # segmented radio switcher (+ test rewrite)
        └── review/
            ├── refill-review.page.{html,scss}      # safety-first order, sticky column, skeleton
            ├── evaluation-panel.component.html     # blockers → warnings → evaluated footer
            ├── decision-form.component.{html,scss,ts}  # progress, counter
            ├── decided-view.component.ts           # outcome banner, read-only summary
            ├── patient-panel.component.html        # allergy chips, compact active list
            └── review-panels.scss
```

Not touched: `frontend/src/app/features/refill-requests/data/**`, `frontend/src/app/core/**`,
`frontend/src/app/shared/http/**`, `frontend/src/app/shared/time/**` (logic), `src/**`
(backend), `tests/**`.

**Structure Decision**: Keep the existing feature-based Angular structure; add one shared
component file. No new folders beyond `shared/ui/icon.component.ts`.

## Implementation increments (revised 2026-10-20 per author)

Each increment ends with `npm run test:ci` green and is reviewable on its own.

1. **Foundation** — tokens (R2), typography, base styles, focus outline, reduced motion,
   buttons/forms/cards/skeleton utilities, `app-icon`, shared alert/badge/empty/error/
   connection components, `app-pagination`, application shell with product mark and a neutral
   demo strip. **Safety exception**: the review screen's DOM order is changed here so that
   safety checks and the decision come before patient details on narrow screens (audit R0 #1 —
   at 360px the blockers are currently below all details).
2. **Queue screen** — page header, segmented status switcher with exact active count (R7),
   Pending summary counts, search, client-side pagination (R13), row grid with stretched link,
   decided styling, skeleton rows, reload progress, two empty states.
3. **Review screen** — 7/5 sticky layout, evaluation panel order and blocker/warning/info/
   success treatment, patient summary with allergy chips, prescription facts, compact active
   prescriptions, decision progress and counter, decided banner, review skeleton.
4. **Responsive, accessibility, high contrast** — 360/768/1024/1280, 200% zoom, forced colors,
   reduced motion, heading outline, focus order, contrast verification.
5. **Regression and visual audit** — after screenshots, at least one refinement pass from
   findings, full tests/build, backend tests, scope check, docs, summary by file.

## Verification commands

```powershell
# Frontend unit/component tests (Karma + Jasmine, headless Edge) — after every increment
cd D:\DHC\prescription-refill\frontend; npm run test:ci

# Production build and bundle budget — after increments 1, 2, 3, 5
cd D:\DHC\prescription-refill\frontend; npm run build

# Backend regression (unchanged code; SQL Server integration tests need local SQL Server)
cd D:\DHC\prescription-refill; dotnet test

# Scope check: no files changed outside presentation/docs (repo is not a git repository,
# so compare timestamps against the baseline time)
Get-ChildItem -Recurse -File src, tests, frontend\src\app\core, frontend\src\app\shared\http, frontend\src\app\features\refill-requests\data |
  Where-Object LastWriteTime -gt <baseline time>

# Screenshots at exact 360px and 1280px (+ reduced-motion and forced-colors variants);
# reports horizontal overflow per page. Requires API on :5080 and `npm start` on :4200.
node <scratchpad>\shots.mjs specs\002-premium-ui-redesign\screenshots\after
node <scratchpad>\shots.mjs specs\002-premium-ui-redesign\screenshots\after '{"forcedColors":true,"suffix":"-hc"}'
node <scratchpad>\shots.mjs specs\002-premium-ui-redesign\screenshots\after '{"reducedMotion":true,"suffix":"-rm","widths":[360]}'
```

Contrast: token pairs checked with a WCAG contrast calculation script (scratchpad) against
4.5:1 (text) and 3:1 (UI components, focus, large text).

## Risks

| Risk | Mitigation |
|------|------------|
| Breaking test selectors/text | [ui-contract.md](contracts/ui-contract.md) checklist reviewed every increment; specs unchanged. |
| Skeleton removes "Loading" text | Visually hidden status text inside skeleton wrapper (R9). |
| First `time` on queue no longer the list `evaluatedAt` | Keep the "checked at" line before rows and counts free of `time` elements. |
| Reordering review DOM changes focus order | Safety-first order is intended; verified in increment 5. |
| Sticky column overlapping content at mid widths | Sticky only at ≥1024px. |
| Bundle growth from icons | ~14 short path strings; checked by build budget. |
| Visual-only regressions not caught by unit tests | Quickstart manual audit at 360/1280px. |

## Complexity Tracking

No constitution violations; nothing to justify.
