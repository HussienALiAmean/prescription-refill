# HTTP Contract Change: Paged Request List

This changes **one endpoint** of
[001 contracts/http-api.md](../../001-refill-request-review/contracts/http-api.md). Every other
endpoint, error code, the evaluation object, and live updates are unchanged.

## `GET /api/refill-requests`

### Query parameters

| Name | Required | Values | Default |
|------|----------|--------|---------|
| `status` | no | `Pending` \| `Approved` \| `Denied` (case-insensitive, as today) | `Pending` |
| `page` | no | whole number ≥ 1 | `1` in paged mode |
| `pageSize` | no | whole number 1–100 | `10` in paged mode |
| `search` | no | text, at most 100 characters after trimming; matched case-insensitively as "contains" against patient name or medication name; empty or whitespace means no search | none |

**Mode**: if `page`, `pageSize`, and `search` are all absent, the request is **unpaged** and the
response is exactly the 001 response (full list, `{ evaluatedAt, items }`). If any of them is
present, the request is **paged**.

### Ordering (both modes, unchanged)

Pending: `requestedAt` ascending, then `id` ascending. Approved/Denied: `decidedAt` descending,
then `id` descending.

### `200` paged response

```json
{
  "evaluatedAt": "2026-10-20T11:00:00.1234567+00:00",
  "items": [
    { "id": 4, "status": "Pending", "requestedAt": "2026-10-19T15:00:00+00:00",
      "decidedAt": null, "patientName": "Jordan Okafor", "medicationName": "Brimexa",
      "canApprove": false, "hasWarnings": false }
  ],
  "page": 2,
  "pageSize": 1,
  "matchingCount": 6,
  "statusTotal": 6,
  "summary": { "blocked": 3, "warnings": 3, "clear": 1 }
}
```

- `items`: this page only; `[]` when `page` is beyond the last page (still `200`).
- `matchingCount`: requests of this status matching `search` (equals `statusTotal` without
  search). Clients compute `pageCount = max(1, ceil(matchingCount / pageSize))`.
- `statusTotal`: all requests of this status, ignoring `search`.
- `summary`: Pending only, over all Pending requests, ignoring `search`; omitted for
  Approved/Denied. Same evaluation instant as `evaluatedAt` and the items' flags.
- `canApprove`/`hasWarnings`: as in 001 (Pending computed at `evaluatedAt`; `null` when decided).

The example uses the six seeded Pending requests (001 data-model seed scenarios 1–6): 3 blocked
(2, 3, 6), 3 with warnings (4, 5, 6), 1 clear (1).

### Errors

Only `400 validation_failed`, with `errors` naming the first invalid field, checked in order
`status`, `page`, `pageSize`, `search`:

| Input | `errors` |
|-------|----------|
| `status=Closed` | `{ "status": ["Status must be Pending, Approved, or Denied."] }` (unchanged) |
| `page=0`, `page=-1`, `page=abc`, `page=1.5` | `{ "page": ["Page must be a whole number of at least 1."] }` |
| `pageSize=0`, `pageSize=101`, `pageSize=abc` | `{ "pageSize": ["Page size must be a whole number from 1 to 100."] }` |
| `search` longer than 100 characters after trimming | `{ "search": ["Search must be at most 100 characters."] }` |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The request is not valid.",
  "status": 400,
  "code": "validation_failed",
  "errors": { "pageSize": ["Page size must be a whole number from 1 to 100."] }
}
```

### Privacy

The `search` value can contain patient names. The server never writes it to logs
(constitution VI).

## Client handling (queue screen)

| Situation | Behavior |
|-----------|----------|
| Open, change status, change page size, search text changes | Request page 1 (search debounced 300 ms) |
| Previous/Next | Request that page |
| `RequestUpdated` hint, reconnect, tab visible again, Refresh | Request the current page with the current search and page size |
| `items` empty, `matchingCount > 0`, `page > 1` | Request page `ceil(matchingCount / pageSize)` once |
| A newer request was sent before an older one answered | Show only the newest response |
| `400`, `5xx`, network error | Existing error state with retry |
