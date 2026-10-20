# HTTP and Live-Update Contract

**Feature**: [../spec.md](../spec.md) | **Base path**: `/api` | **Format**: JSON, camelCase

No authentication (FR-020). Responses are DTOs; persistence fields such as `rowVersion` never
appear (Principle VI). There are no update or delete endpoints for refill requests (FR-015).

## Conventions

- **Instants** (`requestedAt`, `decidedAt`, `lastFilledAt`, `evaluatedAt`, `eligibleAt`): ISO
  8601 with offset `+00:00`, always UTC, e.g. `2026-10-20T11:05:12.3456789+00:00`. Fractional
  seconds have up to 7 digits and are omitted when zero (`2026-10-15T09:00:00+00:00`).
  Clients format them for display (FR-024).
- **Dates** (`dateOfBirth`): `yyyy-MM-dd`, no time or offset; never converted between zones.
- **No client-supplied times**: requests carry no timestamps; the server clock is
  authoritative (FR-006, FR-028).

## Evaluation object (FR-005–FR-009, FR-027)

Returned in request details while Pending, and in `422 approval_blocked`.

| Field | Type | Meaning |
|-------|------|---------|
| `evaluatedAt` | instant | Server time the evaluation was computed. Clients show it with a "may be out of date" notice (FR-005) |
| `canApprove` | boolean | `true` when `blockingReasons` is empty |
| `eligibleAt` | instant \| null | `lastFilledAt + daysSupply × 19.2 h`; `null` when timing data is invalid |
| `blockingReasons` | `BlockingReason[]` | Empty or more entries; order: `INVALID_PRESCRIPTION_DATA` or `TOO_EARLY`, then `NO_REFILLS_REMAINING` |
| `warnings` | `Warning[]` | Never affect `canApprove` |

`BlockingReason` = `{ "code", "message", "eligibleAt" }` — `eligibleAt` is set only for
`TOO_EARLY`, otherwise `null`.

| `code` | When |
|--------|------|
| `TOO_EARLY` | `evaluatedAt − lastFilledAt` < `daysSupply × 0.8 days` (FR-006) |
| `NO_REFILLS_REMAINING` | `remainingRefills` is 0 (FR-007) |
| `INVALID_PRESCRIPTION_DATA` | `lastFilledAt` missing or later than `evaluatedAt`, or `daysSupply` not positive; replaces `TOO_EARLY` (FR-027) |

`Warning` = `{ "code", "message", "medicationName", "description" }`.

| `code` | `medicationName` | `description` |
|--------|------------------|---------------|
| `ALLERGY_CONFLICT` | the allergen (= requested medication) | `null` |
| `DRUG_INTERACTION` | the interacting active medication | synthetic interaction text |

## Endpoints

### `GET /api/refill-requests?status={Pending|Approved|Denied}`

Queue and history (FR-018). `status` defaults to `Pending`. Pending is sorted by
`requestedAt` ascending; Approved and Denied by `decidedAt` descending. Optional paging and
search: see [003 contracts/http-api.md](../../003-backend-pagination/contracts/http-api.md);
without them the response below is unchanged.

- `200` → `RefillRequestList`
- `400 validation_failed` for an unknown status

```json
{
  "evaluatedAt": "2026-10-20T11:00:00.1234567+00:00",
  "items": [
    { "id": 1, "status": "Pending", "requestedAt": "2026-10-18T07:30:00+00:00",
      "decidedAt": null, "patientName": "Avery Lindqvist", "medicationName": "Avelorin",
      "canApprove": true, "hasWarnings": false }
  ]
}
```

`canApprove` and `hasWarnings` are computed at `evaluatedAt` for Pending items and are `null`
for decided items.

### `GET /api/refill-requests/{id}`

- `200` → `RefillRequestDetail`
- `404 not_found`

Example (seed scenario 5, Pending, approvable with a warning):

```json
{
  "id": 5, "status": "Pending", "requestedAt": "2026-10-19T16:00:00+00:00",
  "decidedAt": null, "decisionNote": null,
  "patient": { "id": 5, "fullName": "Sam Patel", "dateOfBirth": "1979-11-02",
               "allergies": [] },
  "prescription": { "id": 7, "medicationName": "Fenquira",
                    "dosage": "10 mg by mouth once daily", "daysSupply": 30,
                    "remainingRefills": 5, "lastFilledAt": "2026-09-23T08:30:00+00:00",
                    "isActive": true },
  "activePrescriptions": [
    { "id": 8, "medicationName": "Glimora", "dosage": "5 mg by mouth once daily",
      "remainingRefills": 2, "lastFilledAt": "2026-10-01T08:30:00+00:00" }
  ],
  "evaluation": {
    "evaluatedAt": "2026-10-20T11:00:00.1234567+00:00",
    "canApprove": true,
    "eligibleAt": "2026-10-17T08:30:00+00:00",
    "blockingReasons": [],
    "warnings": [
      { "code": "DRUG_INTERACTION",
        "message": "Fenquira has a synthetic demo interaction with active medication Glimora.",
        "medicationName": "Glimora",
        "description": "Synthetic demo interaction (fictional, not clinical guidance)." }
    ]
  }
}
```

- `evaluation` is present only while Pending; `decidedAt` and `decisionNote` are set only when
  decided.
- `activePrescriptions` lists the patient's other prescriptions with `isActive = true`
  (FR-022); it is the same set used for interaction checks.
- Clients enable Approve only when `evaluation.canApprove` is `true` (FR-023). This is a
  convenience; the server re-evaluates on approve (FR-011).

### `POST /api/refill-requests/{id}/approve`

Body `{ "note": "string" }`. The server re-evaluates at processing time (FR-011), then commits
the decision and the refill decrement in one transaction (FR-013).

