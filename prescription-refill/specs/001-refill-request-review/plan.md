# Implementation Plan: Prescription Refill Request Review

**Branch**: `001-refill-request-review` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-refill-request-review/spec.md`

## Summary

A clinician works a queue of Pending refill requests, opens one to see the patient profile,
allergies, active prescriptions, and the server's time-stamped evaluation, and approves or
denies it with a note. The backend is an ASP.NET Core modular monolith on SQL Server: Domain
evaluates rules with an injected clock, Application runs one transaction per command, and
`rowversion` tokens on the request and prescription plus a filtered unique index provide
concurrency safety. After each commit, an in-process SignalR hub broadcasts a minimal
`RequestUpdated` hint so other open screens reload from the API. Angular presents the queue and
review screens. Detail: [research.md](research.md), [data-model.md](data-model.md),
[contracts/http-api.md](contracts/http-api.md), [quickstart.md](quickstart.md).

## Technical Context

Versions below were verified on the development machine on 2026-10-20 (research R0).

**Language/Version**: C# targeting `net10.0`, built with the installed default SDK
11.0.100-rc.2.26504.105 (preview) and run on the installed .NET 10.0.13 runtime; no SDK
changes. TypeScript 5.6.3 with Angular 19.2.x (installed CLI 19.0.0); Node 22.18.0.

**Primary Dependencies**: ASP.NET Core minimal APIs and SignalR (shared framework 10.0.13);
`Microsoft.EntityFrameworkCore.SqlServer` 10.0.13 (verified restore); `System.TimeProvider`;
Angular Router and `HttpClient`; `@microsoft/signalr` 10.0.11 (verified install).

**Storage**: Existing local SQL Server 2022 default instance (16.0.1000.6, Windows
authentication). Connection string (no secret):
`Server=.;Database=PrescriptionRefill;Trusted_Connection=True;TrustServerCertificate=True`.
Docker or LocalDB can be used instead by changing the connection string.

**Testing**: xUnit, `Microsoft.Extensions.TimeProvider.Testing`,
`Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.SignalR.Client`; integration tests on
SQL Server via `REFILL_TEST_SQLSERVER` (research R7). Frontend: Karma + Jasmine (Angular 19
default) in headless Edge via `CHROME_BIN`.

**Resolved at implementation (T002, T003, T005)**: `Directory.Packages.props` pins EF Core,
SqlServer, Design, Logging.Abstractions, Mvc.Testing, and SignalR.Client at 10.0.13;
TimeProvider.Testing 10.11.0; xunit 2.9.3, xunit.runner.visualstudio 3.1.4,
Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4; transitive pin
`System.Security.Cryptography.Pkcs` 10.0.13 (replaces vulnerable 9.0.11 from
Microsoft.Data.SqlClient 6.1.7). Frontend (`frontend/package-lock.json`): @angular/core 19.2.25,
@angular/cli and build-angular 19.2.27, TypeScript 5.6.3, Karma 6.4.4, Jasmine 5.4.0,
@microsoft/signalr 10.0.11.

**Target Platform**: Local development on Windows (verified); other platforms need their own
SQL Server and a .NET 10-capable SDK.

**Project Type**: Web application (REST API + SignalR hub + SPA).

**Performance Goals**: Demo scale; SC-003 (decision under 1 minute) and SC-007 (other screen
reflects a decision within 2 seconds).

**Constraints**: Two-day timebox; no distributed infrastructure, cache, outbox, or polling; no
EF Core InMemory or SQLite; no authentication (FR-020); no upgrades of SDKs, Angular, or SQL
Server.

**Scale/Scope**: 6 patients, 11 synthetic medications, ~10 prescriptions, 8 seeded requests,
4 synthetic interaction pairs; 2 screens (queue, review); no request-creation screen.

**Environment constraints to note**: both SDKs are previews; Angular 19 is past its support
window; Docker engine not running; transitive NuGet advisory to fix during implementation
(R16). None blocks the plan.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Pre-research | Post-design |
|-----------|------|--------------|-------------|
| I Architecture | 4 layers, inward dependencies; rules only in Domain; no generic repository, mediator, microservice, or speculative abstraction | Pass | Pass — SignalR runs in the same host; `IRequestNotifier` has a consumer (API) and a test need (R13) |
| II Deterministic rules | Server-only evaluation; injected clock; frontend shows server evaluation | Pass | Pass — `TimeProvider`; integer-tick formula; one evaluator for reads and decisions |
| III Workflow | Pending-only transitions; trimmed non-empty note; re-evaluation at processing; distinct errors | Pass | Pass — contract error table, precedence, and client handling |
| IV Persistence | Explicit configurations; migrations; SQL Server; `rowversion` on request and prescription; one transaction; no auto-retry; proportional | Pass | Pass — data-model "Transactions and Concurrency"; no cache (R14) |
| V Tests | Unit + SQL Server integration incl. thresholds, invalid input, repeats, concurrency; never InMemory/SQLite; report not-run honestly | Pass (planned) | Pass (planned) — local SQL Server instance per R7; nothing written or run |
| VI Security | Fictional data; server validation; DTOs; identifier-only logs; limitations documented | Pass | Pass — event payload has IDs only; unauthenticated hub documented as not production-secure |
| VII Frontend | Feature folders; API and realtime access in services; loading/empty/error + warning/blocked/success states; 360/1280px | Pass | Pass — structure below; quickstart V11–V12 |
| Delivery | 2-day scope; SPEC.md, PROMPTS.md, README.md current; clean-env check | Pass | Pass — PROMPTS.md created in slice 6 |

No violations; Complexity Tracking is empty. No constitution amendment is needed: SignalR,
caching, and the test-database choice are design decisions within existing principles.

**Post-implementation check (T080, 2026-10-20)** — against the delivered code:

| Principle | Evidence | Result |
|-----------|----------|--------|
| I | Domain project has no package references; rules in `Domain/Evaluation` and `RefillRequest`; abstractions limited to `IRefillDbContext` and `IRequestNotifier` (both used); no repository or mediator found by search | Pass |
| II | No `DateTime(Offset).(Utc)Now` in src; `TimeProvider` injected; frontend contains no rule logic (note rule removed from the client per analysis finding C1) | Pass |
| III | Pending-only transitions and note rule in Domain; error codes per contract, covered by integration tests | Pass |
| IV | Explicit configurations, `InitialCreate` migration, `rowversion` on both tables, one `SaveChanges` per command, no retry | Pass |
| V | 47 Domain + 61 SQL Server integration + 56 frontend tests, all run; concurrency conflicts deterministic via a SaveChanges interceptor; the parallel test is labeled supplementary | Pass |
| VI | DTO-only responses (tests assert no `rowVersion`), identifier-only logs, EF Core logging at Warning, notices on every screen | Pass |
| VII | Feature folders; HTTP only in `data/`, SignalR only in `core/realtime/`; states covered by specs; 360/1280px audit 8/8; spacing and type from shared variables | Pass |
| Delivery | README verified from a clean copy (T076); SPEC.md and PROMPTS.md current | Pass |

Recorded deviations from the plan, none of which is a principle violation: integration tests use
the local SQL Server instance instead of Testcontainers (research R7; no Docker engine);
`RefillDecisionContextLoader` narrows the interaction query to pairs involving the requested
medication (data access only; matching stays in Domain); decided seed scenarios are created
through Domain `Approve`/`Deny` (tasks.md T040).

## Decided Assumptions

Confirmed by the author on 2026-10-20. Rules live in the spec; this table maps each decision.

| # | Decision | Spec | Design |
|---|----------|------|--------|
| A1 | Create endpoint and tests kept; no request-creation screen | US2, FR-001 | contract `POST /api/refill-requests` |
| A2 | Inactive prescription → `400 validation_failed` (`errors.prescriptionId`) | FR-021 | contract; seed inactive prescription |
| A3 | Active medication = linked through a prescription with `IsActive = true`, used everywhere | FR-022 | data-model "Active medications" |
| A4 | Allergy match on recorded medication only; no class/ingredient inference | FR-008 | data-model Allergy |
| A5 | Invented medications, synthetic pairs, visible "not clinical guidance" notice | FR-019 | research R11 |
| A6 | Approve disabled when the latest displayed evaluation blocks; Deny available; server re-evaluates and returns `422 approval_blocked` | FR-011, FR-023 | contract client handling |
| A7 | Approved/Denied read-only via status filter, newest decision first | US3, FR-018 | contract list |
| A8 | Local time with UTC offset, UTC on hover/details; date of birth unshifted | FR-024 | research R12 |
| A9 | No component library; native controls and shared SCSS variables | — | research R10 |
| A10 | No cooldown rule; 80% rule on `LastFilledAt`; one refill per approval; dispensing not modeled; repeat approvals documented | FR-012, FR-025, FR-026 | Concurrency Design |

**Decisions added on 2026-10-20 (second refinement)**

| # | Decision | Spec | Design |
|---|----------|------|--------|
| D1 | Evaluation lifecycle: on open, explicit Refresh, from `422`, on live hint/reconnect/return; no polling; `evaluatedAt` shown with a staleness notice; Approve state is UX only | FR-005, FR-011, FR-023 | Evaluation Lifecycle below |
| D2 | Exact 80% rule: elapsed UTC duration strictly less than `DaysSupply × 0.8 days` blocks; equality does not | FR-006 | data-model "Evaluation" |
| D3 | Invalid timing data (future/missing `LastFilledAt`, non-positive `DaysSupply`) → `INVALID_PRESCRIPTION_DATA`, approval blocked | FR-027 | contract evaluation object |
| D4 | Approve = one transaction over request + prescription, both `rowversion`-checked; full rollback before `409 concurrency_conflict`; no retry | FR-013, FR-016 | Concurrency Design |
| D5 | Client on `409`: no success, reload request/prescription/evaluation, explain, keep note | FR-032 | contract client handling |
| D6 | SignalR broadcast `RequestUpdated { requestId, change }` after commit only; hint, not state; reload on reconnect/return; best effort, no outbox | FR-029–FR-031 | Live Updates below; research R13, R15 |
| D7 | No `IMemoryCache` or distributed cache for status, refills, or eligibility | FR-028 | research R14 |
| D8 | Use the installed toolchain and the running local SQL Server; tests use `REFILL_TEST_SQLSERVER` with temporary databases | — | research R0, R1, R7–R9 |

**Other resolved points**: evaluation refresh never polls; notes stored trimmed; a second active
prescription for the same medication is not an interaction; `IsActive` is seed-only; patient
profile shows name and date of birth only; lists are not paginated.

## Project Structure

### Documentation (this feature)

```text
specs/001-refill-request-review/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   └── http-api.md      # Phase 1 (HTTP + live updates)
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks (not yet created)
```

### Source Code (repository root)

```text
src/
├── PrescriptionRefill.Domain/            # entities, RefillEvaluator, transitions, results
├── PrescriptionRefill.Application/       # use cases, DTOs, IRequestNotifier port
├── PrescriptionRefill.Infrastructure/    # DbContext, configurations, UTC converter,
│                                         # migrations, seeder, exception translation
└── PrescriptionRefill.Api/               # endpoint groups, ProblemDetails mapping,
                                          # RefillRequestsHub + SignalR notifier, --seed
