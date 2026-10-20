# Quickstart Validation: Server-Side Pagination

Validation scenarios that prove the feature end to end. Contract:
[contracts/http-api.md](contracts/http-api.md). Shapes: [data-model.md](data-model.md).

## Prerequisites

As in the [README](../../README.md): a .NET SDK that builds `net10.0`, SQL Server, Node 22.
**Use a throwaway database**, never the existing dev database:

```bash
export ConnectionStrings__RefillDb="Server=.;Database=RefillPaging_Check;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src/PrescriptionRefill.Api -- --seed
dotnet run --project src/PrescriptionRefill.Api            # http://localhost:5080
```

Drop `RefillPaging_Check` when finished.

## Automated

```bash
dotnet build PrescriptionRefill.slnx       # 0 warnings
dotnet test PrescriptionRefill.slnx        # all Domain + SQL Server integration tests pass
cd frontend && npm run test:ci && npm run build
```

## API scenarios (seeded data: 6 Pending, 1 Approved, 1 Denied)

| # | Request | Expected |
|---|---------|----------|
| Q1 | `GET /api/refill-requests` | Unchanged 001 response: 6 items, only `evaluatedAt` and `items` keys |
| Q2 | `GET /api/refill-requests?page=1&pageSize=4` | 4 items (seed scenarios 1–4 in order), `matchingCount` 6, `statusTotal` 6, `summary` `{blocked:3, warnings:3, clear:1}` |
| Q3 | `?page=2&pageSize=4` | 2 items (scenarios 5–6); together with Q2 every id exactly once |
| Q4 | `?page=3&pageSize=4` | `200`, `items: []`, totals as Q2 |
| Q5 | `?search=OKAFOR` | Paged (page 1, size 10): 1 item (Jordan Okafor); `matchingCount` 1, `statusTotal` 6, `summary` unchanged from Q2 |
| Q6 | `?search=%20%20brim%20` | Same as Q5 (trim and case) |
| Q7 | `?search=%25` (a literal `%`) | `items: []`, `matchingCount` 0 (no wildcard match) |
| Q8 | `?status=Approved&page=1` | 1 item, `statusTotal` 1, no `summary` key |
| Q9 | `?page=0`, `?pageSize=101`, `?page=abc`, 101-char `search` | `400 validation_failed`, `errors` names `page` / `pageSize` / `page` / `search` |
| Q10 | Create 15 requests through the API, then `?pageSize=10&page=2` | Correct remainder; Pending order kept by `requestedAt` then `id` |
| Q11 | 1,000 Pending requests (test-generated), `?page=1&pageSize=10` and `?search=…` | Each response within 1 s on the dev machine (SC-005); 10 items delivered (SC-001) |

## Screen scenarios (`npm start`, http://localhost:4200)

| # | Action | Expected |
|---|--------|----------|
| S1 | Open the queue | First page, "Showing 1–n of N", summary counts exact |
| S2 | Next/Previous, change rows per page | One API call per action carrying `page`/`pageSize`; focus moves to the results heading |
| S3 | Type a search matching an item that is not on page 1 | Found after the debounce; heading shows "n of N requests match" |
| S4 | Type quickly | Only the final text's results are shown |
| S5 | On the last page, decide its only request in a second tab | First tab shows the new last page; search and page size kept |
| S6 | Stop the API, Refresh | Existing error state with retry |

## Privacy check

Run a search for `Okafor`, then search the API console output for it: no occurrences
(spec FR-009). The automated log-capture test asserts the same.

## Baseline (T001, 2026-10-20, before any 003 code change)

| Command | Result |
|---------|--------|
| `dotnet build PrescriptionRefill.slnx` | 0 warnings, 0 errors |
| `dotnet test PrescriptionRefill.slnx` | Domain 47/47, integration 61/61 |
| `npm run test:ci` | 89/89 |
| `npm run build` | OK, initial 413.32 kB |
