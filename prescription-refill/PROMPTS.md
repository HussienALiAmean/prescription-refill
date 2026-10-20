# Prompts Log

Prompts that produced kept code, tests, or design decisions, with their outcomes (constitution,
Development Workflow & AI-Assisted Development). Prompts are summarized; outcomes link to the
artifacts they changed. AI = Claude Code (Opus 5.5) using Spec Kit commands.

## Planning (2026-10-19 – 2026-10-20)

| # | Prompt (summary) | Outcome | Kept / rejected |
|---|------------------|---------|-----------------|
| P1 | `/speckit-constitution` with no input | AI inferred a generic healthcare constitution (v1.0.0) | Replaced by P2 |
| P2 | Ten engineering priorities (architecture, deterministic rules, workflow integrity, EF Core, testing, security, Angular, AI use, delivery); "concise, enforceable, identify unresolved decisions" | Constitution v2.0.0 with U1–U8 open decisions | Kept |
| P3 | "Review the constitution for missing, ambiguous, contradictory, or unenforceable principles" | Review found C1 (client-side rule checks), G1–G5, E1–E4 | Kept |
| P4 | Apply the fixes as v2.1.0 with qualifications on G1 (refill-count concurrency) and G2 (Domain owns rules) | Constitution v2.1.0 | Kept |
| P5 | Use SQL Server, not SQLite; rowversion on request and prescription; SQL Server tests | Constitution v2.2.0 (U1 resolved); README database setup | Kept |
| P6 | Resolve U2–U8 (80% rule inputs, decision-time evaluation, warnings, no auth, one refill per approval, one Pending per prescription, document roles) | Constitution v3.0.0; spec, plan, SPEC.md created; AI flagged that U7 makes a "two Pending requests compete" test impossible and proposed equivalent tests | Kept |
| P7 | `/speckit-plan` with the full challenge brief | research, data model, contract, quickstart; A1–A10 proposed assumptions | Kept |
| P8 | Decisions on A1–A10 (API-only creation, inactive rejection, active medication, exact allergy match, synthetic data, disabled Approve, read-only history, local time, no UI kit, no cooldown rule) | Spec FR-021–FR-026; constitution v3.0.1 (U9) | Kept; AI's proposal of real drug names **rejected** by the author (A5) |
| P9 | Refinement: evaluation lifecycle, exact 80% rule, atomic approval, SignalR, no cache, use installed environment | Spec FR-027–FR-032, US4; contract with full error examples; environment verified (research R0); Testcontainers replaced by the running local SQL Server | Kept |
| P10 | `/speckit-clarify` | No critical ambiguities; one wording fix in FR-023 | Kept |
| P11 | `/speckit-tasks`, `/speckit-analyze` | 80 tasks; analysis found 2 constitution conflicts in tasks (client-side note rule, active-medication filter in Application) and a seeding order problem | Fixed in tasks.md before implementation |

## Implementation

| # | Prompt (summary) | Outcome | Kept / rejected |
|---|------------------|---------|-----------------|
| I1 | `/speckit-implement` (first run) | Before coding, AI applied the analysis fixes to tasks.md: no client-side note rule, active-medication rule moved to Domain, seeding order, empty `AddApplication()` | Kept |
| I2 | Setup (T001–T007) | Solution, central package versions, Angular 19 workspace; AI traced the NU1903 advisory to Microsoft.Data.SqlClient → Pkcs 9.0.11 and pinned 10.0.13 transitively | Kept |
| I3 | Foundation (T008–T029) | Entities, EF configurations, migration, seeder, SQL Server test fixture | Kept |
| I4 | US1 tests then code (T030–T052) | 44 unit, 25 integration, 27 frontend specs written first and observed failing; implementation until green | Kept |
| I5 | US1 concurrency test design | AI proposed a SaveChanges interceptor that commits a competing change between load and save, making conflicts deterministic | Kept |
| I6 | US1 review page, Approve/Deny | AI's first task wording had the client validate the note "as a convenience"; **rejected** because it duplicates the server rule (constitution Principle I) | Rejected |
| I7 | US2–US4 (T053–T073) | API creation, status filter, SignalR hints; real hub tests and a proxy-level live-update check | Kept |
| I8 | Accessibility audit (T074) | First screenshots looked dark; AI traced it to a Dark Reader browser extension in the test profile, not the app, and re-ran with extensions disabled | Kept (added `color-scheme: only light`) |
| I9 | Clean-environment check (T076) | AI found integration tests failing from a deep folder (Windows 260-character path limit for the SQL Server client DLL) and documented it rather than changing code | Kept |
| I10 | Final verification (T079) | Full test runs and quickstart scripts; results recorded in SPEC.md | Kept |

