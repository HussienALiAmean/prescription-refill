# Prescription Refill Requests — Submission Summary

> **Status: implemented and verified on 2026-10-20.** Detailed, authoritative requirements:
> [specs/001-refill-request-review/spec.md](specs/001-refill-request-review/spec.md).
> The sections below summarize the architecture, data model, API, and rule tests; where they
> differ, the spec, [data-model.md](specs/001-refill-request-review/data-model.md), and
> [contracts/http-api.md](specs/001-refill-request-review/contracts/http-api.md) win.
> Setup: [README.md](README.md). AI usage: [PROMPTS.md](PROMPTS.md).

## Delivered behavior

- **Clinician queue and review** of Pending requests with patient profile, allergies, the
  requested prescription, and other active prescriptions (spec FR-018, FR-022).
- **Server evaluation** with its evaluation time, blocking reasons, and warnings
  (FR-005–FR-009); exact 80% timing rule on elapsed UTC duration, equality allowed (FR-006);
  invalid stored timing data blocks approval (FR-027).
- **Evaluation lifecycle**: evaluated on open, on Refresh, from a rejected approval, and on live
  hints or reconnect; no polling. Approve is disabled when the latest evaluation blocks it, as a
  convenience only; the server re-evaluates on every approval (`422 approval_blocked` returns the
  current evaluation). Deny is always available (FR-011, FR-014, FR-023).
- **Decisions** require a note (server rule only), are final, and are concurrency-safe: approval
  and refill decrement commit in one transaction with SQL Server `rowversion` on the request and
  the prescription; conflicts roll back fully and return `409 concurrency_conflict` with no
  retry; refills never go below zero (FR-010–FR-017, FR-025). After a conflict the client shows
  no success, reloads, explains, and keeps the typed note (FR-032).
- **Read-only history** of Approved/Denied requests through a status filter, newest decision
  first (FR-018).
- **API-only request creation** with inactive-prescription rejection and one Pending request per
  prescription enforced by a filtered unique index (FR-001–FR-004, FR-021).
- **Live updates**: after commit, a SignalR `RequestUpdated` hint (request ID and change only)
  makes other screens reload from the API; screens also reload on reconnect and when shown again
  (FR-029–FR-031).
- **SQL Server is the only source of truth**; no caching of status, refills, or eligibility
  (FR-028).
- **Fictional data notice** and **no sign-in notice** on every screen (FR-019, FR-020).

### UI redesign (feature 002, presentation only)

Spec: [specs/002-premium-ui-redesign/spec.md](specs/002-premium-ui-redesign/spec.md). No server,
API, DTO, or SignalR change.

- Shared design system: slate/teal tokens (all text pairs WCAG AA, verified), icons, alerts,
  badges, empty/error states, skeletons, pagination, focus outline that survives high-contrast
  mode, reduced-motion support.
- Queue: segmented status switcher with an exact count for the loaded status, Pending summary
  (blocked / warnings / no issues), search on patient or medication, pagination (10/25/50).
  Search, counts, and paging moved to the server in feature 003 (below).
- Review: safety checks and the decision come before patient details on narrow screens; sticky
  decision column on wide screens; blockers, warnings, and clear states use distinct color, icon,
  and wording; per-button progress; note character guidance; read-only decision summary.

### Server-side pagination (feature 003)

Spec: [specs/003-backend-pagination/spec.md](specs/003-backend-pagination/spec.md). Only the
list endpoint and the queue screen changed; rules, decisions, detail, creation, and live
updates did not.

- `GET /api/refill-requests` accepts optional `page`, `pageSize` (1–100, default 10), and
  `search` (case-insensitive "contains" on patient or medication name, at most 100 characters).
  Without them the response is exactly the 001 response (full list).
- A paged response adds `page`, `pageSize`, `matchingCount`, `statusTotal`, and, for Pending,
  a whole-status `summary` (blocked / warnings / clear) from the same evaluation instant.
- Approved/Denied load only counts and one page; Pending still evaluates every Pending request
  per load (the summary needs them all), then returns one page.
- The queue requests one page at a time (search debounced 300 ms), shows only the newest
  response, keeps page, search, and page size on live-update reloads, and moves to the last
  existing page when its page disappears. The search text is never logged.

### Creative Clinical UI (feature 004, presentation only)

Spec: [specs/004-creative-clinical-ui/spec.md](specs/004-creative-clinical-ui/spec.md). No API,
data, routing, or rule change.

