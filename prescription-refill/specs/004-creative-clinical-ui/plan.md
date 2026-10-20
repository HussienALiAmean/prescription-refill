# Implementation Plan: Creative Clinical UI ("Ambient Atelier")

**Branch**: `004-creative-clinical-ui` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-creative-clinical-ui/spec.md`

## Summary

Restyle the Angular frontend into a "dark canvas, light sheet" composition (research R1): a
midnight-navy canvas carrying a soft teal and indigo ambient light, with the header and
fictional-data notice on the canvas and all routed content on an opaque Clinical Mist sheet.

One standalone directive, `appAmbientLight`, on the shell drives the light through CSS custom
properties. It uses a single passive pointer listener outside Angular's zone and at most one
animation frame per movement burst, with no continuous loop, and switches off for reduced motion
and touch (R4). Queue rows opt into a local glow with `data-glow`.

The queue becomes denser, with clear hover and focus states (R7). The review keeps its tested
structure but presents blockers, warnings, and the no-issues state with distinct labels, icons, and
neutral wording (R5); nothing is sticky below 1024 px (R6).

Tokens extend the existing SCSS variables with contrast-verified canvas and decorative groups
(R2, R3). No API, data, routing, or rule changes.

## Technical Context

**Language/Version**: TypeScript 5.6, Angular 19 (standalone components, signals, OnPush), SCSS

**Primary Dependencies**: Existing only (`@angular/*` 19, `rxjs`, `@microsoft/signalr`). No new
packages (spec FR-023).

**Storage**: N/A (presentation only)

**Testing**: Karma + Jasmine (`npm run test:ci`, headless Edge); real-browser checks over the
DevTools protocol (research R10); backend `dotnet test` only to confirm nothing changed

**Target Platform**: Evergreen desktop and mobile browsers; light content with a dark canvas

**Project Type**: Web application; only `frontend/` changes

**Performance Goals**: No work while the pointer is still; at most one style write per animation
frame during movement; no measurable effect on typing in the note field or scrolling

**Constraints**: All 002/003 UI-contract hooks and texts preserved; WCAG 2.1 AA; no horizontal
overflow at 360 px or 200% zoom; reduced motion and touch get a static look; no fixed/sticky
element below 1024 px; no web fonts or external requests

**Scale/Scope**: App shell, 2 pages, ~8 shared components, tokens; about 20 frontend files; 1 new
directive with spec

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|-----------|-------|--------|
| I. Pragmatic Clean Architecture | No backend change. One new abstraction (`appAmbientLight`) with an immediate consumer (the shell) replacing what would otherwise be per-row handlers. No component library. | PASS |
| II. Deterministic Server-Side Rules | Safety states are rendered from the server evaluation exactly as before; no client rule logic or note validation; Approve availability still from `evaluation.canApprove`. | PASS |
| III. Workflow Integrity | Decision flow, outcome handling, and messages untouched; only presentation and two wording additions that keep the tested phrases. | PASS |
| IV. Persistence & Concurrency | Not touched. | PASS (N/A) |
| V. Test Evidence | All existing specs kept unchanged; new directive spec and review wording specs; real-browser visual checks reported only if run; unexecuted checks listed as not run. | PASS |
| VI. Security & Data Handling | No new data, logging, or network calls (no web fonts or CDNs); fictional-data and no-sign-in notices kept on every screen. | PASS |
| VII. Frontend Quality | Feature folders kept; HTTP untouched; loading/empty/error/warning/blocked/success states redesigned; keyboard and labels kept; 360/1280 px; spacing, type, and color from shared tokens. | PASS |
| Delivery / Workflow | `SPEC.md`, `README.md` (if affected), `PROMPTS.md` updated at the end; verification recorded from actual runs. | PASS |

**Post-design re-check (after Phase 1)**: PASS. Two risks were checked against the design:
- **Readability over the light.** Light layers sit behind opaque cards, have no pointer events,
  and disappear in forced colors (contract "Layering rules").
- **The reassuring no-issues state.** It is styled neutral, not green, and keeps the tested text
  (R5).

No complexity tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/004-creative-clinical-ui/
├── plan.md              # This file
├── research.md          # Phase 0: R1–R11
├── data-model.md        # Phase 1: tokens + ambient runtime state (no data changes)
├── quickstart.md        # Phase 1: automated + visual checks V1–V11
├── contracts/
│   └── ui-contract.md   # Phase 1: preserved hooks, additions, layering rules
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 (/speckit-tasks; not created here)
```

### Source Code (repository root)

```text
frontend/src/
├── styles/_variables.scss                 # canvas + decorative groups, motion, type scale (R2, R3, R8, R9)
├── styles.scss                            # canvas body, sheet, buttons, focus, reduced motion, forced colors
├── app/
│   ├── app.component.{ts,html,scss}       # shell: canvas, header, notice, sheet; appAmbientLight host
│   ├── app.component.spec.ts              # + directive present on the shell
│   ├── shared/
│   │   ├── ambient/ambient-light.directive.ts       # NEW (R4)
│   │   ├── ambient/ambient-light.directive.spec.ts  # NEW
│   │   └── ui/*.ts                        # alert, badge, empty/error state, pagination, connection indicator restyle
│   └── features/refill-requests/
│       ├── queue/refill-queue.page.{html,scss}      # density, hover/focus, data-glow (R7)
│       ├── queue/status-filter.component.ts         # restyle
│       └── review/
│           ├── evaluation-panel.component.html      # labels, icons, wording (R5)
│           ├── review-panels.scss, refill-review.page.scss, decision-form.component.scss, patient-panel  # layout (R6)
│           └── refill-review.redesign.spec.ts       # + wording assertions
```

**Structure Decision**: Keep the existing frontend layout and the 002 design-system files. The only
new folder is `shared/ambient/` for the directive and its spec. No backend files change.

## Implementation Order

1. Baseline: `npm run test:ci`, `npm run build`, and "before" screenshots (V1, V2, V11).
2. Directive and spec first (tests before code); then tokens, global styles, and the shell.
3. Queue, then review; then shared components.
4. Real-browser checks V1–V11 on a free port with a throwaway database; iterate on weaknesses.
5. Docs: `SPEC.md`, `PROMPTS.md`; record only executed results.

## Complexity Tracking

No constitution violations; nothing to justify.