tests/
├── PrescriptionRefill.Domain.Tests/      # unit tests, FakeTimeProvider
└── PrescriptionRefill.IntegrationTests/  # API + SQL Server + hub
frontend/
└── src/app/
    ├── core/realtime/                    # refill-events.service.ts (SignalR connection)
    ├── shared/ui/                        # status badge, alert, empty/error state, notice
    ├── shared/time/                      # shared instant/date formatters
    └── features/refill-requests/
        ├── data/                         # refill-requests.api.ts, models.ts
        ├── queue/                        # queue page
        └── review/                       # review page, patient panel, evaluation panel,
                                          # decision form
```

**Structure Decision**: Four backend projects map to Principle I layers; the hub lives in the
API project because it is a delivery mechanism. Two test projects separate fast unit tests from
SQL Server tests. In the frontend, only `data/` calls HTTP and only `core/realtime/` talks to
SignalR.

## Evaluation Lifecycle (FR-005, FR-011, FR-023, FR-032)

```text
open review ──GET detail──▶ show evaluation (evaluatedAt + "may be out of date")
   ▲                              │
   │ Refresh / RequestUpdated     │ Approve (enabled only if canApprove)
   │ (same id) / reconnect /      ▼
   │ screen active again   POST approve ──▶ server re-evaluates at processing time
   │                              │
   │        ┌─────────────────────┼──────────────────────┬────────────────────┐
   │      200                   422                     409                400/404/500
   │   decided view      replace evaluation      no success; message;   inline / error
   │                     from response;          reload detail; keep    state
   └──────────────────── Deny still available ── note ─────────────────────────┘