- Composition: a midnight-navy canvas carries the header, the fictional-data notice, and a soft
  ambient light; all routed content sits on an opaque Clinical Mist sheet with white cards. The brief's
  bright palette colors failed AA as text, so they are used only for light, glow, and marks; text keeps
  contrast-verified tokens (0 failures in a 215-element audit).
- Ambient light: one directive (`appAmbientLight`) on the shell, one passive pointer listener outside
  Angular's zone, at most one animation frame per movement burst, nothing while the pointer is still.
  The fixed canvas light uses viewport coordinates (stays under the pointer when scrolled); queue rows
  opt in with `data-glow` and get row-relative coordinates. Off for reduced motion and touch; hidden in
  forced colors and print; never intercepts input.
- Safety presentation: blockers ("Approval blocked — Prevents approval"), warnings ("clinical review
  required. They do not block approval."), and a neutral "No issues found … Review the details before
  deciding." state, each with label, icon, and rule; no reassuring green for the no-issues state.
- Queue: 56 px rows (7 per 1280 × 800 view, was 4), keyboard focus outline plus left bar, 44 px targets
  below 768 px, nothing sticky below 1024 px. Only the selected status's count is shown (FR-012: a
  default adopted at planning because the clarification was not answered).


| Project | Responsibility |
|---------|----------------|
| `PrescriptionRefill.Domain` | Entities and every rule: `RefillEvaluator` (blocking reasons and warnings), `ActiveMedications`, `RefillRequest.Approve/Deny` (note, status, re-evaluation, refill use). No EF Core or ASP.NET Core dependency |
| `PrescriptionRefill.Application` | Use cases (`GetRefillRequests`, `GetRefillRequestDetail`, `CreateRefillRequest`, `ApproveRefillRequest`, `DenyRefillRequest`), DTOs, and `RefillDecisionContextLoader`, which loads stored data without applying rules |
| `PrescriptionRefill.Infrastructure` | `RefillDbContext` (SQL Server), configurations, migration, `DemoDataSeeder`; maps `rowversion` and unique-index violations to `ConcurrencyConflictException` / `PendingRequestExistsException` |
| `PrescriptionRefill.Api` | Minimal API endpoints, ProblemDetails error mapping, SignalR hub `/hubs/refill-requests` |
| `frontend/` | Angular 19 queue and review screens; displays server evaluations and errors, applies no rule |

**Deterministic, AI-free evaluation.** Rules are plain C# in the Domain: pure functions of the
stored data and one instant from an injected `TimeProvider` (`FakeTimeProvider` in tests). The
80% threshold is integer tick arithmetic (`DaysSupply × TicksPerDay × 4 / 5`), no floating
point. The application calls no AI model or external service at runtime; AI was used only to
plan, write, and review code ([PROMPTS.md](PROMPTS.md)). The same `RefillEvaluator.Evaluate`
serves the queue, the review screen, and approval processing.

### Bilingual localization and themes (feature 005)

Spec: [specs/005-bilingual-localization-themes/spec.md](specs/005-bilingual-localization-themes/spec.md).
No rule, code, status, transaction, concurrency, or live-update change.

- **Languages**: Arabic is the default and is right-to-left; English is left-to-right. The header has
  a language control (العربية / English, each named in its own language) and a Light/Dark theme
  control. Both are kept per browser in `localStorage` (`prx.language`, `prx.theme`), independently
  of each other. A boot script in `index.html` applies them before the first paint, so there is no
  flash of the wrong direction or theme. Switching needs no page reload: the current screen reloads
  its server data and keeps status, search, page, page size, and any typed note.
- **Server language**: the API resolves each request from `Accept-Language` only, using ASP.NET Core
  request localization. `ar` and `en` and their regional variants map to that language; anything
  else is Arabic. Responses carry `Content-Language`. Codes, statuses, ids, numbers, and instants are
  identical in both languages; only `message`, `title`, `detail`, `errors`, and names change. Message
  text moved out of Domain, which now returns codes and facts, into a typed catalog (`IAppText`,
  English verbatim). This amends 001 FR-023: a language switch is a reload trigger.
- **Reference data**: medications store `NameAr`/`NameEn` and interactions store
  `DescriptionAr`/`DescriptionEn`, with no translation table. Every name and description in a
  response is selected in SQL for one language by AutoMapper `ProjectTo` with one configuration per
  language; the SQL names only that language's column (tested). All safety values still come from
  the rule read. Search matches the medication name in the active language only (amends 003 FR-007).
