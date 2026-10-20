---

description: "Task list for the Creative Clinical UI (Ambient Atelier) redesign"
---

# Tasks: Creative Clinical UI ("Ambient Atelier")

**Input**: Design documents from `/specs/004-creative-clinical-ui/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: Requested. Spec SC-001 requires every existing test to pass unweakened; research R11
lists the new specs (ambient directive, review wording, queue glow marker). Spec SC-004 to SC-008
require real-browser evidence (quickstart V1–V11). Specs are written first and observed failing.
**No existing assertion is removed or weakened.**

**Organization**: Grouped by user story. US1 (review) and US2 (queue) are both P1 and form the MVP
together; US3 (ambient light) and US4 (cohesion) follow.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: The user story the task belongs to (US1–US4)

## Path Conventions

- `FE` = `frontend/src`; all paths below are under it unless they start with `specs/` or `frontend/`.
- Only `frontend/` and the docs change. No backend file, API, DTO, or test outside `frontend/`
  changes.

## Rules carried into every task

- Keep every hook and text in `specs/004-creative-clinical-ui/contracts/ui-contract.md`
  "Preserved". Change markup only around them.
- Clinical data only on opaque surfaces; decorative layers use `pointer-events: none` and are
  hidden in `@media (forced-colors: active)` and `@media print`.
- Colors only from tokens; the brief's bright hues (`$glow-teal`, `$glow-indigo`, and the
  success/warning/danger brights) are never used for text (research R2).
- No fixed or sticky element below 1024 px.
- Every interactive control is at least `$target-min` (44 × 44 px) below 768 px (spec FR-028);
  sizes and lighting dimensions come from tokens, never raw values (constitution VII).
- **Browser checks** run the API on a port verified free (`Get-NetTCPConnection -LocalPort 5180`
  prints nothing) against a throwaway database set through `ConnectionStrings__RefillDb`. Never use
  the developer's running instance or the `PrescriptionRefill` database. Before any write, confirm
  the responding API is yours: a fresh seed shows 6 Pending, 1 Approved, 1 Denied.

---

## Phase 1: Setup (Baseline)

- [X] T001 In `frontend/`, run `npm run test:ci` and `npm run build`; record counts and initial bundle size in a new "Baseline" section at the end of `specs/004-creative-clinical-ui/quickstart.md` (expected from 003: 98/98; 415.99 kB). Stop if anything fails.
- [X] T002 Create the visual-check script outside the repository, in the session scratchpad as `ui-check.mjs`; research R10. It uses Node 22's built-in `WebSocket` and the DevTools protocol on headless Edge (`msedge --headless=new --remote-debugging-port=<free port>`). Functions:
  - `shot(url, width, height, name)`
  - `overflow()`: `scrollWidth > clientWidth`
  - `emulate({ reducedMotion, hover: 'none', forcedColors, zoom })`
  - `movePointer(x, y)`: `Input.dispatchMouseEvent mouseMoved`
  - `readVars(selector, names)`
  - `animations()`: `document.getAnimations().length`
  - `rowsInView()`
  - `contrastAudit()`: computed text color against the nearest opaque background for every visible text element, reporting pairs below 4.5:1 (3:1 for ≥ 24 px or ≥ 18.66 px bold)
  - `targetSizes(selectors)`: bounding boxes of every visible `a, button, input, select, textarea, [role="radio"], label:has(input[type=radio])`, reporting any smaller than 44 × 44 px by selector (spec FR-028)
  - `scrollBy(y)` and `pointerVarsMatch(x, y)`: after scrolling, compares `--pointer-x/y` with the dispatched viewport coordinates and the hovered row's `--glow-x/y` with the pointer minus `getBoundingClientRect()` origin
  - `longTasks(run)`: checks `PerformanceObserver.supportedEntryTypes.includes('longtask')`; if supported, observes `longtask` entries (`buffered: true`) while `run()` executes and returns each entry's `startTime`, `duration`, and `attribution`; if unsupported, returns `{ supported: false }` so the result is recorded as a limitation
  - `typeText(selector, text, { movePointerEvery })` (`Input.insertText` per character, interleaved with `mouseMoved`) and `scrollQueue({ movePointer: true })` for quickstart V10(b)
- [X] T003 Start a throwaway environment per the Browser-checks rule (API on 5180 with `--seed`; `ng serve` on a free port with a temporary proxy file outside the repo pointing `/api` and `/hubs` to 5180). Capture **before** screenshots: queue Pending/Approved/Denied at 360 and 1280 px, and review seed scenarios 1, 2, 4, 6 at 360 and 1280 px. Record `rowsInView()` at 1280 × 800 into `specs/004-creative-clinical-ui/quickstart.md` "Baseline" (SC-007). Save the screenshots to `specs/004-creative-clinical-ui/screenshots/before/`. Stop the servers afterwards.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tokens, global styles, and the dark-canvas/light-sheet shell that every story renders
in.

- [X] T004 Update `FE/styles/_variables.scss` per `specs/004-creative-clinical-ui/data-model.md` "Design tokens":
  - Add groups: `$canvas-bg #0B172A`, `$canvas-text #F4F7FC`, `$canvas-text-muted #CBD5E1`, `$canvas-accent #5EEAD4`, `$canvas-border rgba(203,213,225,.16)`, and `$glow-teal #0F8989`, `$glow-indigo #6366F1`
  - Add `$color-accent-2 #4F46E5`; change `$color-bg` to `#F4F7FC`
  - Add `$radius-sheet 24px`, `$radius-sheet-sm 16px`, `$shadow-sheet`
  - Motion: `$duration-fast 180ms`, `$duration-base 240ms`, `$duration-slow 300ms`, `$easing-emphasized`
  - Sizing: `$target-min 44px`, `$row-min-height 56px`
  - Lighting: `$ambient-light-size 900px`, `$ambient-light-size-secondary 700px`, `$glow-radius 180px` (data-model "Design tokens")
  - Type scale per research R9: page title 1.5rem, section 1.0625rem, card title 0.9375rem, body 0.9375rem, meta 0.8125rem
  - Keep every existing variable name (other files use them); put a contrast ratio comment on each new text token (values from research R2)
