# Implementation Plan: Server-Side Pagination of Refill Request Lists

**Branch**: `003-backend-pagination` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-backend-pagination/spec.md`

## Summary

Move queue paging, search, and summary counts from the browser to the list endpoint, without
changing any safety rule, decision, detail, or creation behavior. `GET /api/refill-requests`
gains optional `page`, `pageSize`, and `search` parameters. Without them, the response is
exactly today's (research R1). With them, the response carries one page plus `matchingCount`,
`statusTotal`, and, for Pending, a whole-status `summary`.

Pending still evaluates every Pending request once at one instant (the existing code path),
because the summary covers the whole status. SQL then selects the page's ids by search and
order. Approved and Denied, which grow without limit, load only `COUNT`s and one page
(research R3). The Angular queue requests one page at a time through a `switchMap` pipeline and
drops its client-side filtering and slicing, keeping the 002 UI contract (research R9).

## Technical Context

**Language/Version**: C# on .NET (`net10.0`, SDK 11.0.100-rc.2); TypeScript 5.6 / Angular 19

**Primary Dependencies**: Existing only: ASP.NET Core minimal APIs, EF Core 10.0.13 SQL Server
provider; `@angular/*` 19, `rxjs`. No new packages.

**Storage**: SQL Server 2022. **No schema change and no migration** (research R7).

**Testing**: xUnit Domain tests (unchanged); xUnit integration tests against SQL Server via the
existing `SqlServerDatabaseFixture`; Karma + Jasmine for the frontend

**Target Platform**: Windows dev machine (as verified in 001 research R0); evergreen browsers

**Project Type**: Web application. `src/` (4-layer .NET backend) + `frontend/` (Angular)

**Performance Goals**: Page or search response within 1 s with 1,000 requests in a status
(SC-005); at most `pageSize` items delivered per load (SC-001)

**Constraints**: Unpaged response unchanged (FR-002); no rule logic outside Domain; search text
never logged; existing error codes only; HTTP only in `RefillRequestsApi`; 002 DOM hooks and
texts kept

**Scale/Scope**: 1 endpoint, 1 use case, 2 DTOs, 1 new query record; 1 Angular page, 1 API
service, models; roughly 8 source files and 11 test files (7 new, 4 updated)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|-----------|-------|--------|
| I. Pragmatic Clean Architecture | Changes stay inside the existing layers: the endpoint parses (API), `GetRefillRequests` queries (Application), and evaluation stays in `RefillEvaluator` (Domain). One new type, `RefillRequestListQuery`, has an immediate consumer. No repository, mediator, or interface added. | PASS |
| II. Deterministic Server-Side Rules | Flags and summary come from the existing `RefillEvaluator.Evaluate` at one `TimeProvider` instant; the browser stops computing counts (it computed them from server flags before). No rule changes. | PASS |
| III. Workflow Integrity | Decisions, transitions, and notes untouched. | PASS (N/A) |
| IV. Persistence & Concurrency | Reads only; no schema change, so no migration needed. Two reads per Pending page may disagree briefly under a concurrent decision; self-corrects via the live hint. Snapshot isolation rejected as disproportionate (research R3). | PASS |
| V. Test Evidence | New integration tests against SQL Server: paging, search (case, trim, wildcards), totals, summary, validation, beyond-last-page, unpaged unchanged, log privacy, 1,000-row timing. Replaced frontend specs documented with equal or stronger assertions (research R9). Results reported only from real runs. | PASS |
| VI. Security & Data Handling | New inputs validated on the server with the existing `validation_failed`; DTOs only; search text excluded from logs and covered by a test (research R8). | PASS |
| VII. Frontend Quality | HTTP stays in `RefillRequestsApi`; loading, empty, no-match, and error states kept; keyboard and focus behavior kept. | PASS |
| Delivery / Workflow | Scope is optional polish after the delivered challenge. Docs (`SPEC.md`, 001 contract, 002 R13, `PROMPTS.md`, `README.md` if needed) updated at the end. | PASS |

**Post-design re-check (after Phase 1)**: PASS. The design adds no stored data, no dependency,
and no abstraction beyond `RefillRequestListQuery`. The only intentional test rewrites are the
002 client-side search and pagination specs, whose behavior moves to the server (research R9);
equivalent server-side assertions are added in integration tests. No complexity tracking
entries are needed.

## Project Structure

### Documentation (this feature)

```text
specs/003-backend-pagination/
├── plan.md              # This file
├── research.md          # Phase 0: decisions R1–R10
├── data-model.md        # Phase 1: query + response shapes (no stored-data change)
├── quickstart.md        # Phase 1: validation scenarios Q1–Q11, S1–S6
├── contracts/
│   └── http-api.md      # Phase 1: change to GET /api/refill-requests
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 (/speckit-tasks; not created here)
```

### Source Code (repository root)

```text
src/
├── PrescriptionRefill.Api/
│   └── Endpoints/RefillRequestEndpoints.cs        # parse page/pageSize/search; validation errors
├── PrescriptionRefill.Application/
│   └── RefillRequests/
│       ├── Queries/GetRefillRequests.cs           # paged + unpaged branches, search predicate
│       ├── Queries/RefillRequestListQuery.cs      # NEW: input record + limits
│       └── Dtos/RefillRequestDtos.cs              # list DTO fields; ListSummaryDto
├── PrescriptionRefill.Domain/                     # unchanged
└── PrescriptionRefill.Infrastructure/             # unchanged (no migration)

tests/
├── PrescriptionRefill.Domain.Tests/               # unchanged
└── PrescriptionRefill.IntegrationTests/
    ├── Api/PagedListTests.cs                      # NEW: paging, totals, beyond-last, ordering
    ├── Api/ListSearchTests.cs                     # NEW: case, trim, wildcard literals, privacy (log capture)
    ├── Api/ListSummaryTests.cs                    # NEW: whole-status summary on every page
    ├── Api/ListValidationTests.cs                 # NEW: page/pageSize/search 400s
    ├── Api/PagedListPerformanceTests.cs           # NEW: 1,000-request timing (SC-001, SC-005)
    ├── Infrastructure/CapturingLoggerProvider.cs  # NEW: log capture for the privacy test
    ├── Infrastructure/TestData.cs                 # optional patient/medication names
    └── Api/QueueAndDetailTests.cs, StatusFilterTests.cs   # unchanged: prove unpaged mode

frontend/src/app/features/refill-requests/
├── data/models.ts                                 # optional page fields, ListSummary
├── data/refill-requests.api.ts                    # list(status, query?)
├── data/refill-requests.api.spec.ts               # + paged params test
├── queue/refill-queue.page.ts                     # switchMap loads, debounce, server totals
├── queue/refill-queue.page.html                   # bindings to server totals (same DOM hooks)
├── queue/refill-queue.page.spec.ts                # call assertions gain the query argument
├── queue/refill-queue.redesign.spec.ts            # search/pagination specs rewritten (research R9)
└── testing/fixtures.ts                            # paged list fixture
```

**Structure Decision**: Use the existing web-application layout. The backend change is
confined to the API endpoint and the Application list query and DTOs; Domain and
Infrastructure are untouched. The frontend change is confined to the refill-requests feature's
data service, models, and queue page. The shared `app-pagination` component is reused
unchanged.

## Implementation Order

1. Backend contract tests first (new integration test files; observe them failing).
2. `RefillRequestListQuery`, DTO fields, `GetRefillRequests` paged branches; endpoint parsing.
3. Confirm the existing integration tests pass unchanged (unpaged mode).
4. Frontend: models and API service with spec; queue page with rewritten specs.
5. Quickstart Q1–Q11 against a throwaway database; S1–S6 in a browser if performed (report
   only what was run).
6. Docs: 001 contract list section, 002 R13 note, `SPEC.md`, `PROMPTS.md`.

## Complexity Tracking

No constitution violations; nothing to justify.
