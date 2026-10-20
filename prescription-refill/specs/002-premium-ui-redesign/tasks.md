---

description: "Task list for the Premium Clinical UI Redesign (revised 2026-10-20)"
---

# Tasks: Premium Clinical UI Redesign

**Input**: Design documents from `/specs/002-premium-ui-redesign/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: Requested (spec FR-005, SC-001, SC-008; research R12). Existing specs are not
edited, except `status-filter.component.spec.ts` (intentional change, research R7).

**Order (author decision)**: Foundation → Queue → Review → Responsive/a11y → Regression and
visual audit. Phase numbering follows that order; story labels follow spec priorities.

**Paths**: relative to the repository root; `FE` = `frontend/src`.

**Standing rules**: keep every hook in [contracts/ui-contract.md](contracts/ui-contract.md);
do not touch `FE/app/**/data/**`, `FE/app/core/**`, `FE/app/shared/http/**`, backend `src/**`,
or `tests/**`; only tokens from `FE/styles/_variables.scss`; no new dependencies.

## Format: `[ID] [P?] [Story] Description`

- **[Story]**: US1 review (P1), US2 queue (P2), US3 trust/connection (P3), US4 responsive/a11y (P3)

---

## Phase 1: Setup (Baseline)

- [x] T001 Run `npm run test:ci` and `npm run build` in `frontend/`; record baseline (56/56 pass; initial bundle 350.94 kB) in `specs/002-premium-ui-redesign/quickstart.md` "Baseline"
- [x] T002 Capture before screenshots at exact 360px and 1280px into `specs/002-premium-ui-redesign/screenshots/before/` with the DevTools-protocol script (research R14)

---

## Phase 2: Foundation (Increment 1)

**⚠️ No queue or review work begins until `npm run test:ci` is green at the end of this phase.**

- [x] T003 Extend `FE/styles/_variables.scss` per research R2 (keep existing names; slate/teal palette; semantic border tokens; type scale incl. `$font-size-xs`/`$font-size-2xl`; weights; `$space-7/8`; `$radius-sm/md/pill`; `$shadow-xs/sm/md`; motion tokens; `$breakpoint-sm/lg`; `$content-max-width: 1200px`); contrast ratios in comments
- [x] T004 Rework `FE/styles.scss`: font stack, heading scale, `:focus-visible` outline, `.tabular`, `.card`, page-header pattern, `.skeleton`, `.btn` variants (`-primary`, `-danger`, `-ghost`, `-sm`, spinner), inputs/selects with strong border, reduced-motion block; keep existing class names
- [x] T005 [P] Create `FE/app/shared/ui/icon.component.ts` (Lucide-derived inline SVG set, ISC attribution, `aria-hidden`)
- [x] T006 [P] Add `FE/app/shared/ui/icon.component.spec.ts` (svg rendered, host aria-hidden, unknown name renders nothing)
- [x] T007 [P] Update `FE/app/shared/ui/alert.component.ts` + `.scss`: kind icon, title row, tinted surface, info text color; keep classes and roles
- [x] T008 [P] Update `FE/app/shared/ui/status-badge.component.ts`: `info`/`accent` tones, dot, semantic borders; keep `.badge`, `.badge-{tone}`
- [x] T009 [P] Update `FE/app/shared/ui/empty-state.component.ts`: optional `heading`, `icon`, projected action; keep `.empty-state` + `role="status"` containing the message
- [x] T010 [P] Update `FE/app/shared/ui/error-state.component.ts`: icon button; keep danger alert and single button
- [x] T011 [P] Create `FE/app/shared/ui/pagination.component.ts` (research R13: `nav[aria-label=Pagination]`, "Showing x–y of N", "Page p of P", Previous/Next with disabled boundaries, labeled page-size select 10/25/50; outputs `pageChange`, `pageSizeChange`)
- [x] T012 [P] Add `FE/app/shared/ui/pagination.component.spec.ts` (ranges for totals 1/10/11/26 at size 10; Previous disabled on page 1; Next disabled on last page; emits page and size changes; hidden when total is 0)
- [x] T013 [P] Add `FE/app/shared/ui/shared-ui-redesign.spec.ts` for empty-state heading + projected action
- [x] T014 Restyle `FE/app/shared/ui/connection-indicator/connection-indicator.component.ts` as pills (reconnecting: warning + spinner; unavailable: danger outline + `wifi-off`); same texts, roles, empty while connected
- [x] T015 Rework shell `FE/app/app.component.html`, `.scss`, `.ts`: product mark + name, connection pill, neutral info-style demo strip (same text and `role="note"`), consistent page container
- [x] T016 Safety exception: in `FE/app/features/refill-requests/review/refill-review.page.html` and `.scss`, put the decision column (evaluation + decision/decided view) before the patient panel in DOM order; at ≥1024px place details left, decisions right via grid areas
- [x] T017 Run `npm run test:ci`; all pass

---

## Phase 3: User Story 2 — Queue screen (Priority: P2, Increment 2)

**Independent Test**: quickstart §3 Queue.

- [x] T018 [US2] Rewrite `FE/app/features/refill-requests/queue/status-filter.component.ts` as a segmented radio switcher (fieldset, visually hidden legend "Show", radios valued Pending/Approved/Denied, optional `count` input shown only on the selected option); keep `value` input and `changed` output
- [x] T019 [US2] Rewrite `FE/app/features/refill-requests/queue/status-filter.component.spec.ts` with equivalent assertions (group name "Show", option order, Pending checked, selecting Denied emits Denied) plus count only on the selected option — intentional change, research R7
- [x] T020 [US2] Add to `FE/app/features/refill-requests/queue/refill-queue.page.ts`: `query`, `visibleItems`, `counts`, `page`, `pageSize`, `pageCount`, `currentPage`, `pagedItems`, `reloading`, `setQuery()`, `clearQuery()`, `goToPage()`, `setPageSize()`; reset page on status change in the existing effect; `load()` API usage unchanged
- [x] T021 [US2] Rework `FE/app/features/refill-requests/queue/refill-queue.page.html`: header (h1 unchanged text, description, Refresh with progress), switcher with active count, Pending summary counts, labeled search with result status, "Checked against current data at" line (first `time`), skeleton rows with hidden "Loading requests…", rows from `pagedItems()` (stretched link, pill/clock icons, badges, chevron, decided modifier), two empty states, `app-pagination`
- [x] T022 [US2] Rewrite `FE/app/features/refill-requests/queue/refill-queue.page.scss` (toolbar, summary, row grid, stretched link, hover/focus-within, skeleton, wrapping)
- [x] T023 [P] [US2] Add `FE/app/features/refill-requests/queue/refill-queue.redesign.spec.ts`: counts; search filters and keeps order; no-match empty state + Clear; query survives reload; pagination over 0/1/10/11/26 items — union of pages equals list, no duplicates; search/status/size reset to page 1; shrinking reload clamps page; first `time` is `evaluatedAt`
- [x] T024 [US2] Run `npm run test:ci`; existing `refill-queue.page.spec.ts` passes unchanged

---

## Phase 4: User Story 1 — Review screen (Priority: P1, Increment 3)

**Independent Test**: quickstart §3 Review.

- [x] T025 [US1] Finish `FE/app/features/refill-requests/review/refill-review.page.html` + `.scss`: ghost back link with icon, header with h1 (same text), status badge, requested time; sticky decision column ≥1024px; skeleton loading with hidden "Loading request…"; notice region styling
- [x] T026 [US1] Add `pendingDecision` signal to `FE/app/features/refill-requests/review/refill-review.page.ts` (set in `decide()`, cleared on response); no other logic change
- [x] T027 [US1] Rework `FE/app/features/refill-requests/review/evaluation-panel.component.html` + `.ts`: summary badge, blockers first ("Approval blocked", count), warnings ("Warnings — do not block approval"), compact success, footer with evaluated time, staleness text, Refresh
- [x] T028 [US1] Rework `FE/app/features/refill-requests/review/decision-form.component.html`, `.scss`, `.ts`: `pending` input, counter, progress labels, info row for disabled Approve; keep ids and classes
- [x] T029 [P] [US1] Rework `FE/app/features/refill-requests/review/decided-view.component.ts`: outcome banner, note block, read-only label and text
- [x] T030 [P] [US1] Rework `FE/app/features/refill-requests/review/patient-panel.component.html` + `.ts`: patient header, allergy chips, prescription facts grid, compact active list
- [x] T031 [US1] Update `FE/app/features/refill-requests/review/review-panels.scss` for the new panel parts
- [x] T032 [P] [US1] Add `FE/app/features/refill-requests/review/refill-review.redesign.spec.ts`: evaluation before patient panel; blocker alert before warning alert; skeleton loading text; decision-form progress label and both buttons disabled; counter shows `n / 1000`
- [x] T033 [US1] Run `npm run test:ci`; existing review and decision-form specs pass unchanged

---

## Phase 5: User Story 3 + 4 — Trust, responsive, accessibility, high contrast (Increment 4)

- [x] T034 [US4] Capture interim screenshots (R14) at 360/768/1024/1280; fix overflow, wrapping, density in the affected `.scss` files
- [x] T035 [US4] Add `@media (forced-colors: active)` rules in `FE/styles.scss` (badge/alert/chip/segment borders, focus `Highlight`, selected segment `Highlight`)
- [x] T036 [US4] Verify heading outline and tab order on both screens; fix templates if needed
- [x] T037 [US4] Verify token contrast pairs with a WCAG calculation; adjust `FE/styles/_variables.scss` if below AA
- [ ] T038 [US3] Verify connection pill states visually by stopping/starting is not possible without affecting the user's API; verify via unit spec rendering and a forced `disconnected` render in a screenshot harness if available, otherwise document as manual check

---

## Phase 6: Regression and visual audit (Increment 5)

- [x] T039 Capture after screenshots (normal, `-hc` forced colors, `-rm` reduced motion) into `specs/002-premium-ui-redesign/screenshots/after/`
- [x] T040 Review the after screenshots for spacing, alignment, density, hierarchy, clipping; perform and record at least one refinement pass in `specs/002-premium-ui-redesign/quickstart.md` "Results"
- [x] T041 Run `npm run test:ci` and `npm run build`; compare with baseline
- [x] T042 Run `dotnet test`; report results or "not run"
- [x] T043 Scope check against baseline time 2026-10-20T18:29:53+03:00 (no changes in `src/`, `tests/`, `FE/app/core/`, `FE/app/shared/http/`, `FE/app/features/refill-requests/data/`)
- [x] T044 [P] Update `SPEC.md` and `PROMPTS.md`; README only if it describes the UI
- [x] T045 Final summary by file, verification results, intentional test changes, known limitations (incl. server pagination proposal) in `specs/002-premium-ui-redesign/quickstart.md` "Results"

---

## Dependencies

- Phase 1 → Phase 2 → Phase 3 (queue) → Phase 4 (review) → Phase 5 → Phase 6.
- Within Phase 2, T005–T013 are parallel; T015 depends on T005 and T014; T016 is independent.
- Queue and review touch disjoint files; the order is the author's choice.

## Implementation Strategy

Ship increment by increment; every phase ends with a green test run. The safety reorder
(T016) lands in Foundation so the most important review defect is fixed first.