- [X] T005 Update `FE/styles.scss`:
  - `body` background `$canvas-bg`
  - `.sheet` utility: `$color-bg`, `$radius-sheet` top corners, `$shadow-sheet`; `$radius-sheet-sm` and full width at ≤ 480px
  - Buttons: hover `translateY(-1px)` and shadow, `$duration-fast`
  - `:focus-visible` outline unchanged in color but also visible on the canvas: add an outer `$canvas-text` ring only inside `.app-header`/`.demo-notice`
  - Heading scale from T004
  - Extend the existing reduced-motion block to remove transforms and the sheet entrance
  - `@media (forced-colors: active)` hides `.ambient-layer` and `[data-glow]::after`
  - `@media print` hides decorative layers and uses a white background
  - Keep `color-scheme: only light`
- [X] T006 Restructure the shell in `FE/app/app.component.html` and `FE/app/app.component.scss`, keeping the `[role="note"]` notice text exactly:
  - Header and notice sit on the canvas (canvas text tokens; brand mark in `$glow-teal` with white icon)
  - `<main>` gains class `sheet`
  - Add an empty decorative `<div class="ambient-layer" aria-hidden="true"></div>` as the first child of the host: `position: fixed; inset: 0; z-index: 0; pointer-events: none` with two static radial gradients at low alpha: teal of size `$ambient-light-size` at the top-left, and indigo of size `$ambient-light-size-secondary` at the right. Positions are viewport-based (the layer is fixed)
  - Header and main sit above it (`position: relative; z-index: 1`)
  - Header `position: sticky` only at `min-width: 1024px`, static below (contract "Layering rules" 3)
