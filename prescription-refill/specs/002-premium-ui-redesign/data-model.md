# Data Model: Premium Clinical UI Redesign

**Feature**: [spec.md](spec.md) | **Date**: 2026-10-20

No server, API, or stored-data changes. All entities and DTOs are those of
[001 data-model](../001-refill-request-review/data-model.md) and
`frontend/src/app/features/refill-requests/data/models.ts`, unchanged.

## View state added on the client (presentation only)

| Owner | State | Type | Derived from | Notes |
|-------|-------|------|--------------|-------|
| `RefillQueuePage` | `query` | `signal<string>` | user input | Trimmed, case-insensitive; persists across reloads and status changes. |
| `RefillQueuePage` | `visibleItems` | `computed<RefillRequestSummary[]>` | `list().items`, `query` | Preserves server order. |
| `RefillQueuePage` | `counts` | `computed<{ total; blocked; warnings }>` | `list().items` | `blocked`: `canApprove === false`; `warnings`: `hasWarnings === true`. Pending view only. |
| `RefillQueuePage` | `page`, `pageSize` | `signal<number>` | user input | `pageSize` ∈ {10, 25, 50}, default 10; `page` reset to 1 on search/status/size change. |
| `RefillQueuePage` | `pageCount`, `currentPage`, `pagedItems` | `computed` | `visibleItems`, `page`, `pageSize` | `currentPage` = `min(page, pageCount)`; `pagedItems` is a contiguous slice of `visibleItems`. |
| `RefillQueuePage` | `reloading` | `signal<boolean>` | load lifecycle | True during background reloads; drives Refresh progress. |
| `DecisionFormComponent` | `pending` | `input<DecisionKind \| null>` | page `submitting` + last kind | Which button shows progress. |
| `DecisionFormComponent` | `noteLength` | `computed<number>` | `note` | Display only; the 1,000 rule stays on the server. |

None of these evaluate eligibility or alter what is sent to the API.
