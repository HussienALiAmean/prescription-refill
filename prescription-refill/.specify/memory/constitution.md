<!--
Sync Impact Report
==================
Version change: 3.0.0 → 3.0.1 (PATCH: U9 resolved by recording a choice; no rule added,
removed, or redefined)
Modified principles: none
Modified sections: Unresolved Decisions: U9 marked resolved (spec FR-012, FR-025, FR-026)
Added/removed sections: none
Templates: not modified by this command.
Follow-up TODOs: none; no open decisions remain.

Previous amendment (3.0.0)
--------------------------
Version change: 2.2.0 → 3.0.0 (MAJOR: the too-early blocking rule is redefined from request
time to decision time, per U3)
Modified principles:
  - II. Deterministic Server-Side Business Rules: too-early rule evaluated at decision time
    against the last filled time; precise calculation delegated to the feature spec
Modified sections: Unresolved Decisions: U2–U8 resolved with pointers to
  specs/001-refill-request-review/spec.md; U9 added (repeat approvals within a supply period)
Added/removed sections: none
Templates: not modified by this command.
Follow-up TODOs: U9 open.

Previous amendment (2.2.0)
--------------------------
Version change: 2.1.0 → 2.2.0 (MINOR: U1 resolved to SQL Server with added persistence,
concurrency, and test rules; no principle or business rule removed or redefined)
Modified principles:
  - IV. Persistence & Concurrency: backend stack fixed (ASP.NET Core, EF Core, SQL Server
    provider); rowversion tokens on request and prescription; atomic approval commit;
    no automatic retry of conflicts
  - V. Test Evidence: SQL Server for persistence/concurrency tests, schema via migrations,
    unavailable database reported as not run
Modified sections: Unresolved Decisions: U1 marked resolved; U6 cross-reference kept
Added/removed sections: none
Templates: not modified by this command.
Follow-up TODOs: U2–U8 remain open.

Previous amendment (2.1.0)
--------------------------
Version change: 2.0.0 → 2.1.0 (MINOR: enforceable rules added; no principle or business rule
removed or redefined)
Modified principles:
  - I. Pragmatic Clean Architecture: Domain owns rule evaluation and state transitions;
    Application orchestrates (review G2)
  - II. Deterministic Server-Side Business Rules: frontend displays server evaluation only (C1)
  - III. Workflow Integrity: decided requests rejected by Domain behavior (G2); distinct
    documented error categories (G5)
  - IV. Persistence & Concurrency: protection extended to prescription refill state (G1)
  - V. Test Evidence: relational provider required, InMemory excluded (G3); deterministic
    concurrency tests (G4)
  - VI. Security & Data Handling: identifier-only logging (E1)
  - VII. Frontend Quality: per-view state requirements (E2); measurable widths (E3)
Modified sections: Development Workflow & AI-Assisted Development: prompt logging scope (E4);
  Unresolved Decisions: U6 notes its interaction with Principle IV
Added/removed sections: none
Templates: not modified by this command.
Follow-up TODOs: Unresolved Decisions U1–U8 remain open.

Previous amendment
------------------
Version change: 1.0.0 → 2.0.0 (MAJOR: all principles redefined from the author's stated
engineering priorities; previous inferred draft replaced)
Modified principles:
  - I. Patient Safety First → II. Deterministic Server-Side Business Rules
                              + III. Workflow Integrity
  - II. Health Data Privacy & Security → VI. Security & Data Handling (narrowed to stated scope)
  - III. Test-First, Verified Behavior → V. Test Evidence
  - IV. Auditability & Observability → removed (not a stated requirement)
  - V. Simplicity & Accessible Design → I. Pragmatic Clean Architecture
                                        + VII. Frontend Quality
Added principles: IV. Persistence & Concurrency
Added sections: Delivery Constraints; Development Workflow & AI-Assisted Development;
  Unresolved Decisions
Removed sections: Security & Compliance Requirements (regulatory/MFA/backup rules dropped as
  out of scope for a take-home challenge with fictional data)
Templates: not modified by this command.
Follow-up TODOs: see "Unresolved Decisions" (U1–U8).
-->

# Prescription Refill Requests Constitution

## Core Principles

### I. Pragmatic Clean Architecture