- [X] T007 [P] Restyle `FE/app/shared/ui/connection-indicator/connection-indicator.component.ts` for the dark header: reconnecting and unavailable pills use canvas-safe colors (text ≥ 4.5:1 on their own pill background); texts, roles, and the "Refresh" button unchanged; the Refresh button is at least `$target-min` tall and wide below 768 px.
- [X] T008 Run `npm run test:ci` in `frontend/`. All existing specs must pass (the shell change keeps every hook). Fix markup, not tests.

**Checkpoint**: The app renders on the new canvas and sheet with unchanged behavior.

---

## Phase 3: User Story 1 - Safety status impossible to misread (Priority: P1) 🎯 MVP (with US2)

**Goal**: Blockers, warnings, and the no-issues state are distinguishable by label + icon + text;
the no-issues state is neutral; decision controls are easy to find and never covered on mobile.

**Independent Test**: Seed scenarios 1–6 and a decided request: each safety state is named by text
and icon (grayscale check, quickstart V3), and the decision flows behave as before.

### Tests for User Story 1 (write first, observe failing)

- [X] T009 [P] [US1] Add to `FE/app/features/refill-requests/review/refill-review.redesign.spec.ts`, keeping all existing specs:
  - `labels blockers as preventing approval`: `.alert-danger` contains "Prevents approval" and still "Approval blocked"
  - `asks for clinical review on warnings without hiding that they do not block`: `.alert-warning` contains "clinical review required" and still "do not block approval"
  - `words the no-issues state neutrally`: `.alert-success` contains "No blocking rules apply" and "Review the details before deciding"; the safety section has class `is-clear`
  - `labels the safety state in the panel header as text`: the header badge text is one of "Approval blocked", "Warnings", "No issues found"

### Implementation for User Story 1

- [X] T010 [US1] Update `FE/app/features/refill-requests/review/evaluation-panel.component.html` (and `evaluation-panel.component.ts` only for labels), per research R5:
  - The danger alert gets a "Prevents approval" label line with the octagon icon
  - Warning heading becomes "Warnings — clinical review required. They do not block approval."
  - No-issues text becomes "No blocking rules apply. No allergy or interaction warnings. Review the details before deciding."
  - Add `[class.is-clear]` on the section when there are no reasons and no warnings
  - Keep `.alert-danger/.alert-warning/.alert-success`, the stale notice, and the Refresh button
  - T009 passes
- [X] T011 [US1] Restyle the safety section in `FE/app/features/refill-requests/review/review-panels.scss`:
  - Blocked: 4 px danger left rule and danger-tinted header
  - Warning: 4 px warning rule; never muted
  - `.is-clear .alert-success`: neutral mist surface, slate text, check icon in `$color-accent`, **no green fill** (spec FR-015)
  - Reason labels in semibold uppercase meta size; eligible date tabular
  - The evaluation panel's Refresh button is at least `$target-min` in both dimensions below 768 px
- [X] T012 [P] [US1] Restyle `FE/app/shared/ui/alert.component.ts` and `FE/app/shared/ui/alert.component.scss`: icon size 18, heading weight 600, left rule per kind, opaque backgrounds; keep `.alert`, `.alert-{kind}`, roles.
- [X] T013 [P] [US1] Restyle `FE/app/shared/ui/status-badge.component.ts`: compact pill, leading icon slot per tone (danger octagon, warning triangle, success check, info dot), text tokens only; keep `.badge`, `.badge-{tone}`.
- [X] T014 [US1] Update `FE/app/features/refill-requests/review/refill-review.page.html` and `refill-review.page.scss`:
  - Header hierarchy: eyebrow "Refill request #id" in `$color-accent-2` caps; title per type scale; meta row
  - Two columns with the sticky decision column only at ≥ 1024 px
  - Single column and **no sticky** below
  - Keep the DOM order (evaluation, decision, patient)
  - The "Back to requests" link and the error-state retry are at least `$target-min` in both dimensions below 768 px
