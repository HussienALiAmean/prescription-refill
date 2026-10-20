# Research: Server-Side Pagination of Refill Request Lists

All Technical Context unknowns are resolved below. Sources: the current code
(`RefillRequestEndpoints.cs`, `GetRefillRequests.cs`, `refill-queue.page.ts`), 001
`contracts/http-api.md`, and 002 research R5, R6, R13.

## R1 Backward compatibility: paged mode is opt-in

- **Decision**: The list is **paged** when the query has any of `page`, `pageSize`, or `search`.
  Without all three, the list is **unpaged** and the response is byte-for-byte today's shape:
  `{ evaluatedAt, items }`, with the full list. In paged mode a missing `page` defaults to 1 and
  a missing `pageSize` to 10 (spec FR-002).
- **Rationale**: Spec FR-002 and SC-006. Every existing integration test (`QueueAndDetailTests`,
  `StatusFilterTests`, `NotificationTests`, …) calls the list without parameters and keeps
  working unchanged. `search` alone also turns on paged mode, so a search can never return an
  unbounded list.
- **Alternatives**: Always paged with defaults (breaks the 001 contract and about 6 integration
  tests; rejected). A separate `/page` route (two endpoints for one resource; rejected).

## R2 Response shape: additive fields, omitted when unpaged

- **Decision**: `RefillRequestListDto` gains nullable `page`, `pageSize`, `matchingCount`,
  `statusTotal`, and `summary` (`{ blocked, warnings, clear }`, Pending only). Each is marked
  `JsonIgnore(WhenWritingNull)`, the same pattern already used for `evaluation` on the detail
  DTO. In unpaged mode all of them are null, so they are not written.
- **Rationale**: Keeps the unpaged response identical (R1) with no second DTO type. The names
  match the spec's Key Entities: "matching" is the count after search, "status total" is the
  count for the whole status.
- **Alternatives**: A separate `PagedRefillRequestListDto` (duplicates `evaluatedAt`/`items`;
  rejected). An `X-Total-Count` header (hidden from typed clients and ProblemDetails tooling;
  rejected).

## R3 Pending: evaluate the whole status once, page by id

- **Decision**: For Pending in paged mode:
  1. Load all Pending requests with their evaluation facts and evaluate them at one instant.
     This is the existing code path, unchanged; it yields `statusTotal` and `summary`
     (spec FR-010, FR-011).
  2. Run a SQL query with the same status, the search predicate (R4), the established order,
     and `Skip/Take` to get the page's ids, plus a `COUNT` for `matchingCount`.
  3. Build the page items from step 1's evaluations, keyed by id, so flags and counts share one
     `evaluatedAt`.

  For Approved and Denied (no evaluation): `COUNT` for `statusTotal`, `COUNT` with search for
  `matchingCount`, and a `Skip/Take` page query with `Include`s. Nothing else is loaded.
- **Rationale**: Domain evaluation is C# and cannot run in SQL, and the summary covers the whole
  status, so every Pending request must be evaluated anyway (spec Assumptions). Using SQL for
  search and order in both branches keeps one definition of "matches" and "order". The decided
  lists are the ones that grow without limit, and they now load only one page.
- **Consistency note**: Steps 1 and 2 are separate reads. A decision committed between them can
  make one response's summary differ by one from its page. The `RequestUpdated` hint that
  follows that commit triggers a reload (spec FR-015), so the screen corrects itself. A snapshot
  transaction would need `ALLOW_SNAPSHOT_ISOLATION` (a database change) for an effect that
  corrects itself within a moment; rejected as disproportionate (constitution IV, last bullet).
- **Alternatives**: Search and page in memory for Pending and in SQL for decided (two search
  definitions that can drift; rejected). Storing evaluation flags (forbidden by 001 FR-028).

## R4 Search predicate

- **Decision**: A single EF Core expression in `GetRefillRequests`:
  `Contains(term)` on `Prescription.Patient.FullName` or `Prescription.Medication.Name`, each
  wrapped in `EF.Functions.Collate(..., "Latin1_General_100_CI_AS")`. The term is trimmed;
  empty means no filter. The provider escapes `%`, `_`, and `[` in the term, so they match
  literally (to be proven by a test).
- **Rationale**: Case-insensitive regardless of the server's or test database's default
  collation, so the result is deterministic (spec FR-007, acceptance scenario 2.5).
- **Alternatives**: `ToLower()` on both sides (works, but the explicit collation states the
  intent); full-text search (needs a catalog and index; out of proportion).
