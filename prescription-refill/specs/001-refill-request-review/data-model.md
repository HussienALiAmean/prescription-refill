# Data Model: Prescription Refill Request Review

**Feature**: [spec.md](spec.md) | **Date**: 2026-10-20

Rules are referenced by spec ID and not redefined here. Database: SQL Server (constitution
Principle IV).

## Entities

### Patient

| Field | Type | Rules |
|-------|------|-------|
| Id | int, PK | |
| FullName | nvarchar(200) | required; fictional |
| DateOfBirth | date | required |

Relationships: has many Prescriptions, Allergies.

### Medication

| Field | Type | Rules |
|-------|------|-------|
| Id | int, PK | |
| Name | nvarchar(100) | required, unique; synthetic name (FR-019) |

### Prescription

| Field | Type | Rules |
|-------|------|-------|
| Id | int, PK | |
| PatientId | int, FK → Patient | required |
| MedicationId | int, FK → Medication | required |
| Dosage | nvarchar(200) | required; display text, e.g. "10 mg by mouth once daily" |
| DaysSupply | int | CHECK > 0 |
| RemainingRefills | int | CHECK >= 0; decremented only by approval (FR-012) |
| LastFilledAt | datetimeoffset(7) NOT NULL | UTC instant, stored with offset +00:00; never changed by decisions (FR-012) |
| IsActive | bit | required; seed-only, never changed by the application |
| RowVersion | rowversion | shadow property, concurrency token |

Domain behavior: `ConsumeRefill()` — rejects when RemainingRefills is 0 (FR-025). The CHECK
constraint is a second, database-level guard.

**Active medications** (FR-022): the medications of the patient's prescriptions with
`IsActive = 1`. The same query feeds interaction checks and the profile's active prescriptions
list; the requested prescription is excluded from both.

### Allergy

| Field | Type | Rules |
|-------|------|-------|
| Id | int, PK | |
| PatientId | int, FK → Patient | required |
| MedicationId | int, FK → Medication | required; unique with PatientId |

Allergy conflict (FR-008): `Allergy.MedicationId == requested Prescription.MedicationId`. No
other relationship is modeled, so nothing else matches.

### DrugInteraction

| Field | Type | Rules |
|-------|------|-------|
| Id | int, PK | |
| MedicationAId | int, FK → Medication | CHECK MedicationAId < MedicationBId |
| MedicationBId | int, FK → Medication | unique with MedicationAId |
| Description | nvarchar(300) | required; marked synthetic, e.g. "Synthetic demo interaction" |

Storing the pair in ascending order makes the lookup order-independent; the CHECK also rules
out a medication paired with itself.

### RefillRequest

| Field | Type | Rules |
|-------|------|-------|
| Id | int, PK | |
| PrescriptionId | int, FK → Prescription | required |
| Status | nvarchar(16) | `Pending` \| `Approved` \| `Denied` |
| RequestedAt | datetimeoffset(7) | required |
| DecidedAt | datetimeoffset(7) NULL | set on decision |
| DecisionNote | nvarchar(1000) NULL | set on decision, stored trimmed (FR-010) |
| RowVersion | rowversion | shadow property, concurrency token |

Indexes: filtered unique `UX_RefillRequests_Pending` on (PrescriptionId) WHERE
`Status = N'Pending'` (FR-003); non-unique (Status, RequestedAt) for the queue (FR-018).

All foreign keys use `Restrict` on delete.

## Time Storage