- [X] T015 [P] [US1] Restyle `FE/app/features/refill-requests/review/decision-form.component.html` and `decision-form.component.scss`: note field with label and counter; Approve (`btn-primary`) and Deny (`btn-danger`) and the note field at least `$target-min` tall below 768 px; disabled-Approve explanation text visible; keep `.counter`, `.field-error`, `aria-describedby`.
- [X] T016 [P] [US1] Restyle `FE/app/features/refill-requests/review/patient-panel.component.html` and `patient-panel.component.ts`:
  - Identity block; allergy chips (warning palette with icon; text stays visible, not color alone)
  - Requested prescription facts grid: drug, dose, days supply, remaining refills, last filled
  - Other active prescriptions as a compact list
  - Long names wrap
- [X] T017 [P] [US1] Restyle `FE/app/features/refill-requests/review/decided-view.component.ts`: read-only decision summary card with status label + icon, decided time, note; texts unchanged.

**Checkpoint**: All review states meet spec FR-013–FR-016; `npm run test:ci` passes.

---

## Phase 4: User Story 2 - Scan and work the queue quickly (Priority: P1) 🎯 MVP (with US1)

**Goal**: Denser, aligned rows with clear hover and keyboard focus; distinct loading, empty,
no-match, and error states; status switcher, search, and pagination restyled; behavior unchanged.

**Independent Test**: Mouse, keyboard-only, and 360 px use of the queue: all existing behaviors
work; focus visible on every control; no horizontal scrolling; rows per 1280 × 800 view ≥ baseline.

### Tests for User Story 2 (write first)

- [X] T018 [P] [US2] Add to `FE/app/features/refill-requests/queue/refill-queue.redesign.spec.ts`: `marks each row as a glow target` (every `li.queue-item` has a `data-glow` attribute) and `keeps the row link as the first focusable element of each row` (the first focusable descendant of each `li.queue-item` is its `a[href^="/requests/"]`).

### Implementation for User Story 2

- [X] T019 [US2] Update `FE/app/features/refill-requests/queue/refill-queue.page.html`:
  - Add `data-glow` to `li.queue-item`
  - Group the row into identity (avatar, patient, medication) / time / badges columns, keeping the stretched link, `.patient`, `.medication`, `time`, and badge texts
  - Page header with eyebrow in `$color-accent-2`, title, description; keep `h1` texts
  - The search input and the header Refresh button are at least `$target-min` tall (and Refresh wide) below 768 px
  - T018 passes
- [X] T020 [US2] Restyle `FE/app/features/refill-requests/queue/refill-queue.page.scss` per research R7:
  - Rows `min-height: $row-min-height` at ≥ 768 px (`padding: $space-3 $space-4`), single-line identity with ellipsis, time and badges in fixed right columns
  - Below 768 px the stretched row link remains at least `$target-min` tall
  - Two-line stacked layout below 768 px
  - Hover: `$color-surface-subtle` tint
  - Focus: `li.queue-item:focus-within` 2 px `$color-accent` outline plus a 3 px left bar
  - Summary chips with icon + label
  - Results heading and evaluation time as meta row
- [X] T021 [P] [US2] Restyle `FE/app/features/refill-requests/queue/status-filter.component.ts`: segmented control on the sheet with an indigo-tinted selected segment and the count pill; keep the `fieldset`, `legend` "Show", radios, and `.count`. Each segment (its `label`) is at least `$target-min` in both dimensions below 768 px.
- [X] T022 [P] [US2] Restyle `FE/app/shared/ui/pagination.component.ts`: compact bar; Previous, Next, and the page-size select at least `$target-min` in both dimensions below 768 px; texts and structure unchanged.
- [X] T023 [P] [US2] Restyle `FE/app/shared/ui/empty-state.component.ts` and `FE/app/shared/ui/error-state.component.ts`: illustrated icon disc, heading, message, projected action; error uses the danger palette with the retry button; the projected "Clear search" and the retry buttons are at least `$target-min` in both dimensions below 768 px; keep `.empty-state`, `role`s, and texts.
- [X] T024 [US2] Restyle the queue loading skeleton in `FE/app/features/refill-requests/queue/refill-queue.page.scss` to match the new row geometry (shimmer removed under reduced motion; `role="status"` text kept).