- `200` → `RefillRequestDetail` (Approved, no `evaluation`)
- `422 approval_blocked` → current evaluation (example below)
- `400 validation_failed`, `404 not_found`, `409 request_already_decided`,
  `409 concurrency_conflict`

### `POST /api/refill-requests/{id}/deny`

Body `{ "note": "string" }`. Always allowed for Pending (FR-014); refills unchanged (FR-012).

- `200` → `RefillRequestDetail` (Denied)
- `400 validation_failed`, `404 not_found`, `409 request_already_decided`,
  `409 concurrency_conflict`

### `POST /api/refill-requests`

Body `{ "prescriptionId": 1 }` (FR-001–FR-004, FR-021). API only; no UI calls it.

- `201` → `RefillRequestDetail` with `Location` header
- `400 validation_failed` — malformed body, or inactive prescription:
  `"errors": { "prescriptionId": ["Prescription is inactive."] }`
- `404 not_found` (prescription), `409 pending_request_exists`

Checked in order: `400` (body) → `404` → `400` (inactive) → `409 pending_request_exists`.

## Errors (FR-017)

ProblemDetails (RFC 9457) with a stable `code` extension member. Clients MUST branch on `code`,
not on HTTP status alone (three codes share 409).

| Status | `code` | When | Extra members |
|--------|--------|------|---------------|
| 400 | `validation_failed` | malformed body; note empty/whitespace/over 1,000 chars; inactive prescription on create; bad query | `errors` (field → messages) |
| 404 | `not_found` | request or prescription does not exist | |
| 409 | `request_already_decided` | decision on an Approved/Denied request | `requestId`, `requestStatus` |
| 409 | `pending_request_exists` | second Pending request for a prescription | `prescriptionId` |
| 409 | `concurrency_conflict` | the request or its prescription changed after it was loaded; nothing was saved | `requestId` |
| 422 | `approval_blocked` | a blocking rule applies at processing time | `requestId`, `evaluation` |
| 500 | `unexpected_error` | unhandled failure; no internal details | |

Decision endpoints check in this order and return the first failure: `404` → `400` (body/note)
→ `409 request_already_decided` → `422 approval_blocked` → `409 concurrency_conflict` (at
commit).

### `422 approval_blocked` example (seed scenario 6)

```json
{
  "title": "Approval is blocked.",
  "status": 422,
  "detail": "The request cannot be approved based on current data. It can still be denied.",
  "code": "approval_blocked",
  "requestId": 6,
  "evaluation": {
    "evaluatedAt": "2026-10-20T11:05:12.3456789+00:00",
    "canApprove": false,
    "eligibleAt": "2026-11-08T09:00:00+00:00",
    "blockingReasons": [
      { "code": "TOO_EARLY",
        "message": "Less than 80% of the 30-day supply has elapsed since the last fill.",
        "eligibleAt": "2026-11-08T09:00:00+00:00" }
    ],
    "warnings": [
      { "code": "DRUG_INTERACTION",
        "message": "Halvexin has a synthetic demo interaction with active medication Jurvane.",
        "medicationName": "Jurvane",
        "description": "Synthetic demo interaction (fictional, not clinical guidance)." }
    ]
  }
}
```

Here `lastFilledAt` is `2026-10-15T09:00:00+00:00` and `daysSupply` is 30, so the threshold is
24 days and `eligibleAt` is `2026-11-08T09:00:00+00:00`.

### `409 concurrency_conflict` example

```json
{
  "title": "The request changed while your decision was being saved.",
  "status": 409,
  "detail": "No changes were saved. Reload the request and decide again.",
  "code": "concurrency_conflict",
  "requestId": 6
}
```

The response is sent only after the transaction has been rolled back; neither the request nor
the prescription contains any part of the attempted decision (FR-013).

### `409 request_already_decided` example

```json
{
  "title": "The request has already been decided.",
  "status": 409,
  "detail": "Decided requests cannot be changed.",
  "code": "request_already_decided",
  "requestId": 6,
  "requestStatus": "Denied"
}
```

`requestStatus` is used because `status` is the ProblemDetails HTTP status.

## Client handling (FR-011, FR-023, FR-032)

| Response | Client behavior |
|----------|-----------------|
| `200` decision | Show success; render the returned decided, read-only request |
| `422 approval_blocked` | Replace the displayed evaluation with `evaluation`; Approve unavailable; Deny available; keep the note |
| `409 concurrency_conflict` | Do not show success; explain the data changed at the same time; reload the request (with prescription and evaluation); keep the note |
| `409 request_already_decided` | Do not show success; explain it was already decided; reload and show read-only |
| `400 validation_failed` | Show field messages inline; nothing changed |
| `404`, `500`, network error | Error state with retry; no success shown |

## Live updates (FR-029–FR-031)

ASP.NET Core SignalR hub at **`/hubs/refill-requests`**. Unauthenticated demo channel: not
production-secure and not suitable for real patient data (FR-020).

- **Direction**: server → client only. The hub exposes no client-callable methods, so it cannot
  change data; all changes go through the HTTP API above.
- **Audience**: every connected client (broadcast). No groups and no user identity.
- **Event** `RequestUpdated`:

  ```json
  { "requestId": 6, "change": "decided" }
  ```

  `change` is `created` or `decided`. No patient, medication, status, or note data.
- **When**: after the database transaction commits, for successful create, approve, and deny
  only. Never for `4xx` outcomes or rolled-back transactions.
- **Delivery**: best effort, at most once, unordered. Delivery failures are logged with
  identifiers only and do not change the HTTP response (FR-031).
- **Client reaction**: queue screens reload the list; a review screen reloads only when
  `requestId` matches the open request. Clients reload on reconnect and when the screen becomes
  active again. Events are hints, never state.
