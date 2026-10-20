---

description: "Task list for Prescription Refill Request Review"
---

# Tasks: Prescription Refill Request Review

**Input**: Design documents from `/specs/001-refill-request-review/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-api.md,
quickstart.md

**Tests**: Required. The constitution (Principles III and V) mandates test-first development,
unit tests for every Domain rule, and SQL Server integration tests; the spec's success criteria
and plan.md "Test Plan" define the cases. Within each story, write the tests first and observe
them fail. If a test needs a type that does not exist yet, add only its public signature
(throwing `NotImplementedException`) so the test compiles and fails.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested
on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: User story from spec.md (US1â€“US4)
- Paths follow plan.md "Project Structure": backend in `src/` and `tests/`, frontend in
  `frontend/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution, projects, toolchain, and frontend workspace using the verified
environment (research R0) without upgrading anything.

- [X] T001 Create `PrescriptionRefill.slnx` and projects targeting `net10.0` with the installed default SDK (no `global.json`): `src/PrescriptionRefill.Domain/` (classlib), `src/PrescriptionRefill.Application/` (classlib), `src/PrescriptionRefill.Infrastructure/` (classlib), `src/PrescriptionRefill.Api/` (web), `tests/PrescriptionRefill.Domain.Tests/` (xunit), `tests/PrescriptionRefill.IntegrationTests/` (xunit); references Applicationâ†’Domain, Infrastructureâ†’Application+Domain, Apiâ†’Application+Infrastructure, Domain.Testsâ†’Domain, IntegrationTestsâ†’Api+Infrastructure; Domain references no packages
- [X] T002 Add `Directory.Build.props` (Nullable and ImplicitUsings enabled) and `Directory.Packages.props` (central versions: `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` 10.0.13, `Microsoft.EntityFrameworkCore` for Application, `Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.SignalR.Client`, xunit, test SDK) at the repository root; record the exact resolved versions in `specs/001-refill-request-review/plan.md` Technical Context
- [X] T003 Trace the NU1902/NU1903 advisory for `System.Security.Cryptography.Pkcs` 9.0.11 with `dotnet nuget why` (research R16), pin the smallest patched version in `Directory.Packages.props`, and confirm `dotnet restore` reports no NU190x warnings
- [X] T004 [P] Create `.gitignore` (bin, obj, node_modules, dist, .angular, *.user) and `.editorconfig` at the repository root
- [X] T005 [P] Scaffold the Angular app in `frontend/` with the installed global CLI 19.0.0 (`ng new` with SCSS, routing, no SSR, skip git), add `@microsoft/signalr`, and record the resolved Angular, TypeScript, Karma, and SignalR versions from `frontend/package-lock.json` in `specs/001-refill-request-review/plan.md`
- [X] T006 [P] Add `frontend/proxy.conf.json` proxying `/api` and `/hubs` (`ws: true`) to the API development URL, reference it from the serve target in `frontend/angular.json`, and configure the test target for `ChromeHeadless` driven by `CHROME_BIN` (Edge) in `frontend/angular.json`
- [X] T007 [P] Create `PROMPTS.md` at the repository root recording the planning prompts and outcomes so far (constitution, spec, plan, clarifications), per the constitution's Development Workflow

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, schema, migration, seed data, API host, test fixtures, and the frontend
shell that every story needs.

**âڑ ï¸ڈ CRITICAL**: No user story work can begin until this phase is complete.

### Domain entities (state only; behavior is added by the stories)

- [X] T008 [P] Create `Patient` (FullName, `DateOnly` DateOfBirth) and `Allergy` (PatientId, MedicationId) in `src/PrescriptionRefill.Domain/Patients/Patient.cs` and `src/PrescriptionRefill.Domain/Patients/Allergy.cs`
- [X] T009 [P] Create `Medication` and `DrugInteraction` (constructor stores the pair in ascending order and rejects a medication paired with itself) in `src/PrescriptionRefill.Domain/Medications/Medication.cs` and `src/PrescriptionRefill.Domain/Medications/DrugInteraction.cs`
- [X] T010 [P] Create `Prescription` (PatientId, MedicationId, Dosage, DaysSupply, RemainingRefills, `DateTimeOffset` LastFilledAt, IsActive; private setters) in `src/PrescriptionRefill.Domain/Prescriptions/Prescription.cs`
- [X] T011 [P] Create `RefillRequest` (PrescriptionId, Status, RequestedAt, DecidedAt, DecisionNote; private setters) with the single creation path `RefillRequest.Create(prescription, now)` returning a Pending request (US2 adds the inactive rule to this same factory), and `RefillRequestStatus` (Pending, Approved, Denied) in `src/PrescriptionRefill.Domain/RefillRequests/RefillRequest.cs` and `src/PrescriptionRefill.Domain/RefillRequests/RefillRequestStatus.cs`