**Checkpoint (MVP)**: US1 + US2 complete; `npm run test:ci` passes.

---

## Phase 5: User Story 3 - Ambient pointer-reactive light (Priority: P2)

**Goal**: One shared directive moves a diffused canvas light and per-row glow with the pointer, with
no continuous loop, never blocking input, and static under reduced motion or touch.

**Independent Test**: Quickstart V5–V7 and V10 in a real browser.

### Tests for User Story 3 (write first, observe failing)

- [X] T025 [P] [US3] Create `FE/app/shared/ambient/ambient-light.directive.spec.ts`. Host a test component `<div appAmbientLight><ul><li data-glow style="width:200px;height:40px">row</li></ul></div>`; stub `window.matchMedia` per test; stub `requestAnimationFrame`/`cancelAnimationFrame` with a manual queue. Specs:
  - `enables ambient mode only for fine, hover-capable pointers without reduced motion`: `data-ambient="on"` present or absent across 3 media combinations
  - `switches off when reduced motion is turned on later`: fire the media `change` listener
  - `writes viewport pointer coordinates once per frame`: two `pointermove`s before the frame give one frame and the last event's `clientX/clientY` in `--pointer-x/--pointer-y`
  - `keeps pointer coordinates in viewport space when the page is scrolled`: with the host offset by a scrolled or translated container (`getBoundingClientRect().top` negative), `--pointer-y` still equals the event's `clientY`
  - `writes glow coordinates relative to the hovered data-glow element only`: `--glow-x/--glow-y` equal `clientX − rect.left` and `clientY − rect.top` for that element; other elements are untouched
  - `schedules no frame without movement`
  - `does not run change detection on pointer moves`: `spyOn(ApplicationRef.prototype, 'tick')` not called after moves
  - `adds no listeners and writes nothing when disabled`
  - `removes listeners and cancels a pending frame on destroy`
- [X] T026 [P] [US3] Add to `FE/app/app.component.spec.ts`: `hosts the ambient light on the shell` (the host element has the `appAmbientLight` directive, found via `By.directive`) and `marks the decorative layer as hidden from assistive technology and non-interactive` (`.ambient-layer` has `aria-hidden="true"` and computed `pointer-events: none`).

### Implementation for User Story 3

- [X] T027 [US3] Create `FE/app/shared/ambient/ambient-light.directive.ts` per research R4:
  - Standalone `@Directive({ selector: '[appAmbientLight]' })`; inject `ElementRef`, `NgZone`, `DestroyRef`
  - Enable from two `matchMedia` queries with `change` listeners
  - Use `runOutsideAngular` for passive `pointermove`/`pointerleave` on the host
  - One pending `requestAnimationFrame`; in the frame, write `--pointer-x/y` = the last event's `clientX`/`clientY` (viewport coordinates, for the `position: fixed` canvas light) and, for `target.closest('[data-glow]')` within the host, `--glow-x/y` = `clientX − rect.left`, `clientY − rect.top` using that element's `getBoundingClientRect()` read in the same frame
  - `pointerleave` removes the inline pointer variables, so CSS falls back to the resting position
  - Toggle `data-ambient`; clean everything up in `DestroyRef.onDestroy`
  - Doc comment citing spec FR-017–FR-023 and documenting both coordinate spaces
  - T025 passes
- [X] T028 [US3] Apply the directive and the light CSS in `FE/app/app.component.ts`, `FE/app/app.component.html`, and `FE/app/app.component.scss`:
  - Import `AmbientLightDirective` and put it on the host via `hostDirectives`
  - `.ambient-layer` gradients use `radial-gradient(circle calc($ambient-light-size / 2) at var(--pointer-x, 18%) var(--pointer-y, 12%), …)` for the teal light and an offset indigo light of `$ambient-light-size-secondary`, with a `$duration-slow $easing-standard` transition; the values are viewport coordinates because the layer is fixed
  - Under `:host[data-ambient="on"]` the light follows; otherwise it keeps its static positions
  - A faint version shows through the sheet only around its edges, via the sheet's own subtle top gradient, never behind cards
  - T026 passes
