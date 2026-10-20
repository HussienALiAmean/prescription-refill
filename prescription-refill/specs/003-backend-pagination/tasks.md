---

description: "Task list for server-side pagination of refill request lists"
---

# Tasks: Server-Side Pagination of Refill Request Lists

**Input**: Design documents from `/specs/003-backend-pagination/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/http-api.md, quickstart.md

**Tests**: Requested. Spec SC-006 requires existing behaviors to stay verified by passing tests,
and constitution Principle V requires integration tests against SQL Server for endpoints and
validation. Tests are written first and observed failing (as in 001 and 002). Existing tests
are not weakened. The only intentional rewrites are the 002 client-side search and pagination
specs, whose behavior moves to the server (research R9).

**Organization**: Tasks are grouped by user story. US1 and US2 are both P1 and together form
the MVP: once the screen loads one page, search must also run on the server (spec US2 "Why this
priority").

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: The user story the task belongs to (US1–US4)

## Path Conventions

- Backend: `src/PrescriptionRefill.{Api,Application}/…`; tests: `tests/PrescriptionRefill.IntegrationTests/…`
- Frontend: `frontend/src/app/features/refill-requests/…`
- **Not touched**: `src/PrescriptionRefill.Domain/`, `src/PrescriptionRefill.Infrastructure/`
  (no migration; research R7), `tests/PrescriptionRefill.Domain.Tests/`

## Test-data rule for integration tests

`SqlServerDatabaseFixture` gives each test **class** one database, shared by that class's
tests. Paging tests MUST NOT hard-code totals. They compare paged results with the **unpaged**
list fetched in the same test (`GET /api/refill-requests[?status=…]`), and search tests use
names unique to the test (`Guid`-based).

---

## Phase 1: Setup (Baseline)

- [X] T001 Run `dotnet build PrescriptionRefill.slnx`, `dotnet test PrescriptionRefill.slnx`, and in `frontend/` `npm run test:ci` and `npm run build`; record the counts and bundle size in a new "Baseline" section at the end of `specs/003-backend-pagination/quickstart.md` (expected: 0 warnings; Domain 47/47; integration 61/61; frontend 89/89; initial 413.32 kB). Stop if any baseline test fails.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The query type, the response fields, parameter parsing and validation, and the
client API signature. These are used by every story. After this phase, unpaged behavior is
unchanged and the paged branch exists but is not yet implemented.

### Tests (write first, observe failing)

- [X] T002 [P] Create `tests/PrescriptionRefill.IntegrationTests/Api/ListValidationTests.cs` (class fixture `SqlServerDatabaseFixture`, `RefillApiFactory`, helpers from `Infrastructure/Json.cs`). Each case asserts `400`, `code = "validation_failed"`, and `errors` containing exactly the named field with the message from `specs/003-backend-pagination/contracts/http-api.md` "Errors":
  - `page` = `0`, `-1`, `abc`, `1.5`, `+1`, ` 1`
  - `pageSize` = `0`, `101`, `abc`
  - `search` of 101 characters after trimming
  - Order: `status=Closed&page=0` reports `status`; `page=0&pageSize=0` reports `page`
  - Acceptance cases (`200`): `search` of exactly 100 characters; 100 characters with surrounding spaces; `pageSize=100`; `pageSize=1`; `page=999999`

### Implementation

- [X] T003 [P] Create `src/PrescriptionRefill.Application/RefillRequests/Queries/RefillRequestListQuery.cs` with:
  - `public sealed record PageRequest(int Page, int PageSize, string? Search)`
  - `public sealed record RefillRequestListQuery(RefillRequestStatus Status, PageRequest? Paging)`, with constants `DefaultPageSize = 10`, `MaxPageSize = 100`, `MaxSearchLength = 100`
  - `Paging == null` means unpaged (research R1); `Search` is already trimmed and null when empty
  - XML doc comments in the style of `GetRefillRequests.cs`, citing spec FR-001–FR-008
- [X] T004 [P] In `src/PrescriptionRefill.Application/RefillRequests/Dtos/RefillRequestDtos.cs`:
  - Add `public sealed record ListSummaryDto(int Blocked, int Warnings, int Clear);`
  - Extend `RefillRequestListDto` with trailing optional parameters `int? Page = null, int? PageSize = null, int? MatchingCount = null, int? StatusTotal = null, ListSummaryDto? Summary = null`
  - Mark each with `[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`, as `Evaluation` is on the detail DTO, so unpaged JSON stays `{ evaluatedAt, items }` (data-model.md)
- [X] T005 In `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs`:
  - Change `ExecuteAsync` to `ExecuteAsync(RefillRequestListQuery query, CancellationToken)`
  - Move the current body unchanged into `private Task<RefillRequestListDto> ExecuteUnpagedAsync(RefillRequestStatus, DateTimeOffset now, CancellationToken)`
  - Dispatch: `query.Paging is null` → unpaged; otherwise `ExecutePagedAsync(query.Status, query.Paging, now, ct)`, which for now throws `NotSupportedException("Implemented in US1")`
  - Read `now` once in `ExecuteAsync`
- [X] T006 In `src/PrescriptionRefill.Api/Endpoints/RefillRequestEndpoints.cs`, list endpoint:
  - Bind `string? page, string? pageSize, string? search` next to `status`
  - Parse with `int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out …)`, which rejects signs, spaces, and decimals
  - Validate in order `status` → `page` (≥ 1) → `pageSize` (1–`MaxPageSize`) → `search` (trimmed length ≤ `MaxSearchLength`; whitespace → null)
  - Return `ProblemResults.Validation(field, message)` with the exact contract messages
  - Paged when any of the three raw values is non-null; defaults are page 1 and `DefaultPageSize`
  - Build `RefillRequestListQuery` and call `query.ExecuteAsync(listQuery, ct)`
  - Never log `search`
  - Validation tests in T002 for invalid input pass; their `200` cases still fail until T015
- [X] T007 Run `dotnet test PrescriptionRefill.slnx`. All 61 existing integration tests and 47 Domain tests MUST pass unchanged; this proves unpaged mode is identical (spec FR-002). Fix the implementation, never the existing tests.
- [X] T008 [P] In `frontend/src/app/features/refill-requests/data/models.ts`:
  - Add `export interface ListSummary { blocked: number; warnings: number; clear: number; }` and `export interface ListQuery { page: number; pageSize: number; search?: string; }`
  - Add optional `page?`, `pageSize?`, `matchingCount?`, `statusTotal?`, `summary?: ListSummary` to `RefillRequestList`, with a comment citing 003 contracts
- [X] T009 [P] Add a paged-params test to `frontend/src/app/features/refill-requests/data/refill-requests.api.spec.ts`, keeping the existing no-parameter test unchanged:
  - `list('Approved', { page: 2, pageSize: 25, search: '  okafor ' })` sends `status=Approved&page=2&pageSize=25&search=okafor`
  - `list('Pending', { page: 1, pageSize: 10, search: '   ' })` sends `page=1&pageSize=10` with no `status` and no `search`
- [X] T010 In `frontend/src/app/features/refill-requests/data/refill-requests.api.ts`, change `list(status = 'Pending', query?: ListQuery)`:
  - No `query` → current behavior
  - With `query`, add `page` and `pageSize`, plus `search` only when its trimmed value is non-empty (sent trimmed)
  - `status` only when not Pending (as today)
  - T009 passes
- [X] T011 [P] In `frontend/src/app/features/refill-requests/testing/fixtures.ts`, add `pagedList(overrides: Partial<RefillRequestList> = {})`. It returns `list()`'s items plus `page: 1, pageSize: 10, matchingCount`, `statusTotal` (both = items length), and an explicit `summary` (fixed numbers given by the caller or defaults, **not** computed from item flags).

**Checkpoint**: Backend builds; unpaged API identical; frontend unchanged in behavior; T002's
invalid-input cases pass.

---

## Phase 3: User Story 1 - Browse a long list one page at a time (Priority: P1) 🎯 MVP (with US2)

**Goal**: The server returns one page in the established order with `page`, `pageSize`,
`matchingCount`, and `statusTotal`, and the queue screen requests one page at a time.

**Independent Test**: Seed more requests than one page holds; page through each status. Every
request appears exactly once, in order; position text is correct; each load returns only one
page (quickstart Q2–Q4, Q8, Q10).

### Tests for User Story 1 (write first)

- [X] T012 [P] [US1] Create `tests/PrescriptionRefill.IntegrationTests/Api/PagedListTests.cs` (own class fixture). Arrange 23 Pending requests with `TestData.ArrangeAsync(Now, requestedAgo: …)`, giving distinct ages plus 3 requests with an identical `requestedAgo` for ties. Tests:
  - `Paging_through_pending_returns_every_request_once_in_unpaged_order`: concatenating `?page=n&pageSize=10` until `items` is empty equals the unpaged ids exactly
  - `Page_metadata_echoes_the_request_and_totals_match_the_unpaged_list`
  - `Only_page_or_only_page_size_applies_the_default_for_the_other`: `?page=2` → `pageSize` 10; `?pageSize=5` → `page` 1
  - `Page_beyond_the_last_returns_no_items_with_correct_totals`: `200`; also `page=999999`
  - `Equal_request_times_keep_id_order_across_a_page_boundary`
  - `Pending_page_flags_equal_the_unpaged_flags`: same `canApprove`/`hasWarnings` per id
  - `Decided_lists_page_newest_decision_first`: deny 12 requests via the API, advancing `_api.Clock` between them; concatenated `?status=Denied` pages equal the unpaged Denied ids; `statusTotal` equals the unpaged count; `summary` key absent
  - `Unpaged_response_has_only_evaluatedAt_and_items`: JSON object keys exactly `evaluatedAt`, `items`
- [X] T013 [P] [US1] Update `frontend/src/app/features/refill-requests/queue/refill-queue.page.spec.ts`. Replace `toHaveBeenCalledWith('<Status>')` assertions with `toHaveBeenCalledWith('<Status>', jasmine.objectContaining({ page: 1, pageSize: 10 }))`, and return `pagedList()` from `api.list`. Add:
  - `shows only the newest response when an older one arrives late`: two `Subject`s; resolve the second, then the first; the second is displayed (spec FR-016)
- [X] T014 [P] [US1] Rewrite the pagination specs in `frontend/src/app/features/refill-requests/queue/refill-queue.redesign.spec.ts`, keeping the DOM selectors and texts:
  - `paginates the search result, not the whole list` → `requests each page from the server and renders the returned page`: Next sends `page: 2`; rows equal the returned items; "Showing 11–20 of 23" from `matchingCount` 23
  - `returns to page 1 when the search, page size, or status changes` → keep the page-size and status cases (the search case moves to T021)
  - `shows the last existing page when a reload shrinks the list` → `requests the last existing page when the server returns an empty page beyond the end`: response `items: [], matchingCount: 12, page: 3` at size 10 triggers one more call with `page: 2`

### Implementation for User Story 1

- [X] T015 [US1] In `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs`, implement `ExecutePagedAsync`:
  - Extract the existing Pending evaluation (patients' prescriptions, interactions, `RefillEvaluator.Evaluate` at `now`) into `private async Task<List<(RefillRequest Request, RefillEvaluation Evaluation)>> EvaluatePendingAsync(DateTimeOffset now, CancellationToken)`, used by both the unpaged and paged branches (no logic change)
  - Add `private static IOrderedQueryable<RefillRequest> Ordered(IQueryable<RefillRequest>, RefillRequestStatus)` with today's order and id tie-break, used everywhere
  - Compute `skip` as `(long)(page - 1) * pageSize`; if `skip >= matchingCount`, return an empty page without querying items
  - **Pending** (research R3): `statusTotal` = evaluated count; page ids = `Ordered(status filter).Skip(skip).Take(size).Select(r => r.Id)` from SQL; `matchingCount` = SQL `CountAsync`; items mapped from the evaluated list by id, in page-id order
  - **Approved/Denied**: `statusTotal` and `matchingCount` by `CountAsync`; page by `Ordered(...).Skip().Take()` with the existing `Include`s and `AsNoTrackingWithIdentityResolution`; flags null
  - Return the DTO with `page`, `pageSize`, `matchingCount`, `statusTotal`; `summary` stays null until US3
  - Add a summary comment citing research R3, including the brief-inconsistency note
  - T012 passes
- [X] T016 [US1] In `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts`:
  - Replace direct `subscribe` in `load()` with `private readonly loads = new Subject<void>()` piped through `switchMap(() => this.api.list(this.selected(), { page: this.page(), pageSize: this.pageSize(), search: this.query().trim() || undefined }))` and subscribed with `takeUntilDestroyed()`. Keep the loading/reloading/error state handling and its signals.
  - Remove `visibleItems`, `pagedItems`, and local slicing
  - `pageCount = max(1, ceil((list()?.matchingCount ?? 0) / pageSize()))`
  - `goToPage`/`setPageSize` set the signal and call `load()`
  - On a response with `items.length === 0 && matchingCount > 0 && page > 1`, set `page` to `pageCount` and load once more (guard against loops)
  - Keep `afterNextRender` focus to the results heading
  - Until US2, the search box is still bound but `query` is sent as is (search runs on the server after T022)
- [X] T017 [US1] In `frontend/src/app/features/refill-requests/queue/refill-queue.page.html`:
  - Loop over `loaded.items` (the page)
  - Bind `app-pagination` `[page]="currentPage()"`, `[total]="loaded.matchingCount ?? loaded.items.length"`
  - Results heading counts from `matchingCount`
  - Keep every class, id (`queue-results-heading`), and text the 002 specs read (002 `contracts/ui-contract.md`)
  - T013 and T014 pass

**Checkpoint**: Paging works end to end for all statuses; quickstart Q1–Q4, Q8, Q10 pass.

---

## Phase 4: User Story 2 - Search across the whole list (Priority: P1) 🎯 MVP (with US1)

**Goal**: Search runs on the server across every request of the status; paging and
`matchingCount` apply to the matches; the search text is never logged.

**Independent Test**: A match that is on page 3 unfiltered is found from page 1, and the result
count is correct (quickstart Q5–Q7, S3, S4).

### Tests for User Story 2 (write first)

- [X] T018 [P] [US2] In `tests/PrescriptionRefill.IntegrationTests/Infrastructure/TestData.cs`, add an optional `string? patientName = null` parameter to `ArrangeAsync` (default keeps today's generated name, so existing callers are unchanged). Also add an optional `string? medicationName = null` used instead of `UniqueName("Req")` when given.
- [X] T019 [P] [US2] Create `tests/PrescriptionRefill.IntegrationTests/Infrastructure/CapturingLoggerProvider.cs`: an `ILoggerProvider` that records every formatted message plus exception text into a thread-safe list. It is registered through `RefillApiFactory.ConfigureServices` (`services.AddSingleton<ILoggerProvider>(provider)`); the configured logging filters from `appsettings.json` still apply.
- [X] T020 [US2] Create `tests/PrescriptionRefill.IntegrationTests/Api/ListSearchTests.cs` (own class fixture; names built from a `Guid` token):
  - `Search_finds_a_match_that_is_not_on_the_first_unfiltered_page`: 15 Pending requests, the target requested most recently, so it is last; `?search=<token>` from page 1 returns it with `matchingCount` 1
  - `Search_matches_patient_or_medication_ignoring_case_and_surrounding_spaces`: patient-name match, medication-name match, `UPPER`, `"  token  "`; all equal
  - `Search_results_are_paged`: 12 matches at `pageSize=10` → 10 then 2, unpaged order preserved
  - `Wildcard_characters_match_literally`: patient `"Lit_<token> 50%"` and `"LitX<token> 50X"`; `search=_<token>` and `search=50%` each return only the first; `search=[` returns none
  - `Status_total_ignores_search`: `statusTotal` equals the unpaged count while `matchingCount` is smaller
  - `Empty_or_whitespace_search_is_paged_without_a_filter`: `search=` and `search=%20%20` return paged metadata with `matchingCount == statusTotal`
  - `Search_text_is_never_written_to_logs`: register `CapturingLoggerProvider`; search for a unique token in paged Pending and Denied lists; assert no captured entry contains the token (spec FR-009, research R8)
- [X] T021 [P] [US2] Update `frontend/src/app/features/refill-requests/queue/refill-queue.redesign.spec.ts` search specs (use `fakeAsync` and `tick(300)`):
  - `filters by patient or medication, case-insensitively, keeping server order` → `sends the trimmed search to the server after 300 ms and renders the server's matches in order`
  - `shows a distinct empty state for no matches, with a working Clear search`: server returns `items: [], matchingCount: 0, statusTotal: 6`; Clear search triggers an immediate load without `search`
  - `keeps the search text when the list reloads from a live update`: the reload call includes `search`
  - New: `typing quickly sends only the final text` (FR-016, debounce)
  - New: the search case of "returns to page 1 when the search changes"

### Implementation for User Story 2

- [X] T022 [US2] In `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs`:
  - Add `private const string SearchCollation = "Latin1_General_100_CI_AS";`
  - Add `private static IQueryable<RefillRequest> Matching(IQueryable<RefillRequest> source, string? term)`: returns `source` when `term` is null; otherwise filters `EF.Functions.Collate(r.Prescription.Patient.FullName, SearchCollation).Contains(term) || EF.Functions.Collate(r.Prescription.Medication.Name, SearchCollation).Contains(term)` (research R4)
  - Apply it to both the `matchingCount` query and the page query in both paged branches (never to `statusTotal`)
  - No logging of `term`
  - T020 passes (if the wildcard test fails, the provider is not escaping; escape `[`, `%`, `_` explicitly and use `EF.Functions.Like` with an escape character, then re-run)
- [X] T023 [US2] In `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts`:
  - Add `private readonly searches = new Subject<void>()` piped `debounceTime(300)` into `load()`, with `takeUntilDestroyed()`
  - `setQuery(value)` sets `query`, sets `page` to 1, and emits on `searches`
  - `clearQuery()` sets `query` to `''`, sets `page` to 1, and calls `load()` immediately
  - Results heading: "n of N requests match" with `n = matchingCount`, `N = statusTotal` (same wording as today)
- [X] T024 [US2] Adjust bindings in `frontend/src/app/features/refill-requests/queue/refill-queue.page.html` for the search heading and no-match state, using server totals and keeping the existing texts and selectors. T021 passes.

**Checkpoint (MVP)**: US1 + US2 deliver server paging and search; quickstart Q1–Q10 pass.

---

## Phase 5: User Story 3 - Exact summary counts while paging (Priority: P2)

**Goal**: The Pending response carries a whole-status `summary` (blocked / warnings / clear),
and the screen shows it and the status total from the server.

**Independent Test**: Known numbers of blocked, warning, and clear requests spread across pages
give the same exact summary on every page, with or without search (quickstart Q2, Q5).

### Tests for User Story 3 (write first)

- [X] T025 [P] [US3] Create `tests/PrescriptionRefill.IntegrationTests/Api/ListSummaryTests.cs` (own class fixture). Arrange a mixture with `TestData.ArrangeAsync`: too early (`sinceLastFill` short), zero refills, allergy (`allergicToRequested`), interaction (`interactingActive`), too early plus interaction, and clear; spread them over 3 pages at `pageSize=3`. Tests:
  - `Summary_equals_counts_from_the_unpaged_flags`: compute expected from the unpaged response's `canApprove`/`hasWarnings`
  - `Summary_is_identical_on_every_page_and_with_a_search`
  - `Summary_is_absent_for_approved_and_denied`
  - `Summary_and_items_share_one_evaluation_instant`: `evaluatedAt` equals `_api.Clock` now
- [X] T026 [P] [US3] Update the count assertions in `frontend/src/app/features/refill-requests/queue/refill-queue.redesign.spec.ts` and `frontend/src/app/features/refill-requests/queue/status-filter.component.spec.ts` (only where the page passes the count). Render a `pagedList()` whose `summary` and `statusTotal` deliberately differ from what the returned items' flags would give, and assert the screen shows the server numbers. This proves the page no longer counts by itself (spec FR-011, FR-012).

### Implementation for User Story 3

- [X] T027 [US3] In `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs`, Pending paged branch: build `ListSummaryDto` from the full evaluated list:
  - `blocked = count(!CanApprove)`
  - `warnings = count(Warnings.Count > 0)`
  - `clear = count(CanApprove && Warnings.Count == 0)`
  - Ignores search; set `Summary` only for Pending. T025 passes.
- [X] T028 [US3] In `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts` and `.html`:
  - `counts()` returns `{ total: statusTotal, blocked/warnings/clear from summary }` (null-safe; summary section hidden when `summary` is absent)
  - `app-status-filter [count]` binds `list()?.statusTotal ?? null`
  - Remove the item-flag counting. T026 passes.

**Checkpoint**: Summary counts exact on every page and with search.

---

## Phase 6: User Story 4 - Live updates keep the current page correct (Priority: P3)

**Goal**: Hints, reconnects, returning to the tab, and Refresh reload the current page with the
current search and page size; an emptied last page moves to the new last page.

**Independent Test**: On page 2 with a search and size 25, a `RequestUpdated` hint reloads
exactly that page, keeping both (quickstart S5).

### Tests for User Story 4 (write first)

- [X] T029 [P] [US4] Add to `frontend/src/app/features/refill-requests/queue/refill-queue.page.spec.ts` (using `FakeRefillEvents`):
  - `a RequestUpdated hint reloads the current page with the current search and page size`: expects `{ page: 2, pageSize: 25, search: 'x' }`
  - `reconnect and becoming visible reload the current page` (same parameters)
  - `a hint that empties the last page loads the new last page`

### Implementation for User Story 4

- [X] T030 [US4] In `frontend/src/app/features/refill-requests/queue/refill-queue.page.ts`, make sure `reloadOnChanges(() => this.load())` and Refresh go through the `loads` pipeline with current signals and do not reset `page` or `query`; only a status change resets `page` (keep the existing `effect`). T029 passes.
- [X] T031 [US4] Run `dotnet test --filter "FullyQualifiedName~Realtime"` and confirm `tests/PrescriptionRefill.IntegrationTests/Realtime/NotificationTests.cs` and `HubSmokeTests.cs` pass unchanged (hints are emitted only on commits; listing never notifies).

**Checkpoint**: All four stories work independently and together.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T032 [P] Create `tests/PrescriptionRefill.IntegrationTests/Api/PagedListPerformanceTests.cs` (own class fixture). Insert 1,000 Pending requests efficiently: one patient and 1,000 prescriptions of one medication, inserted with one `SaveChangesAsync` through `TestData.NewContext()`. Warm up once, then time `?page=1&pageSize=10`, `?page=100&pageSize=10`, and `?search=<medication name fragment>` with `Stopwatch`. Assert each has at most 10 items (SC-001) and each elapsed time is < 1 s (SC-005). Write the timings to `ITestOutputHelper`.
- [X] T033 [P] Update `specs/001-refill-request-review/contracts/http-api.md` `GET /api/refill-requests`: replace "Not paginated." with "Optional paging and search: see [003 contracts/http-api.md](../../003-backend-pagination/contracts/http-api.md); without them the response below is unchanged." Do not change any other endpoint.
- [X] T034 [P] Append to `specs/002-premium-ui-redesign/research.md` R13 a line "**Superseded 2026-10-20**: server-side paging delivered by feature 003 ([spec](../003-backend-pagination/spec.md)); the queue no longer slices or filters locally." Do not alter the original decision text.
- [X] T035 [P] Update `SPEC.md`:
  - API table row for `GET /api/refill-requests`: add `page`, `pageSize`, `search`; paged fields; `400 validation_failed` for invalid paging or search
  - "UI redesign (feature 002)" bullet: pagination and search now run on the server (link 003)
  - "Known limitations": remove "The request list API is not paginated…" and keep the Pending note that all Pending requests are still evaluated per load (003 spec Assumptions)
  - Add a short "Server-side pagination (feature 003)" subsection under "Delivered behavior"
  - Add `PagedListTests`, `ListSearchTests`, `ListSummaryTests`, and `ListValidationTests` to the test mapping only where they verify listed behavior
- [X] T036 [P] Add a "Server-side pagination (feature 003, 2026-10-20)" table to `PROMPTS.md`: `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, and implementation prompts with outcomes, recording the decisions kept or rejected (opt-in paging; snapshot isolation rejected; search moved with paging; no index or migration).
- [X] T037 [P] Update `README.md` only if needed. Check "Run" and "How it works" for statements about client-side paging and adjust them. Do not add new sections.
- [ ] T038 Final verification, recorded in `SPEC.md` and `specs/003-backend-pagination/quickstart.md`:
  - Run `dotnet build PrescriptionRefill.slnx` (0 warnings), `dotnet test PrescriptionRefill.slnx`, and in `frontend/` `npm run test:ci` and `npm run build`
  - Run quickstart Q1–Q10 against a throwaway database `RefillPaging_Check_<timestamp>` set through `ConnectionStrings__RefillDb` (never the existing `PrescriptionRefill` database); drop it afterwards
  - Clean-environment check (constitution Delivery Constraints): fresh copy without `bin`/`obj`/`node_modules`/`dist`, new empty database; README `--seed`, `dotnet run`, `npm ci`, `npm run test:ci`, `npm start` with `/api` through the proxy
  - Perform S1–S6 only if a browser session is actually run; otherwise list them as not run
  - Confirm `src/PrescriptionRefill.Domain/`, `src/PrescriptionRefill.Infrastructure/` (including `Persistence/Migrations/`), and `tests/PrescriptionRefill.Domain.Tests/` are unchanged
  - Record actual results in the `SPEC.md` "Verification" table and `specs/003-backend-pagination/quickstart.md`

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (T001)** → **Foundational (T002–T011)** → user stories → **Polish (T032–T038)**.
- **US1 (T012–T017)** depends on Foundational.
- **US2 (T018–T024)** depends on US1: it extends `ExecutePagedAsync` (T015) and the `loads` pipeline (T016).
- **US3 (T025–T028)** depends on US1 (paged Pending branch). It is independent of US2, apart from the "with a search" assertion in T025, which needs T022.
- **US4 (T029–T031)** depends on US1 (pipeline) and, for the search parameter, US2.

### Within each story

Tests first and observed failing → Application (`GetRefillRequests.cs`) → frontend page →
template. `GetRefillRequests.cs` is edited by T005, T015, T022, and T027 in sequence (never in
parallel). `refill-queue.page.ts` is edited by T016, T023, T028, and T030 in sequence.

### Story completion order

```text
Setup → Foundational → US1 → US2 → (US3, US4) → Polish
                       └─ MVP = US1 + US2 ─┘
```

## Parallel Opportunities

- **Foundational**: T002, T003, T004, T008, T009, and T011 in parallel (different files); then T005 → T006 → T007; T010 after T009.
- **US1**: T012, T013, and T014 in parallel; then T015 (backend) in parallel with T016 → T017 (frontend).
- **US2**: T018, T019, and T021 in parallel; T020 after T018 and T019; then T022 in parallel with T023 → T024.
- **US3**: T025 and T026 in parallel; then T027 in parallel with T028.
- **Polish**: T032–T037 in parallel; T038 last.

### Example: User Story 1

```text
Parallel: T012 PagedListTests.cs | T013 refill-queue.page.spec.ts | T014 refill-queue.redesign.spec.ts
Then parallel: T015 GetRefillRequests.cs (backend)  ||  T016 → T017 queue page (frontend)
```

## Implementation Strategy

### MVP (US1 + US2)

1. T001 baseline; T002–T011 foundation; confirm the 108 existing backend tests pass unchanged (T007).
2. US1: server paging and the page-at-a-time screen.
3. US2: server search; at this point the screen is never wrong about matches.
4. **Stop and validate**: quickstart Q1–Q10; full test suites.

### Incremental delivery

5. US3: server summary counts (exact on every page).
6. US4: live-update reload keeps page, search, and size.
7. Polish: performance check, docs, final verification with results recorded from real runs only.

### Rules carried from the constitution

- No rule logic outside the Domain; counts come from `RefillEvaluator` results only.
- No new packages, interfaces, repositories, or migrations.
- Never weaken an existing test. If an existing test fails, fix the implementation. The only
  test rewrites are those listed in T013, T014, T021, and T026.
- Report only executed results; list unexecuted checks as not run.