### Persistence

- [X] T012 Create the UTC value converter that normalizes every `DateTimeOffset` to offset `+00:00` on save in `src/PrescriptionRefill.Infrastructure/Persistence/UtcDateTimeOffsetConverter.cs`
- [X] T013 Create one configuration class per entity in `src/PrescriptionRefill.Infrastructure/Persistence/Configurations/` per data-model.md: keys, required fields, lengths, `Restrict` foreign keys, `datetimeoffset(7)` with the UTC converter, `date` DateOfBirth, Status stored as `nvarchar(16)` string, CHECKs (`DaysSupply > 0`, `RemainingRefills >= 0`, `MedicationAId < MedicationBId`), unique indexes (Medication.Name, Allergy pair, interaction pair), filtered unique index `UX_RefillRequests_Pending` on PrescriptionId WHERE `Status = N'Pending'`, index (Status, RequestedAt), and shadow `RowVersion` (`IsRowVersion()`) on Prescription and RefillRequest
- [X] T014 Create `IRefillDbContext` (DbSets and `SaveChangesAsync`) in `src/PrescriptionRefill.Application/Abstractions/IRefillDbContext.cs`, implement it with `RefillDbContext` applying all configurations in `src/PrescriptionRefill.Infrastructure/Persistence/RefillDbContext.cs`, and register it from `ConnectionStrings:RefillDb` without `EnableRetryOnFailure` in `src/PrescriptionRefill.Infrastructure/DependencyInjection.cs`
- [X] T015 Create the API host in `src/PrescriptionRefill.Api/Program.cs`: register `TimeProvider.System`, Application services (create an initially empty `AddApplication()` in `src/PrescriptionRefill.Application/DependencyInjection.cs`) and Infrastructure services, ProblemDetails, camelCase JSON, and a global handler that returns `500 unexpected_error` with no internal details (`src/PrescriptionRefill.Api/Errors/UnexpectedErrorHandler.cs`); expose `public partial class Program`; set the development connection string `Server=.;Database=PrescriptionRefill;Trusted_Connection=True;TrustServerCertificate=True` in `src/PrescriptionRefill.Api/appsettings.Development.json`
- [X] T016 Generate the `InitialCreate` migration into `src/PrescriptionRefill.Infrastructure/Persistence/Migrations/` with `dotnet ef migrations add`, review `dotnet ef migrations script` output for the CHECKs, filtered index, and `rowversion` columns; if the global dotnet-ef 10.0.2 fails against EF Core 10.0.13, add `.config/dotnet-tools.json` pinned to the matching version (research R16)
- [X] T017 Create the idempotent `DemoDataSeeder` (skips when any patient exists; all times from `TimeProvider`) with the 11 synthetic medications, 4 synthetic interaction pairs, 6 patients, prescriptions, allergies, the inactive Corvantil prescription, and the 6 Pending requests (scenarios 1â€“6, created with `RefillRequest.Create`; the decided scenarios 7â€“8 are added in T040 once Domain decisions exist) from data-model.md "Seed Scenarios" in `src/PrescriptionRefill.Infrastructure/Seeding/DemoDataSeeder.cs`; handle `--seed` in `src/PrescriptionRefill.Api/Program.cs` by applying migrations, seeding, and exiting

### Test infrastructure