- [X] T029 [US3] Add the row glow in `FE/app/features/refill-requests/queue/refill-queue.page.scss`:
  - `li.queue-item[data-glow]` is `position: relative`
  - `::after` radial gradient at `var(--glow-x) var(--glow-y)` (`$glow-teal` 10% alpha, radius `$glow-radius`), `pointer-events: none`, `opacity: 0`, shown at `:host-context([data-ambient="on"]) li.queue-item:hover::after` with a 240 ms transition
  - Content stays above the glow (`z-index`)
  - No glow on non-interactive panels

**Checkpoint**: Light and glow follow the pointer; static otherwise; all specs pass.

---

## Phase 6: User Story 4 - Cohesive, art-directed identity (Priority: P3)

**Goal**: One deliberate design across shell, queue, review, and decided views, with restrained
motion and preserved notices.

**Independent Test**: Screenshot review of every screen and state (quickstart V1, V2) against
spec FR-005–FR-008 and FR-024–FR-026.

- [X] T030 [US4] Add the single entrance animation in `FE/styles.scss` and `FE/app/app.component.scss`: `.sheet` fades in and rises 4 px over `$duration-slow` on first paint only (`@starting-style` or a keyframe applied once); nothing animates per row; removed under reduced motion (spec FR-024, FR-025).
- [X] T031 [P] [US4] Review and adjust spacing and type across `FE/app/features/refill-requests/queue/refill-queue.page.scss`, `FE/app/features/refill-requests/review/refill-review.page.scss`, and `FE/app/features/refill-requests/review/review-panels.scss`: 4 px scale only (no ad-hoc values), heading levels per research R9, consistent card radius and shadow, back link with hover feedback.
- [X] T032 [P] [US4] Verify the notice and connection indicator on the canvas in `FE/app/app.component.scss`: the notice is readable (`$canvas-text-muted` on `$canvas-bg`, 12.1:1) with its info icon in `$canvas-accent`, and its texts are unchanged (spec FR-004).

**Checkpoint**: All stories complete.

---

## Phase 7: Polish & Verification