- All instants (`LastFilledAt`, `RequestedAt`, `DecidedAt`) are `datetimeoffset(7)` (100 ns =
  1 .NET tick, so values round-trip exactly). An EF Core value converter normalizes every value
  to offset `+00:00` on save; values are read back as UTC. Serialization: see
  [contracts/http-api.md](contracts/http-api.md#conventions).
- `DateOfBirth` is `date` (`DateOnly` in .NET); it is never converted between time zones.
- "Now" always comes from the injected `TimeProvider`; Domain code never reads
  `DateTime.UtcNow` or `DateTimeOffset.UtcNow`.

## State Machine (FR-010, FR-014, FR-015)

```text
            approve(note) [no blocking reasons]
 Pending ───────────────────────────────────────▶ Approved   (terminal)
    │
    └──── deny(note) [always allowed] ─────────▶ Denied     (terminal)
```

Any decision on Approved/Denied → "already decided"; no field changes. A blocked approval leaves
the request Pending.

## Evaluation (computed, not stored)

Produced by Domain from: the requested prescription, the patient's allergies, the patient's
other active prescriptions, seeded interactions, and `now` from `TimeProvider`.

Field shapes and codes: [contracts/http-api.md](contracts/http-api.md#evaluation-object-fr-005fr-009-fr-027).
One Domain evaluator produces it for list/detail reads and for approval processing (FR-006).

Calculation (all integer ticks, no floating point):

```text
threshold  = DaysSupply × 691,200,000,000 ticks      (0.8 × 24 h = 19.2 h per supply day)
elapsed    = now − LastFilledAt                      (both UTC)
invalid    = DaysSupply <= 0  or  LastFilledAt missing  or  LastFilledAt > now
TOO_EARLY  = not invalid and elapsed < threshold     (equality is NOT blocked)
eligibleAt = LastFilledAt + threshold                (null when invalid)
```

`invalid` yields `INVALID_PRESCRIPTION_DATA` instead of `TOO_EARLY` (FR-027). Database
constraints (NOT NULL, `DaysSupply > 0`) prevent the first two through the application; the
evaluator still checks them so bad data can never allow approval.

## Transactions and Concurrency (FR-013, FR-016, FR-025)

| Operation | Rows written in one transaction | Concurrency check | On conflict |
|-----------|---------------------------------|-------------------|-------------|
| Approve | RefillRequest (status, decision), Prescription (RemainingRefills − 1) | both `RowVersion` values must match what was loaded | whole transaction rolled back; `409 concurrency_conflict` |
| Deny | RefillRequest only | request `RowVersion` | rolled back; `409 concurrency_conflict` |
| Create | RefillRequest insert | filtered unique index | rolled back; `409 pending_request_exists` |

Guards against over-consumption, outermost last: Domain `ConsumeRefill()` → prescription
`RowVersion` → CHECK `RemainingRefills >= 0`.

## Not Stored or Cached

- `RequestUpdated` notifications are transient (FR-029); nothing about them is persisted.
- Request status, remaining refills, and eligibility are always read from SQL Server; no
  in-memory or distributed cache holds them (FR-028, research R14).

## Seed Scenarios (FR-019)

All patients and medications are fictional; medication names are invented and every
interaction pair is synthetic (research R11). Times are relative to `now` at seed time.

**Synthetic interaction pairs (4)**: Avelorin + Corvantil; Fenquira + Glimora;
Halvexin + Jurvane; Brimexa + Kestrolin (no patient has both; shows a non-matching pair).

| # | Patient | Requested prescription | Other data | Expected review |
|---|---------|------------------------|------------|-----------------|
| 1 | Avery Lindqvist | Avelorin, 30 d, filled now−26 d, 3 refills | inactive Corvantil | Approvable, no warnings (inactive pair does not warn) |
| 2 | Jordan Okafor | Brimexa, 30 d, filled now−10 d, 2 refills | — | Blocked: TOO_EARLY (eligible at filled + 24 d) |
| 3 | Morgan Reyes | Dexolune, 90 d, filled now−80 d, 0 refills | — | Blocked: NO_REFILLS_REMAINING |
| 4 | Riley Chen | Elvadine, 10 d, filled now−9 d, 1 refill | allergy: Elvadine | Approvable with ALLERGY_CONFLICT |
| 5 | Sam Patel | Fenquira, 30 d, filled now−27 d, 5 refills | active Glimora | Approvable with DRUG_INTERACTION |
| 6 | Taylor Nguyen | Halvexin, 30 d, filled now−5 d, 2 refills | active Jurvane | Blocked: TOO_EARLY + DRUG_INTERACTION |
| 7 | Avery Lindqvist | earlier Avelorin request | — | Approved, read-only |
| 8 | Morgan Reyes | earlier Dexolune request | — | Denied, read-only |

Inactive prescription for API tests (FR-021): Jordan Okafor, Corvantil, inactive, no request.

Scenarios 1–6 are Pending; 7–8 are decided requests on the same prescriptions as 1 and 3 (the
filtered index allows this because they are not Pending).