- **Text**: patient names, dosages, and notes are never translated; they render in their own
  direction (`dir="auto"`/`<bdi>`). Dates keep the 001 format with localized month names and
  Western digits; the time and offset sit in a left-to-right isolate (amends the 001 FR-024 example).
- **Dark theme**: every color role is a CSS custom property with a Light (004, unchanged) and a Dark
  value. Clinical content stays on opaque surfaces (amends 004 FR-007 for Dark). Print always uses
  Light. All 70 checked token pairs (35 per theme) meet WCAG AA
  (`specs/005-bilingual-localization-themes/scripts/contrast-check.mjs`).
- **AutoMapper** 16.2.0 is used under the Lucky Penny Community License (license verified
  2026-10-21; research R1). The key comes from configuration `AutoMapper:LicenseKey` and is never
  committed.

## Data model

Full field rules: [data-model.md](specs/001-refill-request-review/data-model.md). All data is
fictional.

| Entity | Key fields | Relationships and constraints |
|--------|------------|-------------------------------|
| Patient | `FullName`, `DateOfBirth` | has many Allergies and Prescriptions |
| Medication | `NameAr`, `NameEn` (each unique, synthetic; 005) | referenced by Prescription, Allergy, DrugInteraction |
| Allergy | `PatientId`, `MedicationId` | unique per patient and medication; matching is exact medication |
| DrugInteraction | `MedicationAId`, `MedicationBId`, `DescriptionAr`, `DescriptionEn` (005) | CHECK `MedicationAId < MedicationBId`; unique pair; 4 seeded synthetic pairs |
| Prescription | `Dosage`, `DaysSupply`, `RemainingRefills`, `LastFilledAt` (UTC), `IsActive`, `RowVersion` | belongs to Patient and Medication; CHECK `DaysSupply > 0`, `RemainingRefills >= 0` |
| RefillRequest | `Status` (`Pending`/`Approved`/`Denied`), `RequestedAt`, `DecidedAt`, `DecisionNote` (≤ 1,000), `RowVersion` | belongs to Prescription; filtered unique index `UX_RefillRequests_Pending` allows one Pending request per prescription |

Evaluations are computed on every read and approval, never stored. Approval writes the request
and the prescription (`RemainingRefills − 1`) in one transaction checked by both row versions;
denial writes only the request.

## API

Full contract and examples: [contracts/http-api.md](specs/001-refill-request-review/contracts/http-api.md).
Errors are ProblemDetails with a stable `code`. Since feature 005, every endpoint reads
`Accept-Language` (`ar` default, or `en`) for human-readable text and names, and answers with
`Content-Language` ([005 contract](specs/005-bilingual-localization-themes/contracts/http-api.md)).
A client that sends no header now gets Arabic text; `Accept-Language: en` returns the English output
as before.

| Method and route | Purpose | Success | Errors |
|------------------|---------|---------|--------|
| `GET /api/refill-requests?status=` | Queue (`Pending`, default, oldest first) or history (`Approved`/`Denied`, newest decision first); Pending items carry `canApprove`/`hasWarnings`. Optional `page`, `pageSize`, `search` return one page (feature 003) | `200` list with `evaluatedAt`; paged: plus `page`, `pageSize`, `matchingCount`, `statusTotal`, Pending `summary` | `400 validation_failed` (unknown status; invalid `page`, `pageSize`, or `search`) |
| `GET /api/refill-requests/{id}` | Review: patient, allergies, requested prescription, other active prescriptions, and a fresh evaluation while Pending | `200` detail | `404 not_found` |
| `POST /api/refill-requests/{id}/approve` | Approve with `{ "note" }`; server re-evaluates, then uses one refill | `200` decided detail | `400 validation_failed`, `404 not_found`, `409 request_already_decided`, `409 concurrency_conflict`, `422 approval_blocked` (with current evaluation) |
| `POST /api/refill-requests/{id}/deny` | Deny with `{ "note" }`; allowed even when approval is blocked; refills unchanged | `200` decided detail | `400 validation_failed`, `404 not_found`, `409 request_already_decided`, `409 concurrency_conflict` |
| `POST /api/refill-requests` | Create with `{ "prescriptionId" }` (API only, no UI) | `201` detail + `Location` | `400 validation_failed` (bad body or inactive prescription), `404 not_found`, `409 pending_request_exists` |

