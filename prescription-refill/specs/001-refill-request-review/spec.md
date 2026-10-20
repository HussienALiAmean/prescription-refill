# Feature Specification: Prescription Refill Request Review

**Feature Branch**: `001-refill-request-review`

**Created**: 2026-10-19 | **Updated**: 2026-10-20 (A1–A10 decided; evaluation lifecycle, exact
timing, atomic approval, live updates, and caching refined)

**Status**: Draft

**Input**: User description: "Prescription Refill Requests: a clinician reviews a queue of
pending refill requests for fictional patients, inspects the patient profile, allergies, active
prescriptions, remaining refills, dosage and last-filled date, sees server-evaluated safety
warnings and blocking rules, and approves or denies each request with a required note;
decisions are final and concurrency-safe; connected screens learn about changes live."

> This document is the detailed source of truth for feature requirements. `SPEC.md` at the
> repository root summarizes delivered behavior and links here; it does not restate rules.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Review and decide a pending request (Priority: P1)

A clinician opens a pending refill request from the queue, sees the patient's profile,
allergies, and active prescriptions together with the server's evaluation (blocking reasons,
warnings, and the time it was evaluated), and approves or denies it with a note. An approval
uses one remaining refill; a denial changes nothing on the prescription. A decided request is
final.

**Why this priority**: The decision workflow and its safety rules are the core of the product.

**Independent Test**: With seeded data containing pending requests, a clinician can approve an
eligible request, deny any pending request, and is prevented from approving a blocked one.

**Acceptance Scenarios**:

1. **Given** a pending request whose prescription has refills remaining and at least 80% of its
   supply period elapsed, **When** the clinician approves it with a note, **Then** the request
   becomes Approved, the remaining refills decrease by exactly one, and the last filled time is
   unchanged.
2. **Given** a pending request whose prescription has zero remaining refills, **When** approval
   is attempted, **Then** it is rejected, the response lists the "no refills remaining" reason,
   and neither the request nor the prescription changes.
3. **Given** a pending request less than 80% into its supply period, **When** approval is
   attempted, **Then** it is rejected with a "too early" reason that states when the
   prescription becomes eligible.
4. **Given** any pending request, including a blocked one, **When** the clinician denies it with
   a note, **Then** the request becomes Denied and the remaining refills are unchanged.
5. **Given** a pending request with an allergy conflict or drug interaction, **When** the
   clinician views it, **Then** the warnings are displayed before deciding, and approval is
   still permitted if no blocking rule applies.
6. **Given** a decided request, **When** anyone attempts another approval or denial, **Then** it
   is rejected as already decided and nothing changes.
7. **Given** a review screen whose displayed evaluation allowed approval, **When** the stored
   data changed so approval is no longer allowed and the clinician approves, **Then** the
   approval is rejected, the response contains the current evaluation and blocking reasons, the
   screen replaces its evaluation with that one, Approve becomes unavailable, and Deny remains
   available.
8. **Given** the latest displayed evaluation of a pending request reports a blocking reason,
   **When** the clinician views it, **Then** the Approve action is unavailable, the reason is
   shown next to it, and Deny remains available.
9. **Given** an open review screen, **When** the clinician looks at the evaluation, **Then** its
   evaluation time is shown with a notice that it may be out of date and is re-checked when
   approving; **When** the clinician uses Refresh, **Then** a new server evaluation replaces it.
10. **Given** a clinician submits a decision while another change to the same request or its
    prescription commits first, **When** the conflict is detected, **Then** nothing from the
    attempted decision is saved, the screen does not report success, it explains that the data
    changed at the same time, and it reloads the request, prescription, and evaluation from the
    server.

---

### User Story 2 - Create a refill request through the API (Priority: P2)

A client submits a refill request for a fictional patient's active prescription through the
API. There is no request-creation screen; seed data supplies the requests shown in the queue.
A request is accepted even if approval is currently blocked; the review shows why.

**Why this priority**: Supports the one-pending-request rule and lets new requests be created
after a decision; the clinician workflow does not depend on it.

**Independent Test**: Through the API, submit a request for an active prescription with no
pending request; it appears in the queue as Pending. Submit a second one for the same
prescription; it is rejected.

**Acceptance Scenarios**:

1. **Given** an active prescription with no pending request, **When** a request is submitted,
   **Then** a Pending request is created.
2. **Given** an active prescription with zero refills or too early in its supply period,
   **When** a request is submitted, **Then** the request is still created and its review shows
   the blocking reason.
3. **Given** a prescription that already has a Pending request, **When** another request is
   submitted (including simultaneously), **Then** it is rejected with a "pending request
   exists" error.
