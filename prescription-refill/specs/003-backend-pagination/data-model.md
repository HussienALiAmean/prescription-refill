# Data Model: Server-Side Pagination of Refill Request Lists

**No stored data changes.** No entity, column, index, constraint, or migration is added
(research R7). The persisted model stays as defined in
[001 data-model.md](../001-refill-request-review/data-model.md). This feature adds one query
type and extends one response type.

## RefillRequestListQuery (Application, input)

Created by the endpoint after parsing (research R5); received by `GetRefillRequests`.

| Field | Type | Rules |
|-------|------|-------|
| Status | `RefillRequestStatus` | as today; absent means `Pending` |
| Page | int? | null in unpaged mode; otherwise ≥ 1; defaults to 1 in paged mode |
| PageSize | int? | null in unpaged mode; otherwise 1–100 (`MaxPageSize`); defaults to 10 (`DefaultPageSize`) |
| Search | string? | trimmed; null when empty or whitespace-only; at most 100 characters (`MaxSearchLength`) after trimming |
| IsPaged | bool (derived) | true when the request had any of `page`, `pageSize`, `search` (research R1) |

Validation order and error: `status` → `page` → `pageSize` → `search`; the first failure is a
`400 validation_failed` naming that field. The search text is never logged.

## RefillRequestListDto (response, extended)

| Field | Type | Unpaged | Paged |
|-------|------|---------|-------|
| `evaluatedAt` | instant | as today | as today: the single instant at which every flag and count was computed |
| `items` | `RefillRequestSummaryDto[]` | full list, as today | only this page, in the established order (may be empty past the last page) |
| `page` | int | omitted | the page requested |
| `pageSize` | int | omitted | the page size used |
| `matchingCount` | int | omitted | requests of this status that match the search (= `statusTotal` without search) |
| `statusTotal` | int | omitted | all requests of this status, ignoring search |
| `summary` | `ListSummaryDto` | omitted | Pending only; omitted for Approved and Denied |

Omitted means not written to JSON (`JsonIgnore(WhenWritingNull)`), so unpaged responses are
unchanged (spec FR-002).

## ListSummaryDto (response, new; Pending only)

Counts over **all** Pending requests, ignoring search, from the same evaluations as the items
(spec FR-010, FR-011).

| Field | Type | Meaning |
|-------|------|---------|
| `blocked` | int | requests whose evaluation has at least one blocking reason (`canApprove = false`) |
| `warnings` | int | requests with at least one warning (`hasWarnings = true`); can overlap with `blocked` |
| `clear` | int | `canApprove = true` and no warnings |

Invariants (asserted by tests): `blocked + clear ≤ statusTotal`; `clear + warnings ≥ statusTotal
− blocked`; and with search, `matchingCount ≤ statusTotal`.

## RefillRequestSummaryDto

Unchanged (001 contract).

## Ordering (unchanged, total)

| Status | Order |
|--------|-------|
| Pending | `RequestedAt` ascending, then `Id` ascending |
| Approved, Denied | `DecidedAt` descending, then `Id` descending |

## Derived values the client computes (display only, never rules)

`pageCount = max(1, ceil(matchingCount / pageSize))`; "Showing x–y of N" from `page`,
`pageSize`, `items.length`, `matchingCount`.