Any unhandled failure returns `500 unexpected_error` without internal details. There is no
update or delete endpoint.

## Safety rules and their tests

Domain tests: `tests/PrescriptionRefill.Domain.Tests` (`RefillEvaluatorTests`,
`RefillRequestDecisionTests`, `PrescriptionTests`, `CreateRefillRequestTests`). API tests run
against SQL Server: `tests/PrescriptionRefill.IntegrationTests`.

| Rule | Behavior | Tests |
|------|----------|-------|
| Allergy | Recorded allergy to the requested medication → `ALLERGY_CONFLICT` warning; never blocks | `RefillEvaluatorTests.Allergy_to_the_requested_medication_warns_but_does_not_block`, `.Allergy_to_another_medication_does_not_warn` |
| Interaction | Seeded pair with another **active** medication → `DRUG_INTERACTION` warning; never blocks | `RefillEvaluatorTests.Interaction_with_another_active_medication_warns_in_either_pair_order`, `.Same_medication_on_another_active_prescription_is_not_an_interaction`, `.Warnings_never_block_even_together` |
| Active prescriptions | The patient's other prescriptions with `IsActive`; inactive ones neither warn nor appear as active | `RefillEvaluatorTests.Interacting_medication_only_on_an_inactive_prescription_does_not_warn`, `.Active_medications_are_the_patients_other_active_prescriptions`; `QueueAndDetailTests.Inactive_other_prescriptions_are_not_listed_as_active` |
| No refills | `RemainingRefills` 0 → `NO_REFILLS_REMAINING` blocks approval; never goes negative | `RefillEvaluatorTests.Zero_refills_blocks_approval`; `PrescriptionTests.Consume_refill_at_zero_is_rejected_and_never_goes_negative`; `DecisionEndpointTests.Zero_refills_returns_approval_blocked`; `SchemaTests.Remaining_refills_cannot_be_negative` |
| 80% threshold | Elapsed time since last fill < 80% of `DaysSupply` → `TOO_EARLY` with `eligibleAt` | `RefillEvaluatorTests.One_tick_before_the_threshold_is_too_early`; `DecisionEndpointTests.One_tick_too_early_returns_approval_blocked_with_the_current_evaluation` |
| 80% exact boundary | Exactly 80% is allowed (strict `<`); time zone offsets do not matter | `RefillEvaluatorTests.Exactly_at_the_threshold_is_not_blocked`, `.One_tick_after_the_threshold_is_not_blocked`, `.Offsets_do_not_change_the_result` |
| Invalid timing data | Future or missing last fill, or non-positive supply → `INVALID_PRESCRIPTION_DATA` blocks | `RefillEvaluatorTests.Last_filled_after_now_is_invalid_data_not_too_early`, `.Non_positive_days_supply_is_invalid_data`, `.Missing_last_filled_time_is_invalid_data` |
| Combined results | All blockers and warnings are reported together | `RefillEvaluatorTests.Blockers_and_warnings_are_all_reported`, `.Timing_reason_comes_before_the_refill_reason` |
| Server re-evaluation | Approval re-runs every rule on current stored data; the displayed evaluation is never trusted | `RefillRequestDecisionTests.Approval_re_evaluates_at_decision_time`, `.Blocked_approval_changes_nothing_and_returns_the_evaluation`; `DecisionEndpointTests.Stale_displayed_evaluation_is_ignored_and_the_current_reasons_are_returned`, `.Displayed_evaluation_and_approval_use_the_same_calculation` |
| Required note | Both decisions need a non-blank note of at most 1,000 characters (server only) | `RefillRequestDecisionTests.Blank_notes_are_rejected_for_both_decisions`, `.Note_of_maximum_length_is_accepted_and_one_more_is_rejected`; `DecisionEndpointTests.Blank_note_returns_validation_failed_and_changes_nothing`, `.Oversized_note_and_malformed_body_return_validation_failed`; frontend `decision-form.component.spec.ts` "does not apply its own note rule" |
| Immutable decisions | Approved/Denied requests reject further decisions; one refill used per approval; denial uses none | `RefillRequestDecisionTests.Decided_requests_reject_further_decisions_without_changes`, `.Approve_from_pending_consumes_one_refill_and_records_the_decision`, `.Deny_from_pending_does_not_change_the_prescription`; `DecisionEndpointTests.Repeated_decisions_are_rejected_and_refills_are_consumed_once`, `.Requests_cannot_be_edited_or_deleted` |
| Concurrency | Conflicting approvals roll back fully (`409`); one Pending request per prescription | `DecisionConcurrencyTests.*` (5 tests), `CreateConcurrencyTests.*` (4 tests), `SchemaTests.Pending_index_is_unique_and_filtered_to_pending_requests` |
| Paged list flags and counts (003) | Paged items carry the same server flags as the unpaged list; the Pending summary counts those same evaluations | `PagedListTests.Pending_page_flags_equal_the_unpaged_flags`; `ListSummaryTests.Summary_equals_counts_from_the_unpaged_flags`, `.Summary_is_identical_on_every_page_and_with_a_search` |