- [X] T033 Run `npm run test:ci` and `npm run build` in `frontend/`: all specs pass (expected 98 + new), production build within budget. Run `dotnet test PrescriptionRefill.slnx` from the repo root only to confirm nothing outside `frontend/` changed. If the integration suite shows the fixture database timeouts seen in 003, report it as an environment issue with evidence; do not change backend tests in this feature.
- [X] T034 Verify from a **clean copy**, following `specs/004-creative-clinical-ui/quickstart.md` "Clean-copy verification" (constitution Delivery Constraints):
  - Check that ports 5080 and 4200 are free; if not, use the documented port overrides and record the deviation (never stop or reuse a developer's instance)
  - Copy the repository excluding `bin`, `obj`, `node_modules`, `dist`, `.angular`, `.vs`, `TestResults`
  - Throwaway database via `ConnectionStrings__RefillDb` (never `PrescriptionRefill`)
  - Run the README commands: `dotnet run --project src/PrescriptionRefill.Api -- --seed`; `dotnet run --project src/PrescriptionRefill.Api`; `cd frontend`, `npm ci`, `npm run test:ci`, `npm start`
  - Confirm the clean API answers with fresh seed counts (6 / 1 / 1) before anything else, and that `/api` and `/hubs` work through the dev proxy
  - Against that clean environment, run quickstart V1–V12 with `ui-check.mjs`, including V5's scrolled-pointer check and V12's target sizes at 360 and 767 px
  - V10 performance: with ambient mode confirmed on, type 200 characters into the decision note (pointer moving every 10 characters) and scroll the Pending queue with the pointer moving, recording `longtask` entries over 50 ms. Repeat with ambient mode off (reduced motion emulated) for comparison. Record each long task's timing and attribution, and whether any is attributable to pointer-effect handling (present only in the ambient-on run). If `longtask` is unsupported, record that as a limitation, not a pass, and never report "no long tasks" unless measured
  - Save **after** screenshots to `specs/004-creative-clinical-ui/screenshots/after/`, with grayscale copies of the review states; for V3 record per scenario label seen, icon seen, pass/fail (spec SC-003)
  - Record prerequisites, every command, all results, and any deviations in `specs/004-creative-clinical-ui/quickstart.md` "Results", including `contrastAudit()` output (SC-002) and `rowsInView()` against the baseline (SC-007)
  - Report nothing as passed that was not run
- [X] T035 Fix every weakness found in T034 in the relevant files listed above. Repeat the affected checks and record the before and after of each fix in `specs/004-creative-clinical-ui/quickstart.md` "Results".
- [X] T036 [P] Add `specs/004-creative-clinical-ui/  Creative Clinical UI spec, plan, contract, tasks, screenshots` to the project layout in `README.md`, keeping the 001–003 lines. Update `SPEC.md`: a "Creative Clinical UI (feature 004)" subsection under "Delivered behavior" covering composition, ambient light, safety presentation, and the FR-012 default; verification rows from T033–T035, listing checks not run.
- [X] T037 [P] Add a "Creative Clinical UI (feature 004)" table to `PROMPTS.md` with the prompts and outcomes. Record what was kept or rejected: palette colors rejected as text; sidebar rejected; neutral no-issues state; FR-012 default adopted without an answer.
- [X] T038 Stop every server you started and drop the throwaway database (only one whose name you created this session). Confirm ports 5180 and the ng port are free and that no file outside `frontend/`, `specs/004-creative-clinical-ui/`, `README.md`, `SPEC.md`, and `PROMPTS.md` changed.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (T001–T003)** → **Foundational (T004–T008)** → user stories → **Polish (T033–T038)**.
- **US1 (T009–T017)** and **US2 (T018–T024)** depend only on Foundational and are independent of
  each other.
- **US3 (T025–T029)** depends on Foundational (shell and `.ambient-layer`). T029 depends on T019
  (`data-glow` on rows), and the glow is visible only on the US2 row layout.
- **US4 (T030–T032)** depends on US1–US3 being in place, because it is the cross-screen pass.

### Within each story

Specs first → shared components → page templates → page styles. Files edited by more than one
task, always in this order and never in parallel:

- `refill-queue.page.scss`: T020 → T024 → T029 → T031
- `app.component.scss`: T006 → T028 → T030 → T032
- `styles.scss`: T005 → T030

### Story completion order

```text
Setup → Foundational → (US1 ∥ US2) → US3 → US4 → Polish
                        └ MVP ┘
```

## Parallel Opportunities

- **Foundational**: T007 alongside T006 (different files) after T004–T005.
- **US1**: T009 first; then T012, T013, T015, T016, T017 in parallel; T010 → T011 → T014 in sequence.
- **US2**: T018 first; then T021, T022, T023 in parallel with T019 → T020 → T024.
- **US3**: T025 and T026 in parallel; then T027 → T028 → T029.
- **US1 and US2** can run fully in parallel (no shared files).
- **Polish**: T036 and T037 in parallel after T035.

### Example: User Story 1

```text
T009 specs → parallel: T012 alert | T013 badge | T015 decision form | T016 patient panel | T017 decided view
           → sequence: T010 evaluation panel → T011 safety styles → T014 review page layout
```

## Implementation Strategy

### MVP (US1 + US2)

1. Baseline (T001–T003), then tokens, global styles, and shell (T004–T008).
2. US1 (safety-first review) and US2 (dense queue) in parallel.
3. **Stop and validate**: `npm run test:ci`; quickstart V1–V4 and V8.

### Incremental delivery

4. US3: the ambient light directive and glow; quickstart V5–V7 and V10.
5. US4: entrance motion and the cohesion pass; V9 and V11.
6. Polish: full verification, fixes from real screenshots, docs, cleanup.

Report only executed results; list unexecuted checks as not run.