## UI redesign (feature 002, 2026-10-20)

| # | Prompt (summary) | Outcome | Kept / rejected |
|---|------------------|---------|-----------------|
| D1 | Audit and plan a premium UI/UX redesign; Spec Kit spec, plan, tasks; no code before approval | 15-item audit with file locations (research R0); spec/plan/tasks for 002; found that an always-visible "Live" indicator would break a tested behavior and resolved it in favor of the test | Kept |
| D2 | Approval with refinements: order (foundation → queue → review), segmented switcher, exact counts only, stronger visual identity, real visual verification | Plan and tasks revised; status-filter spec rewritten with equivalent assertions; counts only for the loaded status | Kept |
| D3 | Pagination for collections | API returns full lists ("Not paginated"), so client-side pagination with exact totals; server paging documented as a separate proposed change | Kept |
| D4 | Visual verification | Headless Edge `--window-size` could not go below a 496px viewport, so 360px screenshots were wrong; AI replaced them with a DevTools-protocol script that sets exact widths and reports overflow | Kept (script outside the repo) |
| D5 | Refinement pass from screenshots | Fixed: implicit grid column causing 459px overflow on the decided review at 360px; underlined ghost link; count/label run together for screen readers ("1approval blocked", caught by a new test); status badge placement; allergy chips moved from danger to warning palette; prescription facts column widths; compact mobile summary | Kept |

## Server-side pagination (feature 003, 2026-10-20)

| # | Prompt (summary) | Outcome | Kept / rejected |
|---|------------------|---------|-----------------|
| B1 | `/speckit-specify` "we need the pagination to be in the backend" | Spec 003: AI pointed out that once only one page reaches the screen, client-side search would miss matches, so search and summary counts move with paging; unpaged requests stay exactly as in 001 | Kept |
| B2 | `/speckit-plan` | Research R1–R10: opt-in paging (existing tests prove the unpaged contract), Pending evaluated once then paged by id in SQL, no index or migration | Kept; snapshot isolation for the two Pending reads **rejected** as disproportionate (a brief mismatch self-corrects through the live hint) |
| B3 | `/speckit-tasks`, `/speckit-analyze` | 38 tasks; analysis found the clean-environment check missing from final verification (constitution), a FR-002 wording conflict with "search alone is paged", and two wrong task cross-references | Fixed before implementation |
| B4 | `/speckit-implement` | Validation, paging, search, summary, and log-privacy tests against SQL Server; queue loads one page at a time through `switchMap`, search debounced | Kept |
| B5 | Search collation | Planned `EF.Functions.Collate` needs the EF Core relational package in Application (a new dependency); replaced by lower-casing both sides, the alternative already listed in research R4 | Changed |

## Creative Clinical UI (feature 004, 2026-10-20)

| # | Prompt (summary) | Outcome | Kept / rejected |
|---|------------------|---------|-----------------|
| C1 | `/speckit-specify` with the "Creative Clinical / Ambient Atelier" brief | Spec 004: safety-first review, denser queue, pointer light, presentation only; one clarification (counts for other statuses) | Kept |
| C2 | `/speckit-plan` without answering the clarification | Recommended default adopted and recorded as a default, not a user decision (selected status count only) | Kept, flagged |
| C3 | Palette check | Five of the seven brief colors fail AA as small text (teal 4.23, indigo 4.47, danger 3.76, success 2.28, warning 2.15) | Brief colors **rejected** as text; used only for light and glow |
| C4 | Layout options | Left navigation rail with one destination **rejected** as fake chrome; dark canvas + light sheet kept; green "no issues" state **rejected** as reassuring | Kept / rejected as noted |
| C5 | `/speckit-analyze` (three rounds) | Found the canvas light would drift from the pointer after scrolling (host vs. viewport coordinates), a missing clean-copy check, missing 44 px coverage, raw sizes, and an untested performance edge case | Fixed in the spec before implementation |
| C6 | `/speckit-implement` | Directive with 11 specs (including one proving pointer moves run outside Angular's zone), tokens, shell, queue, review; real-browser checks V1–V12 from a clean copy | Kept |
| C7 | Visual iteration | Screenshot review found the queue's "No issues found" badge still green; changed to neutral. Check-script defects (touch parameter, assertions before data rendered, throttling hiding the skeleton) fixed and all checks re-run | Kept |

## Review notes

- Every AI-generated change was built and tested before it was kept; test results are recorded
  only from actual runs (SPEC.md "Verification").
- Suggestions that would add complexity without a requirement (outbox, cache, Testcontainers when
  no Docker engine was running, real drug names) were rejected and are recorded in research.md.