## Known limitations

- No authentication or authorization; the live update channel is unauthenticated and not
  production-secure; not suitable for real patient data (FR-020).
- Dispensing is not modeled; more than one request may be approved within the same supply period
  while refills remain (FR-026).
- Live notifications are best effort (no outbox); screens recover on reconnect, return, or
  Refresh (FR-031).
- All patients, medications, and interactions are fictional demo data, not clinical guidance
  (FR-019).
- Built and verified with preview .NET SDKs (11.0.100-rc.2 targeting `net10.0`) and Angular 19,
  which is past its support window; neither was upgraded (research R1, R9).
- On Windows, very deep clone paths can exceed the 260-character path limit for the SQL Server
  client DLL (README "Prerequisites").
- Paging is offset-based: when requests are decided between page loads, items can shift between
  pages; each reload reflects current data (003 spec Assumptions). Every Pending request is
  still evaluated on each Pending list load, because the summary covers the whole status.
- Counts are shown only for the selected status; other statuses show no count because their
  records are not loaded (002 research R6, R7).
- The Arabic interface and server texts have not yet been reviewed by an Arabic speaker (005
  quickstart V13, not run).
- Arabic search is a literal "contains" match, with no normalization of letter forms or diacritics
  (005 spec Assumptions). Each browser tab picks up the other tab's preference changes only when it
  reloads.
- API clients that send no `Accept-Language` now get Arabic text and names instead of English
  (005 spec C7).

## Verification

All results below come from commands actually run on 2026-10-20 (local SQL Server 2022, .NET
SDK 11.0.100-rc.2 → net10.0, Angular 19.2, headless Edge 155). Nothing listed was assumed.

| Checkpoint | Command / method | Result |
|------------|------------------|--------|
| Foundation (T029) | `dotnet build`, `dotnet test`, `npm run test:ci`, `ng build` | 0 warnings; 11/11 integration; 12/12 frontend; build OK |
| US1 (T052) | `dotnet test` (3 repeated integration runs) | Domain 44/44; integration 36/36 each run |
| US1 (T052) | Seeded dev DB, scripted HTTP checks of quickstart V1–V7, V9–V11 | 12/12 PASS |
| US2 (T060) | `dotnet test` (3 integration runs) | Domain 47/47; integration 47/47 each run |
| US3 (T065) | `dotnet build`, `dotnet test`, `npm run test:ci`, `ng build` | 0 warnings; 47/47; 53/53; 43/43; build OK |
| US4 (T073) | Real SignalR client through the Angular dev proxy + HTTP decisions | Hint after a committed denial with only `requestId` and `change`; none for a rejected (409) decision |
| Accessibility (T074) | DevTools-protocol audit, mobile emulation 360px and desktop 1280px, 4 pages each | 8/8 PASS: no horizontal scroll, every field labeled, every control named, logical tab order |
| Clean environment (T076) | Fresh copy without build output or `node_modules`, empty database, README steps | Seed, migrations, `dotnet test` (47/47, 61/61), `npm ci`, `npm run test:ci` (56/56), `npm start` + proxy, README `curl` (201), `ef database drop` all worked; about 3 minutes of commands, well under SC-005's 20 minutes. Found and documented the Windows long-path issue |
| **Final (T079)** | `dotnet build PrescriptionRefill.slnx` | 0 warnings, 0 errors |
| **Final (T079)** | `dotnet test` | Domain **47/47**, SQL Server integration **61/61** |
| **Final (T079)** | `npm run test:ci` | Frontend **56/56** |
| **Final (T079)** | `ng build` | Production build OK, no budget warnings |
| **Final (T079)** | Freshly seeded dev DB, scripted quickstart V1–V12 | 17/17 PASS |
| **Final (T079)** | V13: SignalR client reconnect across an API restart | Reconnected 2 s after restart |
| UI redesign 002 | `npm run test:ci` | 89/89 (baseline 56/56; status-filter spec rewritten for the switcher, other existing specs unchanged) |
| UI redesign 002 | `dotnet test PrescriptionRefill.slnx` | Domain 47/47; integration 61/61 (backend unchanged) |
| UI redesign 002 | `npm run build` | OK; initial 413.32 kB (baseline 350.94 kB) |
| UI redesign 002 | DevTools-protocol screenshots at 360/768/1024/1280, forced colors, reduced motion | No horizontal overflow on any page |
| Final documentation pass | `dotnet build PrescriptionRefill.slnx`, `dotnet test PrescriptionRefill.slnx`, `npm run test:ci`, `npm run build` | 0 warnings; Domain 47/47, integration 61/61; frontend 89/89; build OK (initial 413.32 kB) |
| Final documentation pass | Fresh copy without build output or `node_modules`, new empty database via `ConnectionStrings__RefillDb`: README `--seed` (twice), `dotnet run`, `npm ci`, `npm run test:ci`, `npm run build`, `npm start` | Seeded once, then skipped; 6 Pending, 1 Approved, 1 Denied; README `curl` 201, duplicate 409 `pending_request_exists`, too-early approve 422 `approval_blocked`, blank note 400, eligible approve 200 (refills 3 → 2), repeat 409; frontend 89/89; `/api` and `/hubs` negotiate 200 through the dev proxy. No browser session or SignalR reconnect test in this pass |