4. **Given** a prescription whose previous request was decided, **When** a new request is
   submitted, **Then** it is created as Pending.
5. **Given** an inactive prescription, **When** a request is submitted, **Then** it is rejected
   with a validation error stating that the prescription is inactive, and nothing is created.

---

### User Story 3 - Browse requests and outcomes (Priority: P3)

A clinician filters the request list by status to see Pending, Approved, or Denied requests,
and opens decided requests read-only to see the decision, note, and time.

**Why this priority**: Shows that decisions are recorded and final; not required for safety.

**Independent Test**: With seeded requests in all three states, the list shows each with the
correct status, the filter narrows results, and a decided request has no decision controls.

**Acceptance Scenarios**:

1. **Given** requests in Pending, Approved, and Denied states, **When** the list is filtered by
   Pending, **Then** only pending requests appear, oldest first.
2. **Given** the list is filtered by Approved or Denied, **When** it loads, **Then** those
   requests appear, most recently decided first.
3. **Given** a decided request, **When** the clinician opens it, **Then** its decision, note,
   and times are shown and no approve, deny, or edit action is offered.
4. **Given** no requests match a filter, **When** the list loads, **Then** an empty state is
   shown.

---

### User Story 4 - See changes made elsewhere (Priority: P3)

Two clinician screens (tabs or devices) are open. When a request is created or decided on one,
the other learns about it and reloads the affected data from the server.

**Why this priority**: Reduces stale screens; correctness never depends on it because the
server re-checks every decision.

**Independent Test**: Open the queue in two tabs, decide a request in one, and observe the other
reload its queue; disconnect and reconnect a tab and observe it reload.

**Acceptance Scenarios**:

1. **Given** two open queue screens, **When** a request is decided on one, **Then** the other
   reloads its list from the server and the request leaves the Pending queue.
2. **Given** a review screen open on a request, **When** that request is decided elsewhere,
   **Then** the screen reloads it from the server, shows the decided, read-only state, and
   explains that it was decided elsewhere.
3. **Given** a screen that lost its live connection, **When** the connection is restored or the
   clinician returns to the screen, **Then** it reloads its data from the server.
4. **Given** the live connection is unavailable, **When** the clinician works normally, **Then**
   loading, Refresh, and decisions still work, and the screen indicates that live updates are
   unavailable.

---

### Edge Cases

- Decision time one instant before, exactly at, and one instant after the 80% threshold.
- The decision is attempted in a clock or time zone other than UTC; the result is identical.
- The last filled time is later than the evaluation time, missing, or the days supply is not
  positive (invalid stored data): approval is blocked, never allowed.
- The decision note is missing, empty, whitespace-only, or longer than the maximum length.
- The request or prescription does not exist; the prescription is inactive (creation only).
- Two decisions (approve/approve or approve/deny) on the same request arrive simultaneously.
- Another transaction changes the prescription between loading and committing an approval.
- Two requests for the same prescription are submitted simultaneously.
- Both blocking reasons apply at once; both are reported.
- Warnings and blocking reasons apply at once; approval is blocked and all are reported.
- An interacting medication exists only on an inactive prescription; no warning is shown.
- The patient has another active prescription for the same medication as the request; this is
  not an interaction (no pair is defined for a medication with itself).
- A decision commits but its live notification is not delivered.
- A screen receives a notification for a request it is not showing, or a duplicate
  notification.

## Requirements *(mandatory)*

### Functional Requirements

**Request creation (API only)**

- **FR-001**: The system MUST allow creating a refill request for an existing prescription
  through the API; a new request starts as Pending and records its submission time. No
  request-creation screen is provided.
- **FR-002**: The system MUST accept a request even when a blocking rule currently prevents
  approval.
- **FR-003**: The system MUST permit at most one Pending request per prescription, including
  under simultaneous submissions, and MUST reject violations with a "pending request exists"
  error.
- **FR-004**: After a prescription's request is decided, a new request for it MUST be accepted,
  subject to FR-003 and FR-021.
- **FR-021**: A request for an inactive prescription MUST be rejected with a documented
  validation error identifying the prescription as inactive.

**Evaluation**

- **FR-005**: Eligibility rules MUST be evaluated only by the server. Every evaluation MUST carry
  the instant it was computed, the approval outcome, every blocking reason, and every warning.
  The review view MUST show the evaluation time and state that the evaluation may be out of
  date and is re-checked by the server when approving.