- The backend MUST be a single deployable modular monolith with four layers: Domain,
  Application, Infrastructure, and API.
- Dependencies MUST point inward: Domain depends on nothing; Application depends only on
  Domain; Infrastructure and API depend on Application/Domain. EF Core and HTTP concerns MUST
  NOT appear in Domain.
- Safety-rule evaluation and state transitions MUST be implemented in Domain. Application MAY
  orchestrate use cases, load the required data, supply the clock, and coordinate persistence,
  but MUST NOT contain rule logic. Each rule exists in exactly one place; controllers and the
  frontend MUST NOT re-implement it.
- Abstractions (interfaces, generic repositories, mediators, extra services) MUST have a current
  consumer or test need; speculative abstractions are rejected. Microservices MUST NOT be
  introduced without a documented requirement that a monolith cannot meet.

### II. Deterministic Server-Side Business Rules

- All refill safety checks MUST run on the server. The frontend MUST NOT evaluate rules; it
  displays the evaluation returned by the server.
- Rule evaluation MUST be deterministic: identical data and identical clock value produce an
  identical result. The current time MUST come from an injectable clock, never read directly.
- **Blocking rules** (approval MUST be rejected):
  - The prescription has zero remaining refills.
  - At decision time, less than 80% of the prescription's supply period has elapsed since it
    was last filled (exact calculation: feature spec FR-006).
- **Warnings** (MUST be reported, MUST NOT block approval):
  - An allergy conflict for the patient.
  - A drug interaction with the patient's other medications.
- Every evaluation result MUST identify each triggered rule so the API and UI can explain it.

### III. Workflow Integrity

- A request MUST be created in the Pending state.
- The only permitted transitions are Pending → Approved and Pending → Denied.
- Every decision MUST include a note that is non-empty after trimming whitespace.
- Approval MUST re-evaluate all rules against current persisted data inside the same unit of
  work that commits the decision; a blocking result rejects the approval.
- A decided (Approved or Denied) request MUST reject any further state change through Domain
  behavior, not merely through the absence of API endpoints; the application API MUST NOT
  modify it. Repeated or conflicting decisions MUST be rejected, not ignored.
- Validation failure, blocking-rule rejection, invalid state transition, concurrency conflict,
  and not-found MUST each map to a distinct, documented API error.

### IV. Persistence & Concurrency

- The backend MUST use ASP.NET Core; the database MUST be Microsoft SQL Server, accessed through
  EF Core with the SQL Server provider (`Microsoft.EntityFrameworkCore.SqlServer`).
- Persistence MUST use Entity Framework Core with an explicit configuration class per entity
  (keys, required fields, lengths, relationships, delete behavior); no reliance on unstated
  conventions for these.
- Schema changes MUST be delivered as committed EF Core migrations.
- Concurrent decisions on the same request MUST be safe: at most one decision commits, and the
  others fail with a conflict response.
- Conflicting approvals MUST NOT consume the same remaining refill: concurrency protection MUST
  cover the request decision and, as needed, the prescription refill state the blocking rules
  read.
- The refill request and the prescription MUST each carry a SQL Server `rowversion` concurrency
  token, mapped in Infrastructure so Domain stays persistence-free. A token on the request alone
  MUST NOT be treated as protecting prescription refill state.
- An approval MUST commit the request decision and any prescription refill-state change in one
  database transaction. If any concurrency check fails, no change persists and the API returns
  a concurrency conflict.
- Concurrency conflicts MUST NOT be retried automatically; the caller reloads and decides again.
- The mechanism MUST stay proportional to the project: no distributed locks, queues, or custom
  locking services.

### V. Test Evidence

- Unit tests MUST cover every Domain rule and state transition.
- Integration tests MUST cover API endpoints, persistence, validation, and the decision workflow.
  Persistence, constraint, and concurrency tests MUST run against SQL Server (e.g. a disposable
  container); EF Core InMemory and SQLite are not acceptable substitutes.
- Test databases MUST be created by applying the EF Core migrations, not `EnsureCreated`.
- When no SQL Server instance is available, those tests MUST be reported as not run, never as
  passed or silently skipped.
- Concurrency tests MUST be deterministic (e.g. two units of work load the same version and
  commit in sequence; the second MUST fail), not dependent on thread timing.