| Creative Clinical UI 004 | `npm run test:ci`, `npm run build`, `dotnet build`, `dotnet test` | Frontend 117/117 (baseline 98); build OK, initial 424.01 kB; backend 0 warnings, 47/47 and 103/103 |
| Creative Clinical UI 004 | Fresh copy without build output, new throwaway database, README commands unchanged (ports free) | Seeded; 6/1/1; `npm ci`; 117/117; `npm start`; `/api` and `/hubs` 200 through the proxy |
| Creative Clinical UI 004 | Headless Edge over the DevTools protocol, quickstart V1–V12 ([results](specs/004-creative-clinical-ui/quickstart.md#results-t033t035-2026-10-20-all-from-actual-runs)) | All pass: no overflow; safety-first order; states labeled in grayscale; 200% zoom; light matches pointer before/after scrolling; 0 animations at rest; static under reduced motion and touch; focus visible; forced colors; 0 long tasks while typing and scrolling (light on and off); 44 px targets; 0 contrast failures. Not run: a manual human browser session |
| Bilingual localization and themes 005 | `dotnet test` (3 consecutive runs) | Domain 47/47; SQL Server integration 187/187 each run (baseline 103; 84 new, none removed) |
| Bilingual localization and themes 005 | `npm run test:ci`, `npm run build` | Frontend 159/159 (baseline 117; 42 new); build OK, initial 462.64 kB |
| Bilingual localization and themes 005 | Throwaway seeded database, API on 5180, dev server on 4300, headless Edge over the DevTools protocol ([results](specs/005-bilingual-localization-themes/quickstart.md#results)) | 73/73: Arabic/RTL/Light on first visit and first frame; 4 combinations × 5 screens × 360 px/1280 px/200% with no overflow; switches visible in ≤ 16 ms; preferences independent and persistent; corrupt or blocked storage safe; forced colors and print in Dark. Not run: axe audit, Narrator, Arabic-speaker review |
| Bilingual localization and themes 005 | Fresh copy without build output, `node_modules`, or `.git`; new throwaway database; README commands | `--seed` seeded, then skipped; API without a header answers `Content-Language: ar` and Arabic names, `Accept-Language: en` the English names; README `curl` 201, duplicate 409 `pending_request_exists` worded in Arabic; `npm ci`, `npm run test:ci` 158/158, `npm run build` OK. Not run in this pass: `npm start` with a browser session |

Quickstart coverage: V1–V12 and V13 were run as above; V14 (API stopped → error state and
"live updates unavailable") and V15–V17 (time display, notice, 360px) are covered by component
specs and the T074 audit and screenshots rather than a separate manual pass. SC-003 (decision
under 1 minute) and SC-007 (other screen within 2 seconds) were not formally timed; the live
hint was observed arriving during the T073 and T079 checks, but its latency was not measured.