- **FR-006**: Too-early rule. Inputs: the prescription's last filled time (a UTC instant) and
  days supply (a positive whole number). Threshold = days supply × 0.8 days, where one day is
  exactly 24 hours (so days supply × 19.2 hours). Elapsed = evaluation instant − last filled
  time, both in UTC, as an exact duration (not a count of calendar days). Approval is blocked
  when elapsed is **strictly less than** the threshold; at exact equality or after, this rule
  does not block. The "too early" reason MUST state the eligible-from instant (last filled time
  + threshold). The same calculation MUST be used for displayed evaluations and for decision
  processing.
- **FR-027**: Invalid timing data MUST fail safely. If the last filled time is missing or later
  than the evaluation instant, or the days supply is missing or not positive, the evaluation
  MUST report an "invalid prescription data" blocking reason (instead of "too early"), approval
  MUST be blocked, and denial MUST remain possible. Invalid data MUST never cause approval to
  be allowed.
- **FR-007**: No-refills rule: approval MUST be blocked when remaining refills are zero.
- **FR-022**: An **active medication** of a patient is a medication linked to that patient
  through a prescription marked active. This definition MUST be used for interaction checks and
  for the active prescriptions shown in the patient profile.
- **FR-008**: Warnings MUST be reported and MUST NOT block approval:
  - Allergy conflict: the patient has a recorded allergy whose medication is the same
    medication as the requested prescription. Matching uses only recorded allergy data; no
    drug-class or ingredient relationships are inferred.
  - Drug interaction: a seeded interaction pair joins the requested medication with another
    active medication of the patient (FR-022), excluding the requested prescription itself.
- **FR-009**: Warnings require no separate acknowledgment step.
- **FR-023**: Evaluation lifecycle in the interface. A fresh server evaluation is obtained when
  a request is opened, when the clinician uses an explicit Refresh action, after a blocked
  approval (taken from the error response, FR-011), after a concurrency conflict or "already
  decided" error (by reloading, FR-032), and when a live update or reconnection requires a
  reload (FR-030). There is no polling. While a request is Pending, the Approve action MUST be
  unavailable whenever the latest displayed evaluation reports a blocking reason, with the
  reason shown; Deny MUST remain available. Approve availability is an interface convenience
  only and is never relied on for correctness (FR-011).

**Decisions**

- **FR-010**: Only a Pending request MAY be approved or denied. Each decision MUST include a note
  that is non-empty after trimming and at most 1,000 characters; the note is stored without
  leading or trailing whitespace. The decision time MUST be recorded.
- **FR-011**: When the server processes an approval, it MUST re-evaluate every blocking rule at
  that instant against current stored data, ignoring any evaluation previously shown. If a
  blocking rule applies, the approval MUST be rejected with the documented "approval blocked"
  error, the request MUST remain Pending, and the response MUST contain the full current
  evaluation including its time, blocking reasons, and warnings. The interface MUST replace its
  displayed evaluation with that evaluation and keep Deny available.
- **FR-012**: An approval MUST consume exactly one remaining refill. A denial MUST NOT change the
  refill count. Neither decision changes the last filled time; dispensing is not modeled.
- **FR-013**: The approval decision and the refill decrement MUST be committed in a single
  transaction. A concurrent change to either the request or its prescription since they were
  loaded MUST be detected; on detection the entire transaction MUST be rolled back, leaving
  both unchanged, before the error is returned.
- **FR-025**: Remaining refills MUST never fall below zero and approvals MUST never consume more
  refills than were available, including under simultaneous approvals. This MUST be enforced by
  the business rules and by a storage-level constraint.
- **FR-014**: A denial MUST be permitted regardless of blocking reasons or warnings.
- **FR-015**: A decided request MUST NOT change again; further decisions MUST be rejected with an
  "already decided" error. There is no way to edit or delete a request.
- **FR-016**: When simultaneous operations conflict, at most one MUST succeed; the others MUST
  receive a "concurrency conflict" error with a stable, documented code and MUST NOT be retried
  automatically.
- **FR-032**: After a "concurrency conflict" or "already decided" error, the interface MUST NOT
  indicate success. It MUST reload the request, prescription state, and evaluation from the
  server, explain that the data changed at the same time (or that the request was already
  decided), and keep the typed note so the clinician can act again if the request is still
  Pending.
- **FR-017**: Errors MUST be distinguishable: validation failure, not found, approval blocked
  (with the current evaluation), already decided, pending request exists, concurrency conflict,
  and unexpected error (no internal details).
- **FR-028**: Stored data is the only source of truth for request status, remaining refills, and
  approval eligibility. Decisions MUST be validated against stored data at the time they are
  processed, never against cached or client-supplied copies.

**Viewing**