- Tests MUST include: 80% timing boundaries (just before, exactly at, just after), zero and
  non-zero refills, invalid inputs, empty/whitespace notes, repeated decisions, and concurrent
  decisions.
- Test results MUST be reported only from an actual execution in the current state of the code;
  unexecuted or failing tests MUST be reported as such.

### VI. Security & Data Handling

- Only fictional patient data MAY be used in seed data, tests, screenshots, and documentation.
- All input MUST be validated on the server; invalid requests return a structured error.
- API responses MUST use DTOs; persistence entities MUST NOT be serialized.
- Logs MAY contain identifiers only; they MUST NOT contain patient names, allergies,
  medications, or decision notes.
- Documentation MUST state accurately what authentication/authorization exists; nothing MAY be
  described as production-grade unless implemented and tested (see U5).

### VII. Frontend Quality

- The frontend MUST use Angular with TypeScript in a feature-based folder structure.
- HTTP access MUST live in dedicated services; presentation components MUST NOT call the API
  directly.
- Every data-driven view MUST render loading, empty, and error states; the decision flow MUST
  also render warnings, blocking reasons, and success.
- Controls MUST be keyboard-operable with accessible labels; layouts MUST be usable without
  horizontal scrolling at 360px and 1280px widths; spacing and typography MUST come from shared
  styles, not ad-hoc values.

## Delivery Constraints

- Scope MUST fit the two-day timebox; a complete, tested request → review → decision workflow
  takes priority over any optional feature.
- `SPEC.md`, `PROMPTS.md`, and `README.md` MUST be kept current with the delivered behavior.
- The README setup steps MUST be verified from a clean environment (fresh clone, no prior
  database) before delivery.

## Development Workflow & AI-Assisted Development

- AI MAY be used for planning, implementation, test generation, and review; every AI-generated
  change MUST be reviewed by the author before it is kept.
- AI suggestions MUST be rejected when they violate a business rule, add complexity forbidden by
  Principle I, or are not covered by tests.
- Prompts that produced kept code, tests, or design decisions, and their outcomes, MUST be
  recorded in `PROMPTS.md`.
- Each plan MUST include a Constitution Check against Principles I–VII; any deviation MUST be
  justified in the plan's complexity tracking.

## Unresolved Decisions

These MUST be decided (in the spec or plan) before the affected work is implemented:

- **U1 – Database provider**: RESOLVED in v2.2.0 — Microsoft SQL Server with `rowversion`
  tokens (Principle IV) and SQL Server integration tests (Principle V).
Resolved decisions are defined in `specs/001-refill-request-review/spec.md` (cited
requirements) and are not restated here.

- **U2 – 80% rule inputs**: RESOLVED in v3.0.0 — spec FR-006.
- **U3 – 80% rule reference time**: RESOLVED in v3.0.0 — decision time; spec FR-011.
- **U4 – Warning handling**: RESOLVED in v3.0.0 — no acknowledgment; spec FR-008, FR-009.
- **U5 – Identity**: RESOLVED in v3.0.0 — out of scope, documented; spec FR-020.
- **U6 – Refill count effect**: RESOLVED in v3.0.0 — spec FR-012, FR-013.
- **U7 – Request creation**: RESOLVED in v3.0.0 — spec FR-002, FR-003, FR-004.
- **U8 – Document roles**: RESOLVED in v3.0.0 — the feature spec is the source of truth;
  `SPEC.md` summarizes delivered behavior and links to it.
- **U9 – Repeat approvals within one supply period**: RESOLVED in v3.0.1 — no additional rule;
  documented known limitation; refills never over-consumed. Spec FR-012, FR-025, FR-026.

## Governance

- This constitution overrides other project guidance where they conflict.
- Amendments MUST be written with rationale and update the version and Sync Impact Report.
- Versioning: MAJOR for removed or redefined principles or business rules; MINOR for new
  principles or sections or materially expanded guidance; PATCH for wording fixes.
- Resolving an Unresolved Decision is a MINOR amendment if it adds a rule, PATCH if it only
  records a choice.
- Compliance MUST be checked during plan review and before each delivery; violations block
  delivery unless justified in writing.

**Version**: 3.0.1 | **Ratified**: 2026-10-19 | **Last Amended**: 2026-10-20