- **Revised during implementation (T022)**: `EF.Functions.Collate` is defined in the EF Core
  relational package, which the Application project does not reference, and adding it would be a
  new dependency (plan: none). The alternative above is used instead: both sides are lower-cased
  (`LOWER()` in SQL, `ToLowerInvariant()` for the term). It is equally independent of the
  database collation. `ListSearchTests` prove case, trim, and literal `%`, `_`, `[`.

## R5 Validation and binding

- **Decision**: The endpoint binds `page`, `pageSize`, and `search` as `string?` (like `status`)
  and parses them itself with invariant culture. The checks run in order `status`, `page`,
  `pageSize`, `search`; the first failure returns the existing
  `400 validation_failed` with `errors` naming that field (spec FR-004). The limits
  (`DefaultPageSize = 10`, `MaxPageSize = 100`, `MaxSearchLength = 100`) are constants on an
  Application `RefillRequestListQuery` record that the endpoint creates and the use case
  receives.
- **Rationale**: Binding as `int?` makes the framework reject `page=abc` with a body that does
  not name the field. Parsing the shape of the input belongs to the API; the limits are part of
  the list contract and are kept with the query type, not in the Domain (they are not refill
  rules).
- **Alternatives**: FluentValidation or data annotations (new dependency or attribute plumbing
  for four checks; rejected by constitution I).

## R6 Page beyond the last page

- **Decision**: The server returns `items: []` with correct `matchingCount`, `statusTotal`, and
  `summary`, and `200`. The client, on receiving no items while `matchingCount > 0` and
  `page > 1`, requests `ceil(matchingCount / pageSize)` once (spec FR-005, FR-014).
- **Rationale**: A pure server rule ("this page is empty"), and the client's existing
  clamp-to-last-page behavior is kept. Clamping on the server would make the response's `page`
  differ from the request's, which complicates the client and the tests.

## R7 Ordering and indexes

- **Decision**: Keep today's order with an id tie-break: Pending by `RequestedAt`, then `Id`;
  decided by `DecidedAt` descending, then `Id` descending. No new index and no migration.
- **Rationale**: The order is already total, so pages do not overlap while data is unchanged
  (spec FR-006, SC-002). SC-005 (1 s at 1,000 rows) is met by the existing `(Status,
  RequestedAt)` index plus a small sort; a `(Status, DecidedAt)` index is unneeded at demo scale
  and would add a migration (constitution IV: schema changes as migrations; keep in
  proportion). This is to be revisited only if the SC-005 timing check fails.

## R8 Search text and logging

- **Decision**: No application log statement includes the query. The project's configured
  `Microsoft.AspNetCore` level is `Warning`, so the hosting "Request starting … ?search=…" line
  (`Information`) is not written. EF Core sensitive-data logging stays off, so parameter values
  are not logged. An integration test captures all log output at the configured levels during
  a search and asserts that the search text never appears (spec FR-009).
- **Rationale**: Constitution VI: logs carry identifiers only.

## R9 Frontend request flow

- **Decision**: `RefillRequestsApi.list(status, query?)`, where `query` is
  `{ page, pageSize, search }`; params are sent only when the page asks for paging. The queue
  page always requests paged mode. Loads go through a single `Subject` piped with `switchMap`,
  so only the latest response is shown (spec FR-016). Search input is debounced by 300 ms;
  paging, page-size, status, Refresh, and live-update reloads are not. `counts()` and the status
  filter's `count` read `statusTotal` and `summary` from the response; the results heading reads
  `matchingCount`. `visibleItems`/`pagedItems` slicing is removed (spec FR-012).
- **Rationale**: Minimal change to a working page; the template hooks and texts stay, so the
  002 UI contract holds.
- **Test impact (intentional, equal or stronger coverage)**:
  - `refill-queue.redesign.spec.ts`: the search and pagination specs (`filters by patient or
    medication…`, `paginates the search result…`, `returns to page 1…`, `shows the last
    existing page…`, `keeps the search text…`, `shows a distinct empty state…`) are rewritten to
    assert the parameters sent to `api.list` and to render server-provided pages, totals, and
    summary.
  - `refill-queue.page.spec.ts`: assertions like `toHaveBeenCalledWith('Approved')` gain the
    query argument.
  - `refill-requests.api.spec.ts` keeps its no-parameter test and gains a paged-params test.

## R10 Documentation impact

- **Decision**: Update `specs/001-refill-request-review/contracts/http-api.md` (list endpoint:
  "Not paginated" → optional paging, with a link to this feature's contract), the 002 research
  R13 limitation (mark it delivered by 003), `SPEC.md` (API table, known limitations: remove the
  "not paginated" item), and `PROMPTS.md`.