- **FR-018**: Clinicians MUST be able to see a queue of Pending requests (oldest submission
  first), filter by status (Approved and Denied listed most recently decided first), and open a
  request to view: the patient profile (name, date of birth), recorded allergies, the patient's
  active prescriptions (FR-022), and for the requested prescription its medication, dosage,
  days supply, remaining refills, and last filled time; plus the evaluation (Pending only) or
  the decision, note, and times (decided only). Decided requests are read-only.
- **FR-024**: Instants MUST be displayed in the viewer's local time in one consistent format
  that includes the date, 24-hour time, and UTC offset (e.g. `20 Oct 2026, 14:05 (UTC+03:00)`),
  with the exact UTC value available on hover or in supporting details. Dates without a time
  (date of birth) are shown as dates and never shifted by time zone.

**Live updates**

- **FR-029**: Connected clinician screens MUST receive a notification when a request is created
  or decided. The notification MUST contain only the request identifier and the kind of change;
  it MUST NOT contain patient, medication, or note data.
- **FR-030**: A notification MUST be sent only after the change has been committed, and never
  for a rejected or rolled-back operation. Receiving screens MUST treat it only as a prompt to
  reload the affected queue or request from the server. Screens MUST also reload when their
  live connection is restored and when the clinician returns to them, because notifications are
  not guaranteed to arrive.
- **FR-031**: Failure to deliver a notification MUST NOT undo or fail a committed change or its
  response. Recovery is by reconnection, returning to the screen, or Refresh.

**Data and limitations**

- **FR-019**: The system MUST ship with fictional seed data covering: eligible, too early, zero
  refills, allergy conflict, drug interaction, an interacting medication on an inactive
  prescription that does not warn, both a blocker and a warning, and already-decided requests.
  Medications and 3–5 interaction pairs MUST be clearly identified as synthetic demo data, not
  clinical guidance.
- **FR-020**: Authentication and authorization are out of scope. The application and
  documentation MUST state that any user can create and decide requests and receive live
  notifications, that the live notification channel is unauthenticated and not
  production-secure, and that the demo is not suitable for deployment with real patient data.
  Reviewer identities MUST NOT be recorded or displayed.
- **FR-026**: Documentation MUST state that dispensing is not modeled and that, because approval
  does not change the last filled time, more than one request may be approved within the same
  supply period while refills remain (known limitation; no additional rule).

### Key Entities

- **Patient**: fictional person; name, date of birth; has prescriptions and allergies.
- **Medication**: a synthetic drug identified by name.
- **Prescription**: a patient's medication with dosage (display text), days supply (positive
  whole days), remaining refills (zero or more), last filled time (UTC instant), and whether it
  is active.
- **Allergy**: a patient's recorded allergy to a specific medication.
- **Drug Interaction**: a seeded, synthetic pair of two different medications with a
  description.
- **Refill Request**: belongs to one prescription; status (Pending, Approved, Denied), submitted
  time, and, once decided, decision time and note.
- **Evaluation** (computed, not stored): evaluation time, eligible-from time, approval outcome,
  blocking reasons, warnings.
- **Request Notification** (transient): request identifier and kind of change (created,
  decided).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In automated tests, 100% of approval attempts that violate a blocking rule are
  rejected, including the three threshold instants (just before, at, just after) and invalid
  timing data.
- **SC-002**: In automated simultaneous-operation tests, zero cases occur where a request
  receives more than one decision, a refill is consumed more than once, remaining refills go
  below zero, or a request and its prescription disagree after a failed approval.
- **SC-003**: A clinician can open a pending request, read its warnings and blocking reasons, and
  record a decision in under 1 minute.
- **SC-004**: Every rejected decision tells the user why, in plain language, without a manual
  page reload being needed to see the reason.
- **SC-005**: A new developer can go from a fresh clone to a running, seeded application by
  following the README in under 20 minutes.
- **SC-006**: In automated tests, every successful create or decision produces exactly one
  notification after commit, and rejected or rolled-back operations produce none.
- **SC-007**: With two screens connected, a decision made on one is reflected on the other
  within 2 seconds (manual check).

## Assumptions

- The only interface is the clinician's review interface; requests are created by seed data or
  through the API (A1).
- Patients, medications, prescriptions (including the active flag), allergies, and interactions
  are seed data with no editing feature; therefore a prescription cannot become inactive while
  a request is Pending, and approval does not re-check the active flag.
- No drug-class or ingredient data is seeded, so allergy matching is by exact medication only
  (A4).
- Duplicate-therapy detection (two active prescriptions for the same medication) is out of
  scope.
- The 1,000-character note limit is a demo default.
- The submission time is recorded for ordering and display only; it does not affect
  eligibility (FR-011).
- Lists are not paginated (demo scale).
- Live notifications go to every connected screen; there is no per-user targeting because there
  is no user identity (FR-020).