- [X] T018 [P] Create `FakeTimeProvider` helpers and entity builders for unit tests in `tests/PrescriptionRefill.Domain.Tests/TestSupport/Builders.cs`
- [X] T019 Create `SqlServerDatabaseFixture` in `tests/PrescriptionRefill.IntegrationTests/Infrastructure/SqlServerDatabaseFixture.cs`: read `REFILL_TEST_SQLSERVER` (default `Server=.;Trusted_Connection=True;TrustServerCertificate=True`), create `RefillTests_<guid>` by applying migrations (never `EnsureCreated`, InMemory, or SQLite), drop it on dispose, and fail with "SQL Server not available" when unreachable
- [X] T020 Create `RefillApiFactory` (`WebApplicationFactory<Program>` with `FakeTimeProvider` and the fixture's connection string) and `TestData` (arranges rows directly and opens extra `RefillDbContext` instances for concurrency tests) in `tests/PrescriptionRefill.IntegrationTests/Infrastructure/RefillApiFactory.cs` and `tests/PrescriptionRefill.IntegrationTests/Infrastructure/TestData.cs`
- [X] T021 [P] Write schema tests in `tests/PrescriptionRefill.IntegrationTests/Persistence/SchemaTests.cs`: migrations apply to an empty database; direct UPDATE of RemainingRefills to âˆ’1 and DaysSupply to 0 is rejected; NULL LastFilledAt is rejected; interaction pair order CHECK holds; `UX_RefillRequests_Pending` exists with its filter
- [X] T022 [P] Write time storage tests in `tests/PrescriptionRefill.IntegrationTests/Persistence/TimeStorageTests.cs`: a LastFilledAt saved with offset +03:00 reads back as the same instant at +00:00; one-tick precision round-trips
- [X] T023 [P] Write seeder tests in `tests/PrescriptionRefill.IntegrationTests/Persistence/SeederTests.cs`: expected counts per data-model.md; a second run adds nothing

### Frontend foundation

- [X] T024 [P] Create shared SCSS variables (spacing, type scale, colors, focus ring) in `frontend/src/styles/_variables.scss` and base styles in `frontend/src/styles.scss`; no component library
- [X] T025 [P] Write `frontend/src/app/shared/time/format-time.spec.ts` first, then implement `frontend/src/app/shared/time/format-time.ts`: instants as `d MMM yyyy, HH:mm (UTCآ±hh:mm)` in local time, ISO UTC for `title`, `yyyy-MM-dd` dates never shifted (FR-024)
- [X] T026 [P] Create shared standalone components (alert, empty state, error state with retry, status badge) with specs in `frontend/src/app/shared/ui/`
- [X] T027 [P] Write `frontend/src/app/shared/http/problem-details.spec.ts` first, then implement `frontend/src/app/shared/http/problem-details.ts` to read `code`, `errors`, `requestId`, `requestStatus`, and `evaluation` from ProblemDetails responses
- [X] T028 Create the app shell with a persistent notice "Fictional demo data â€” not clinical guidance. No sign-in: anyone with access can decide requests; not for real patient data." (FR-019, FR-020) and router outlet in `frontend/src/app/app.component.ts`, `frontend/src/app/app.component.html`, `frontend/src/app/app.routes.ts`, with `frontend/src/app/app.component.spec.ts` asserting the notice (FR-019)
- [X] T029 Run `dotnet build PrescriptionRefill.slnx`, `dotnet test tests/PrescriptionRefill.IntegrationTests` (schema, time, seeder), and `npm test` in `frontend/`; record the actual output, reporting anything not run as not run

**Checkpoint**: Database, seed data, host, fixtures, and shell ready; user stories can begin.

---

## Phase 3: User Story 1 - Review and decide a pending request (Priority: P1) ًںژ¯ MVP

**Goal**: A clinician opens a Pending request from the queue, sees the profile, allergies,
active prescriptions, and a time-stamped server evaluation, and approves or denies it with a
note; approvals are atomic and concurrency-safe.

**Independent Test**: With seeded data, approve scenario 1, deny scenario 3, see Approve
disabled on scenario 2, and get `422` when approving scenario 2 through the API (quickstart
V1â€“V7, V9â€“V11).

### Tests for User Story 1 (write first; they must fail)

- [X] T030 [P] [US1] Write evaluator unit tests in `tests/PrescriptionRefill.Domain.Tests/Evaluation/RefillEvaluatorTests.cs`: DaysSupply 1, 30, 90 at `eligibleAt âˆ’ 1 tick` (TOO_EARLY with eligibleAt), exactly `eligibleAt`, and `+ 1 tick` (not blocked); same instant with offsets +00:00, +03:00, âˆ’05:00; LastFilledAt one tick after now and DaysSupply 0/âˆ’1 â†’ INVALID_PRESCRIPTION_DATA only, `eligibleAt` null, `canApprove` false; zero refills â†’ NO_REFILLS_REMAINING; documented reason order; allergy match and non-match; interaction in either pair order; interacting medication only on an inactive prescription â†’ no warning; same medication on another active prescription â†’ no warning; warnings never block
- [X] T031 [P] [US1] Write decision unit tests in `tests/PrescriptionRefill.Domain.Tests/RefillRequests/RefillRequestDecisionTests.cs` and `tests/PrescriptionRefill.Domain.Tests/Prescriptions/PrescriptionTests.cs`: approve and deny from Pending; deny while blocked; any decision after Approved/Denied â†’ already decided with no field changes; notes null, empty, whitespace rejected, 1,000 chars accepted, 1,001 rejected, stored trimmed; LastFilledAt unchanged by both decisions; `ConsumeRefill` at 0 rejected and at 1 leaves 0
- [X] T032 [P] [US1] Write queue and detail API tests in `tests/PrescriptionRefill.IntegrationTests/Api/QueueAndDetailTests.cs`: Pending list oldest first with list-level `evaluatedAt`, `canApprove`, `hasWarnings`; detail with profile, allergies, active prescriptions only (requested one excluded), evaluation present; `404 not_found`; instants end in `+00:00`; `dateOfBirth` is `yyyy-MM-dd`; no `rowVersion` or entity-only fields
- [X] T033 [P] [US1] Write decision API tests in `tests/PrescriptionRefill.IntegrationTests/Api/DecisionEndpointTests.cs`: approve â†’ 200, Approved, refills âˆ’1, LastFilledAt unchanged, both RowVersions changed; deny on a blocked request â†’ 200, refills and prescription RowVersion unchanged; fake clock one tick early â†’ `422 approval_blocked` whose body matches the contract example shape; zero refills â†’ 422; both rows unchanged after 422; stale view (detail shows `canApprove = true`, test sets refills to 0 directly, approve â†’ 422 with current reasons); approveâ†’approve and approveâ†’deny â†’ `409 request_already_decided` with `requestStatus`; blank and oversized note â†’ `400 validation_failed`; detail evaluation and approve outcome agree at the same fake time
- [X] T034 [P] [US1] Write SQL Server concurrency tests in `tests/PrescriptionRefill.IntegrationTests/Concurrency/DecisionConcurrencyTests.cs`: request-row conflict (context B changes only the request and commits; A's approve â†’ `409 concurrency_conflict`; prescription unchanged); prescription-row conflict (B changes only the prescription; A's approve â†’ 409; request still Pending; refills equal B's value); a supplementary (non-authoritative, timing-independent assertions only) check that five simultaneous approve calls on one request with one refill give exactly one 200, others 409, refills 0 and never negative; sequential last refill â†’ second approval 422 NO_REFILLS_REMAINING
- [X] T035 [P] [US1] Write `frontend/src/app/features/refill-requests/data/refill-requests.api.spec.ts` (HttpTestingController): list, detail, approve, deny map to DTOs; error codes surfaced via the ProblemDetails helper
- [X] T036 [P] [US1] Write `frontend/src/app/features/refill-requests/queue/refill-queue.page.spec.ts`: loading, empty, error with retry, populated list with blocked and warning badges, link to review
- [X] T037 [P] [US1] Write `frontend/src/app/features/refill-requests/review/refill-review.page.spec.ts` and `frontend/src/app/features/refill-requests/review/decision-form.component.spec.ts`: loading state; 404 and network error states with retry; loads on open; Refresh re-fetches; `evaluatedAt` with "may be out of date" notice; Approve disabled and Deny enabled when `canApprove` is false; 200 â†’ decided read-only view; 422 â†’ evaluation replaced from the response and Deny still available; `409 concurrency_conflict` â†’ no success, explanation, detail reloaded, note kept; `409 request_already_decided` â†’ read-only; 400 â†’ inline messages; no HTTP calls when time passes with no trigger (no polling)

### Implementation for User Story 1

- [X] T038 [P] [US1] Create evaluation types (`RefillEvaluation`, `BlockingReason`, `Warning`, reason and warning code constants) in `src/PrescriptionRefill.Domain/Evaluation/RefillEvaluation.cs`
- [X] T039 [US1] Implement `ActiveMedications.For(requested, patientPrescriptions)` (the single FR-022 definition: the patient's other prescriptions with `IsActive = true`) in `src/PrescriptionRefill.Domain/Evaluation/ActiveMedications.cs` and `RefillEvaluator` in `src/PrescriptionRefill.Domain/Evaluation/RefillEvaluator.cs` using integer ticks per data-model.md "Evaluation" (threshold `DaysSupply أ— 691,200,000,000`, strict `<`, invalid-data branch, reason order, warnings from allergies, active medications, and interactions) until T030 passes
- [X] T040 [US1] Implement `Prescription.ConsumeRefill()` in `src/PrescriptionRefill.Domain/Prescriptions/Prescription.cs` and `RefillRequest.Approve(...)`/`Deny(...)` with note rules and a `DecisionResult` in `src/PrescriptionRefill.Domain/RefillRequests/RefillRequest.cs` and `src/PrescriptionRefill.Domain/RefillRequests/DecisionResult.cs` until T031 passes; then extend `src/PrescriptionRefill.Infrastructure/Seeding/DemoDataSeeder.cs` with decided scenarios 7â€“8 using `Approve`/`Deny` and update the counts in `tests/PrescriptionRefill.IntegrationTests/Persistence/SeederTests.cs`
- [X] T041 [P] [US1] Create DTOs matching contracts/http-api.md (`RefillRequestListDto`, `RefillRequestSummaryDto`, `RefillRequestDetailDto`, `PatientDto`, `PrescriptionDto`, `ActivePrescriptionDto`, `EvaluationDto`, `BlockingReasonDto`, `WarningDto`) in `src/PrescriptionRefill.Application/RefillRequests/Dtos/`
- [X] T042 [US1] Create `RefillDecisionContextLoader` (loads the request with its prescription, the patient with allergies, all of the patient's prescriptions, and the seeded interactions; no rule filtering â€” Domain `ActiveMedications` selects active medications for both the evaluator and the profile DTO) in `src/PrescriptionRefill.Application/RefillRequests/RefillDecisionContextLoader.cs`
- [X] T043 [US1] Implement `GetRefillRequests` (Pending, oldest first, one `evaluatedAt` per call) and `GetRefillRequestDetail` (evaluation only while Pending) in `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs` and `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequestDetail.cs`
- [X] T044 [US1] Implement `ApproveRefillRequest` and `DenyRefillRequest` (load via T042, `now` from `TimeProvider`, Domain decision, one `SaveChangesAsync`, results for not found, validation, already decided, blocked with evaluation, success) in `src/PrescriptionRefill.Application/RefillRequests/Commands/ApproveRefillRequest.cs` and `src/PrescriptionRefill.Application/RefillRequests/Commands/DenyRefillRequest.cs`, with `ConcurrencyConflictException` in `src/PrescriptionRefill.Application/Common/ConcurrencyConflictException.cs` and registrations in `src/PrescriptionRefill.Application/DependencyInjection.cs`
- [X] T045 [US1] Override `SaveChangesAsync` in `src/PrescriptionRefill.Infrastructure/Persistence/RefillDbContext.cs` to translate `DbUpdateConcurrencyException` (raised after EF Core rolls back) into `ConcurrencyConflictException`; no automatic retry
- [X] T046 [US1] Map `GET /api/refill-requests`, `GET /api/refill-requests/{id}`, `POST /api/refill-requests/{id}/approve`, and `POST /api/refill-requests/{id}/deny` in `src/PrescriptionRefill.Api/Endpoints/RefillRequestEndpoints.cs`, and the ProblemDetails mappings for `validation_failed`, `not_found`, `request_already_decided` (`requestId`, `requestStatus`), `approval_blocked` (`requestId`, `evaluation`), and `concurrency_conflict` (`requestId`) in `src/PrescriptionRefill.Api/Errors/ProblemResults.cs`, logging identifiers only, until T032â€“T034 pass
- [X] T047 [P] [US1] Create TypeScript models matching the contract and `RefillRequestsApi` in `frontend/src/app/features/refill-requests/data/models.ts` and `frontend/src/app/features/refill-requests/data/refill-requests.api.ts` until T035 passes
- [X] T048 [US1] Implement the Pending queue page (route `/requests`, default route) with loading, empty, and error states in `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts`, `.html`, `.scss` until T036 passes
- [X] T049 [P] [US1] Implement `PatientPanelComponent` (name, date of birth, allergies, active prescriptions, requested prescription details) and `EvaluationPanelComponent` (`evaluatedAt`, "may be out of date and is re-checked when approving" notice, blocking reasons, warnings labeled synthetic, Refresh button) in `frontend/src/app/features/refill-requests/review/patient-panel.component.ts` and `frontend/src/app/features/refill-requests/review/evaluation-panel.component.ts`
- [X] T050 [US1] Implement `DecisionFormComponent` (labeled note field sent exactly as typed â€” no client-side note rule, per constitution Principle I; server `400` field errors shown inline; Approve disabled when `canApprove` is false; Deny always enabled) in `frontend/src/app/features/refill-requests/review/decision-form.component.ts`
- [X] T051 [US1] Implement the review page (route `/requests/:id`) in `frontend/src/app/features/refill-requests/review/refill-review.page.ts`, `.html`, `.scss` and `DecidedViewComponent` in `frontend/src/app/features/refill-requests/review/decided-view.component.ts`: load on open, Refresh, and outcome handling per the contract's client-handling table (FR-011, FR-023, FR-032) until T037 passes
- [X] T052 [US1] Run `dotnet test` (both test projects) and `npm test` in `frontend/`, then quickstart.md V1â€“V7 and V9â€“V11 against a seeded database; record the actual results

**Checkpoint**: User Story 1 works on its own with seeded requests (MVP).

---

## Phase 4: User Story 2 - Create a refill request through the API (Priority: P2)

**Goal**: API-only request creation with the inactive-prescription rule and one Pending
request per prescription enforced by SQL Server.

**Independent Test**: `POST /api/refill-requests` for an active prescription â†’ 201 and the
request appears in the queue; a second â†’ `409 pending_request_exists`; the inactive Corvantil
prescription â†’ `400` (quickstart V8).

### Tests for User Story 2 (write first; they must fail)

- [X] T053 [P] [US2] Write creation unit tests in `tests/PrescriptionRefill.Domain.Tests/RefillRequests/CreateRefillRequestTests.cs`: an active prescription yields a Pending request with RequestedAt = now; an inactive one yields a validation error
- [X] T054 [P] [US2] Write creation API tests in `tests/PrescriptionRefill.IntegrationTests/Api/CreateRefillRequestTests.cs`: 201 with `Location`; malformed body â†’ 400; unknown prescription â†’ 404; inactive â†’ `400 validation_failed` with `errors.prescriptionId`; blocked prescription â†’ 201 and the detail shows the reason; second Pending â†’ `409 pending_request_exists` with `prescriptionId`; new request after a decision â†’ 201
- [X] T055 [P] [US2] Write creation concurrency tests in `tests/PrescriptionRefill.IntegrationTests/Concurrency/CreateConcurrencyTests.cs`: two contexts insert a Pending request for one prescription â†’ exactly one commits; approving A while creating B â†’ at most one Pending and the refill consumed once

### Implementation for User Story 2

- [X] T056 [US2] Add the inactive-prescription rejection to the existing `RefillRequest.Create(prescription, now)` factory (no second creation path) in `src/PrescriptionRefill.Domain/RefillRequests/RefillRequest.cs` until T053 passes
- [X] T057 [US2] Extend `SaveChangesAsync` in `src/PrescriptionRefill.Infrastructure/Persistence/RefillDbContext.cs` to translate SQL errors 2601/2627 on `UX_RefillRequests_Pending` into `PendingRequestExistsException` (`src/PrescriptionRefill.Application/Common/PendingRequestExistsException.cs`)
- [X] T058 [US2] Implement `CreateRefillRequest` (order: not found â†’ inactive validation â†’ insert) in `src/PrescriptionRefill.Application/RefillRequests/Commands/CreateRefillRequest.cs`
- [X] T059 [US2] Map `POST /api/refill-requests` (201 with `Location`) in `src/PrescriptionRefill.Api/Endpoints/RefillRequestEndpoints.cs` and the `pending_request_exists` mapping in `src/PrescriptionRefill.Api/Errors/ProblemResults.cs` until T054â€“T055 pass; no frontend changes
- [X] T060 [US2] Run the US2 tests in `tests/PrescriptionRefill.Domain.Tests` and `tests/PrescriptionRefill.IntegrationTests` and quickstart.md V8; record the actual results

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Browse requests and outcomes (Priority: P3)

**Goal**: Status filter for Pending, Approved, and Denied, with decided requests read-only and
listed newest decision first.

**Independent Test**: Filter by Approved and Denied, see the seeded decided requests newest
first, and open one with no decision controls.

### Tests for User Story 3 (write first; they must fail)

- [X] T061 [P] [US3] Write status filter API tests in `tests/PrescriptionRefill.IntegrationTests/Api/StatusFilterTests.cs`: each status filters correctly; Approved/Denied sorted by `decidedAt` descending; `canApprove` and `hasWarnings` null for decided items; unknown status â†’ `400 validation_failed`; decided detail has no `evaluation` and shows note and times
- [X] T062 [P] [US3] Write `frontend/src/app/features/refill-requests/queue/status-filter.component.spec.ts` and add filter cases to `frontend/src/app/features/refill-requests/queue/refill-queue.page.spec.ts`: changing the filter reloads with `?status=`; empty state per filter; decided items open read-only

### Implementation for User Story 3

- [X] T063 [US3] Add the `status` parameter and per-status ordering to `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs` and validate it in `src/PrescriptionRefill.Api/Endpoints/RefillRequestEndpoints.cs` until T061 passes
- [X] T064 [US3] Implement `StatusFilterComponent` (labeled native `select`) in `frontend/src/app/features/refill-requests/queue/status-filter.component.ts` and wire it into `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts` via the `status` query parameter until T062 passes
- [X] T065 [US3] Run the US3 tests in `tests/PrescriptionRefill.IntegrationTests` and `frontend/`; record the actual results

**Checkpoint**: User Stories 1â€“3 work independently.

---

## Phase 6: User Story 4 - See changes made elsewhere (Priority: P3)

**Goal**: After each commit, a SignalR `RequestUpdated` hint makes other open screens reload
from the API; reconnect and return also reload.

**Independent Test**: Two tabs on the same request; deciding in one makes the other reload and
show "decided elsewhere"; reconnecting reloads (quickstart V12â€“V14).

### Tests for User Story 4 (write first; they must fail)

- [X] T066 [P] [US4] Write notification tests with `RecordingRequestNotifier` (`tests/PrescriptionRefill.IntegrationTests/Infrastructure/RecordingRequestNotifier.cs`, reads the database in a new context when called) in `tests/PrescriptionRefill.IntegrationTests/Realtime/NotificationTests.cs`: exactly one call after commit per successful approve, deny, and create; zero calls for 400, 404, each 409 code, 422, and a forced concurrency rollback; a throwing notifier still yields 200 with the decision committed
- [X] T067 [P] [US4] Write a hub smoke test in `tests/PrescriptionRefill.IntegrationTests/Realtime/HubSmokeTests.cs`: a `HubConnection` using the test server's handler receives `RequestUpdated` with exactly `requestId` and `change` after an approval
- [X] T068 [P] [US4] Write `frontend/src/app/core/realtime/refill-events.service.spec.ts` (connection state, reconnected signal, events are not used as state) and add cases to the queue and review page specs: queue reloads on any event and on reconnect; review reloads only for a matching `requestId`, on reconnect, and on re-activation; "decided elsewhere" message; typed note kept; "live updates unavailable" indicator while disconnected

### Implementation for User Story 4

- [X] T069 [US4] Create `IRequestNotifier` (`RequestUpdated(requestId, change)`) in `src/PrescriptionRefill.Application/Abstractions/IRequestNotifier.cs` and call it only after a successful `SaveChangesAsync` in `ApproveRefillRequest.cs`, `DenyRefillRequest.cs`, and `CreateRefillRequest.cs` under `src/PrescriptionRefill.Application/RefillRequests/Commands/`, catching and logging failures with the request ID only (FR-031)
- [X] T070 [US4] Create `RefillRequestsHub` (no client-callable methods) and `SignalRRequestNotifier` (`Clients.All.SendAsync("RequestUpdated", { requestId, change })`) in `src/PrescriptionRefill.Api/Realtime/RefillRequestsHub.cs` and `src/PrescriptionRefill.Api/Realtime/SignalRRequestNotifier.cs`; add `AddSignalR` and `MapHub("/hubs/refill-requests")` in `src/PrescriptionRefill.Api/Program.cs` until T066â€“T067 pass
- [X] T071 [US4] Implement `RefillEventsService` (`@microsoft/signalr`, automatic reconnect, `requestUpdated$`, `connectionState`, `reconnected$`) in `frontend/src/app/core/realtime/refill-events.service.ts`
- [X] T072 [US4] Wire reloads into `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts` and `frontend/src/app/features/refill-requests/review/refill-review.page.ts` (events, reconnect, `visibilitychange`, route re-entry; "decided elsewhere"; note kept), and add `ConnectionIndicatorComponent` to the shell in `frontend/src/app/shared/ui/connection-indicator/connection-indicator.component.ts` until T068 passes
- [X] T073 [US4] Run the US4 tests in `tests/PrescriptionRefill.IntegrationTests` and `frontend/`, then quickstart.md V12â€“V14 with two tabs; record the actual results

**Checkpoint**: All user stories work independently.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility, logging review, documentation, and the final verified run.

- [X] T074 [P] Accessibility and responsive pass in `frontend/src/app/**/*.html` and `frontend/src/app/**/*.scss`: labels, focus order, `aria-live` for decision results and errors, no horizontal scroll at 360px and 1280px (quickstart V15â€“V17)
- [X] T075 [P] Review logging in `src/PrescriptionRefill.Api/` and `src/PrescriptionRefill.Application/` so only identifiers are logged (no names, medications, or notes) and set levels in `src/PrescriptionRefill.Api/appsettings.json`
- [X] T076 Verify every README command from a fresh clone and an empty database, then update `README.md` (remove "planned" status, record the verification date and any differences)
- [X] T077 [P] Update `SPEC.md` with delivered behavior, known limitations, and the actual test command output and date
- [X] T078 [P] Update `PROMPTS.md` with the implementation prompts, outcomes, and any rejected AI suggestions with reasons
- [X] T079 Run the full suite (`dotnet test` with `REFILL_TEST_SQLSERVER`, `npm test -- --watch=false --browsers=ChromeHeadless` in `frontend/`) and quickstart.md V1â€“V17; record the actual results in `SPEC.md`, reporting anything not run as not run
- [X] T080 Re-check `specs/001-refill-request-review/plan.md` "Constitution Check" against the delivered code and record any deviation in its Complexity Tracking section

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies. T002 needs T001; T003 needs T002; T006 needs T005.
- **Foundational (Phase 2)**: needs Setup; blocks all stories. T012 â†’ T013 â†’ T014 â†’ T015 â†’
  T016 â†’ T017; T019 â†’ T020 â†’ T021â€“T023; T026â€“T027 â†’ T028; T029 last.
- **User Stories (Phases 3â€“6)**: need Foundational.
- **Polish (Phase 7)**: needs the stories to be delivered.

### User Story Dependencies

- **US1 (P1)**: only Foundational. Seed data supplies its requests.
- **US2 (P2)**: only Foundational; touches `RefillDbContext.cs`, `RefillRequest.cs`,
  `RefillRequestEndpoints.cs`, and `ProblemResults.cs` after US1's edits, so run it after US1
  when one person works sequentially.
- **US3 (P3)**: extends US1's query, endpoint, and queue page.
- **US4 (P3)**: adds notifications to US1's approve/deny commands and US2's create command;
  the create notification needs US2.

### Within Each User Story

- Tests first, observed failing â†’ Domain â†’ Application â†’ Infrastructure translation â†’ API â†’
  frontend services â†’ pages â†’ story run (T052, T060, T065, T073).

### Parallel Opportunities

- Setup: T004, T005, T007 in parallel after T001.
- Foundational: T008â€“T011 together; T018, T024â€“T027 together; T021â€“T023 together after T020.
- US1: all tests T030â€“T037 together; T038, T041, T047, T049 together.
- US2: T053â€“T055 together. US3: T061â€“T062 together. US4: T066â€“T068 together.
- Polish: T074, T075, T077, T078 together.

---

## Parallel Example: User Story 1

```text
# Tests first, together:
Task: "T030 Evaluator unit tests in tests/PrescriptionRefill.Domain.Tests/Evaluation/RefillEvaluatorTests.cs"
Task: "T033 Decision API tests in tests/PrescriptionRefill.IntegrationTests/Api/DecisionEndpointTests.cs"
Task: "T034 Concurrency tests in tests/PrescriptionRefill.IntegrationTests/Concurrency/DecisionConcurrencyTests.cs"
Task: "T037 Review page specs in frontend/src/app/features/refill-requests/review/"

# Independent implementation files, together:
Task: "T038 Evaluation types in src/PrescriptionRefill.Domain/Evaluation/RefillEvaluation.cs"
Task: "T041 DTOs in src/PrescriptionRefill.Application/RefillRequests/Dtos/"
Task: "T047 Models and API service in frontend/src/app/features/refill-requests/data/"
Task: "T049 Patient and evaluation panels in frontend/src/app/features/refill-requests/review/"
```

## Parallel Example: User Story 4

```text
Task: "T066 Notification tests in tests/PrescriptionRefill.IntegrationTests/Realtime/NotificationTests.cs"
Task: "T067 Hub smoke test in tests/PrescriptionRefill.IntegrationTests/Realtime/HubSmokeTests.cs"
Task: "T068 Realtime service spec in frontend/src/app/core/realtime/refill-events.service.spec.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup â†’ Phase 2 Foundational (T029 confirms the database and seed work).
2. Phase 3 US1 â†’ T052: stop and validate with seeded data.
3. This already satisfies the core challenge: queue, review, safety rules, decisions,
   concurrency.

### Incremental Delivery

1. US1 (MVP) â†’ US2 (API creation) â†’ US3 (history filter) â†’ US4 (live updates) â†’ Polish.
2. Each story ends with an actual test run; results are recorded, never assumed.
3. If the timebox runs short, ship after US3 and document US4 as not delivered rather than
   leaving it half-built.

---

## Notes

- [P] tasks touch different files and do not depend on incomplete tasks.
- Never use EF Core InMemory or SQLite; never report a test as passing unless it ran.
- Record prompts that produced kept code in `PROMPTS.md` as work proceeds.
- Commit after each task or logical group; stop at any checkpoint to validate.