```

Approve's disabled state never substitutes for the server check; a stale enabled button still
ends in `422` if a blocking rule applies.

## Concurrency Design (FR-013, FR-016, FR-025)

**Approve** (one `DbContext`, one `SaveChanges`, one transaction):

1. Load the request with its prescription, the patient's allergies, other active
   prescriptions, and matching interactions (tracked). Original `RowVersion` values are
   captured for both rows.
2. `now = TimeProvider.GetUtcNow()`. Domain: not Pending → already decided; evaluation blocked
   → result with evaluation, no state change; otherwise status → Approved and
   `prescription.ConsumeRefill()`.
3. `SaveChanges` issues both UPDATEs in one transaction, each with
   `WHERE Id = @id AND RowVersion = @original`. If either affects 0 rows, EF Core throws
   `DbUpdateConcurrencyException` after rolling the transaction back; the API returns
   `409 concurrency_conflict`. Nothing is published.
4. After a successful commit, call `IRequestNotifier.RequestUpdated(id, "decided")`.

**Deny**: same steps; only the request is updated (refills unchanged).

**Create**: insert; the filtered unique index enforces FR-003; SQL errors 2601/2627 on it
become `409 pending_request_exists`. On success, notify `created`.

**Not used**: `EnableRetryOnFailure`, lock hints, `ExecuteUpdate`, caching.

**Why the request token alone is insufficient**: it detects a second decision on the same
request but not a change to the prescription row; the prescription token makes the approval fail
if another transaction changed refill state after it was loaded.

## Live Updates (FR-029–FR-031)

- Server: `RefillRequestsHub` mapped at `/hubs/refill-requests`, no client-callable methods.
  `SignalRRequestNotifier` (API) implements `IRequestNotifier` via
  `IHubContext.Clients.All.SendAsync("RequestUpdated", { requestId, change })`. Called only
  after `SaveChanges` returns; exceptions are caught and logged with the request ID only.
- Client: `RefillEventsService` builds one connection with automatic reconnect and exposes
  `requestUpdated$` and `connectionState`. Queue page reloads on any event; review page reloads
  when `requestId` matches. Both reload on `onreconnected` and when the route is re-entered or
  the tab becomes visible. A small indicator shows "Live updates unavailable — use Refresh"
  while disconnected.
- Failure mode accepted: commit succeeds but the notification is lost (process crash, network).
  The decision stands; other screens catch up on reconnect, return, or Refresh. No outbox (R15).
- Dev setup: Angular dev-server proxies `/api` and `/hubs` (with `ws: true`) to the API, so no
  CORS configuration is needed.

## Test Plan

Nothing below has been written or run.

### Domain unit tests (no database)

| Area | Cases |
|------|-------|
| Threshold (FR-006) | DaysSupply 1, 30, 90 at `eligibleAt − 1 tick` (TOO_EARLY, `eligibleAt` set), `= eligibleAt` (not blocked), `+ 1 tick` (not blocked) |
| UTC (FR-006) | the same instant expressed with offsets +00:00, +03:00, −05:00 for `now` and `LastFilledAt` → identical result |
| Invalid data (FR-027) | `LastFilledAt` 1 tick after `now` → INVALID_PRESCRIPTION_DATA, no TOO_EARLY, `eligibleAt` null, `canApprove` false; DaysSupply 0 and −1 → same; denial still allowed |
| Refills (FR-007, FR-025) | 0 → NO_REFILLS_REMAINING; 1 → approve leaves 0; `ConsumeRefill` at 0 rejected |
| Combined | both blockers reported in the documented order; blockers and warnings all reported |
| Warnings (FR-008, FR-022) | allergy match; allergy to another medication → none; interaction in either pair order; interacting medication only on an inactive prescription → none; same medication on another active prescription → none; warnings never block |
| Transitions (FR-010, FR-014, FR-015) | approve/deny from Pending; deny while blocked; any decision after decided → already decided, no field changes |
| Notes (FR-010) | null, empty, whitespace → rejected; 1,000 chars accepted; 1,001 rejected; stored trimmed |
| Fill date (FR-012) | approve and deny leave LastFilledAt unchanged |

### SQL Server integration tests (migrations applied; `REFILL_TEST_SQLSERVER`)

| Area | Cases |
|------|-------|
| Schema | migrations apply to an empty database; CHECKs (`DaysSupply > 0`, `RemainingRefills >= 0`), NOT NULL `LastFilledAt`, and the filtered index exist; direct UPDATE to RemainingRefills −1 or DaysSupply 0 is rejected |
| Time storage | `LastFilledAt` saved with a +03:00 offset is read back as the same instant at +00:00; tick precision round-trips |
| Serialization | API instants end in `+00:00`; `dateOfBirth` is `yyyy-MM-dd` |
| Same calculation | at one fake time, detail `evaluation` and approve outcome agree (eligible and blocked cases) |
| Queries (FR-018, FR-022) | Pending oldest first; decided newest decision first; list `evaluatedAt`; detail has profile, allergies, active prescriptions only, evaluation only while Pending; 404 |
| Create (FR-001–004, FR-021) | 201; 404; inactive → 400 with `errors.prescriptionId`; blocked prescription → 201; second Pending → 409 `pending_request_exists`; after decision → 201 |
| Simultaneous create | two contexts insert Pending for one prescription → exactly one commits |
| Approve success (FR-012, FR-013) | 200; status Approved; refills −1; LastFilledAt unchanged; both RowVersions changed |
| Deny (FR-012, FR-014) | 200 on a blocked request; refills and prescription RowVersion unchanged |
| Blocked (FR-011) | fake clock 1 tick early → 422 with full evaluation matching the contract; zero refills → 422; both rows unchanged including RowVersion |
| Stale view (FR-011) | detail shows `canApprove = true`; test sets refills to 0 directly; approve → 422 with current reasons |
| Repeat (FR-015) | approve→approve, approve→deny → 409 `request_already_decided`; refills decremented once |
| Request-row conflict (FR-013) | context A loads; context B changes only the request row and commits; A approves → 409 `concurrency_conflict`; prescription unchanged; request shows only B's change |
| Prescription-row conflict (FR-013) | context A loads; context B changes only the prescription row and commits; A approves → 409; request still Pending; refills equal B's value |
| Over-consumption (FR-025) | 1 refill: 5 simultaneous approve calls on one request → exactly one 200, others 409; refills 0, never negative (assertions hold for any interleaving); sequential: approve A → 0, create B, approve B → 422 NO_REFILLS_REMAINING |
| Create vs approve race | approve A while creating B → at most one Pending, refill consumed once |
| Contract | no `rowVersion` or entity-only fields; ProblemDetails `code`, `requestId`, `requestStatus`, `evaluation` as documented |

### Notification tests (FR-029–FR-031)

| Area | Cases |
|------|-------|
| After commit only | recording `IRequestNotifier` reads the database in a new context when called and sees the committed state; one call per successful approve, deny, create |
| No event on failure | 400, 404, 409 (each code), 422, and forced concurrency rollback → zero calls |
| Delivery failure | notifier throws → HTTP 200 and the decision is committed |
| Hub smoke | a real SignalR client connected to the in-process test server receives `RequestUpdated` with exactly `requestId` and `change` |

### Frontend unit tests (Karma + Jasmine)

| Area | Cases |
|------|-------|
| API service | maps DTOs; surfaces ProblemDetails `code` |
| Queue | loading, empty, error (retry), populated; reloads on any `RequestUpdated` and on reconnect |
| Review lifecycle | loads on open; Refresh re-fetches; `evaluatedAt` and staleness notice shown; Approve disabled and Deny enabled when `canApprove` is false; reloads on matching event only; reloads on reconnect and re-activation; no requests fire when time passes with no trigger (no polling) |
| Decision outcomes | 200 → decided view; 422 → evaluation replaced from response, Deny available; 409 conflict → no success, message, detail reloaded, note kept; 409 already decided → read-only; 400 inline |
| Realtime service | indicator shown while disconnected; events are not used as state |
| Time formatting (FR-024) | local offset in the shared format; `title` holds ISO UTC; date of birth not shifted |
| Demo notice (FR-019) | rendered in the app shell |

If SQL Server is unavailable, the integration suite is reported as **not run**.

## Delivery Slices

Implemented one slice at a time; each ends with its tests run and results reported as executed.

1. Domain: entities, evaluator (incl. invalid data), transitions + unit tests.
2. Infrastructure: DbContext, configurations, UTC converter, `InitialCreate`, seeder + schema
   and time-storage tests; resolve the R16 advisory.
3. Application + API: queries, approve, deny, create, error mapping, notifier port, hub +
   integration, concurrency, and notification tests.
4. Frontend: queue and review screens, API and realtime services, states + frontend tests.
5. Accessibility and responsive pass (360px / 1280px, keyboard).
6. Docs: README verified from a clean clone, SPEC.md, PROMPTS.md.

## Complexity Tracking

No violations.
