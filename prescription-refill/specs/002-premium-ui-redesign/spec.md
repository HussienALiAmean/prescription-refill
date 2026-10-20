# Feature Specification: Premium Clinical UI Redesign

**Feature Branch**: `002-premium-ui-redesign`

**Created**: 2026-10-20

**Status**: Draft

**Input**: User description: "Premium UI/UX redesign of the existing Angular frontend (queue +
review screens, shared design system), frontend-only, preserving all API/SignalR contracts,
business rules, and tests."

> This feature changes presentation only. Behavior, rules, and data contracts remain defined by
> [001-refill-request-review/spec.md](../001-refill-request-review/spec.md) (cited below as
> "001 FR-xxx"). Where this spec and 001 disagree, 001 wins and this spec is corrected.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Decide a request with unmistakable safety signals (Priority: P1)

A clinician opens a pending request and, in one glance, understands who the patient is, what is
being requested, whether approval is blocked and why, and which non-blocking warnings exist.
They write a note and approve or deny; the result and any rejection are clearly shown, and a
decided request visibly becomes read-only.

**Why this priority**: The review screen is where clinical risk lives; clarity there matters
more than anything else in the redesign.

**Independent Test**: Using the seeded scenarios (eligible, too early, zero refills, allergy
conflict, interaction, blocker plus warning), open each request and confirm the blocked,
warning, and clear states are visually distinct and every existing review workflow still
succeeds.

**Acceptance Scenarios**:

1. **Given** a pending request with a blocking reason, **When** the review screen loads,
   **Then** a blocked summary is the most prominent element of the safety section, each reason
   is listed with its eligible-from time where provided, Approve is visibly unavailable with the
   reason next to it, and Deny remains available.
2. **Given** a pending request with warnings and no blockers, **When** it loads, **Then**
   warnings appear in a style that is clearly different from blockers (different color, icon,
   and heading wording), state that they do not block approval, and Approve is available.
3. **Given** a pending request with neither blockers nor warnings, **When** it loads, **Then** a
   compact "no blocking rules / no warnings" confirmation is shown without dominating the
   screen.
4. **Given** any pending request, **When** the clinician views the safety section, **Then** the
   evaluation time and the "may be out of date; re-checked when approving" notice are visible
   near the Refresh action.
5. **Given** a decision is being submitted, **When** the request is in flight, **Then** both
   decision buttons are disabled and the pressed button shows a progress indication.
6. **Given** a decision succeeds, **When** the response arrives, **Then** a success
   confirmation is announced, and the decision area is replaced by a read-only decision summary
   (outcome, note, time) with no decision controls.
7. **Given** an approval is blocked, a concurrency conflict occurs, the request was already
   decided, or the note is invalid, **When** the response arrives, **Then** the existing message
   for that case (001 FR-011, FR-032) is shown in the redesigned alert style, success is never
   shown, and the typed note is preserved where 001 requires it.
8. **Given** the patient has recorded allergies, **When** the review loads, **Then** allergies
   are shown as distinct, scannable items in the patient summary, not as a run-on sentence.

---

### User Story 2 - Scan and open the right request from the queue (Priority: P2)

A clinician lands on the queue, sees how much pending work exists and which requests need
attention, narrows the list by text or status, and opens the correct request.

**Why this priority**: The queue is the entry point; faster scanning reduces wrong-request
errors and time to decision.

**Independent Test**: With seeded data, confirm the queue shows a count summary derived from the
loaded list, each row exposes patient, medication, time, and safety indicators in a consistent
hierarchy, the text filter narrows rows, and opening a row lands on that request.

**Acceptance Scenarios**:

1. **Given** the Pending view has loaded, **When** the clinician looks at the header, **Then**
   they see the page title, a one-line description, and counts of pending requests, of those
   with approval blocked, and of those with warnings, all computed only from the server's list
   response.
2. **Given** a loaded list, **When** the clinician types in the search field, **Then** only rows
   whose patient name or medication name contains the text (case-insensitive) remain, the
   result count updates, and server order is preserved.
3. **Given** a search with no matches, **When** the list renders, **Then** an empty state names
   the search text and offers to clear it; this is distinct from "no requests in this status".
4. **Given** each row, **When** the clinician scans it, **Then** patient name is the primary
   text, medication is secondary, the relevant time (requested for Pending, decided otherwise)
   is tertiary, and status and safety badges are aligned in one place.
5. **Given** the clinician selects Approved or Denied, **When** the list loads, **Then** rows are
   visually distinguishable from pending rows (status badge, no safety indicators) and open
   read-only.
6. **Given** the list is loading for the first time or after a status change, **When** it
   renders, **Then** skeleton rows matching the final row layout are shown instead of a text
   line.
7. **Given** a background reload (Refresh, live update, tab return), **When** it is in progress,
   **Then** the current rows stay visible and the Refresh control shows progress.
8. **Given** the list fails to load, **When** the error renders, **Then** a retryable error
   state is shown with a Try again action.
9. **Given** the queue, **When** the clinician switches between Pending, Approved, and Denied
   with the segmented status switcher (pointer or arrow keys), **Then** the same status
   semantics as before apply (Pending default, server ordering), and only the active segment
   shows a count, equal to the number of records the server returned for it.
10. **Given** more records than the page size, **When** the list renders, **Then** it shows one
    page of records, "Showing x–y of N", "Page p of P", Previous/Next (disabled at the
    boundaries), and a page-size selector (10, 25, 50).
11. **Given** the clinician is on page 2, **When** they change the search text, status, or page
    size, **Then** the list returns to page 1; **When** a background reload shrinks the list so
    the current page no longer exists, **Then** the last existing page is shown.

---

### User Story 3 - Trust what the screen is showing (Priority: P3)

A clinician can always tell whether live updates are connected and whether what they see may
be stale, on both screens and at any width.

**Why this priority**: Correctness never depends on live updates (001 FR-030), but visible
connection state prevents clinicians from trusting a stale screen.

**Independent Test**: Stop the live channel and observe the indicator change on both screens;
restore it and observe the indicator return and data reload.

**Acceptance Scenarios**:

1. **Given** live updates are connected, **When** any screen is shown, **Then** the header shows
   no connection message (unchanged, tested behavior: the indicator appears only when something
   needs attention).
2. **Given** live updates are reconnecting or unavailable, **When** any screen is shown,
   **Then** the indicator changes to a distinct reconnecting or unavailable state with text
   that tells the clinician to use Refresh, announced to assistive technology.
3. **Given** the queue was last checked at a known time, **When** the Pending view is shown,
   **Then** the "checked against current data at …" time is shown near the list controls.

---

### User Story 4 - Use it comfortably on any device and with a keyboard (Priority: P3)

A clinician can complete every workflow at 360px and 1280px widths, using only a keyboard,
with visible focus and sufficient contrast.

**Why this priority**: Required by the constitution (Principle VII) and needed for the redesign
not to regress accessibility.

**Independent Test**: At 360px and 1280px, complete open → review → deny and open → review →
approve with keyboard only, checking for horizontal scrolling, focus visibility, and contrast.

**Acceptance Scenarios**:

1. **Given** a 360px-wide viewport, **When** either screen is shown, **Then** there is no
   horizontal page scrolling and the decision actions are reachable without overlapping
   content.
2. **Given** keyboard-only use, **When** the clinician tabs through either screen, **Then** every
   interactive element is reachable in a logical order and shows a clearly visible focus
   indicator.
3. **Given** the operating system requests reduced motion, **When** state transitions occur,
   **Then** no non-essential motion plays.

### Edge Cases

- Long patient names, medication names, dosages, and notes (up to 1,000 characters) wrap
  without breaking the layout at 360px.
- A patient with many allergies or many other active prescriptions.
- Both blocking reasons plus multiple warnings at once: all are visible, blockers first.
- A decided request opened directly by URL.
- A request decided elsewhere while its review is open: the read-only transition and the
  "decided elsewhere" notice still appear.
- Search text while a background reload replaces the list: the search text is kept and applied
  to the new list.
- Switching status while search text is entered: the search text is kept and applied.
- A live update removes records while the clinician is on the last page; the page clamps to
  the new last page.
- Exactly 10, 11, 25, 26 records: page boundaries show correct ranges and Next enablement.
- A failed background reload while data is shown: data stays visible, an error notice appears.
- Request not found.
- Browser zoom at 200%: content reflows without loss.

## Requirements *(mandatory)*

### Functional Requirements

**Preservation (non-negotiable)**

- **FR-001**: The redesign MUST NOT change any server behavior, API request or response shape,
  live-update message, business rule, or stored data. All behavior defined in 001 MUST
  continue to work exactly as specified.
- **FR-002**: The interface MUST NOT evaluate eligibility rules or derive safety conclusions;
  every safety indicator MUST come directly from server-provided values (001 FR-005).
- **FR-003**: The interface MUST NOT display invented metrics, priorities, risk scores, or
  clinical data. Counts and indicators MAY be shown only when computed directly from data the
  server returned for the current view.
- **FR-004**: The fictional-data and no-sign-in notice (001 FR-019, FR-020) MUST remain
  visible on every screen with its current wording.
- **FR-005**: All existing automated tests MUST continue to pass without being weakened; any
  test change MUST keep or strengthen what it checks.

**Shared visual system**

- **FR-006**: The interface MUST use one shared set of visual tokens (color, spacing, type
  scale, radii, shadows, motion) and one shared set of components for buttons, form controls,
  cards, badges, alerts, and loading, empty, error, and success states. Screens MUST NOT use
  ad-hoc values for these.
- **FR-007**: The palette MUST use a refined neutral background, white content surfaces, deep
  slate text, and a single restrained teal/blue accent. Danger (blocking), warning
  (non-blocking), success, and info MUST each have a distinct color AND a distinct icon or
  label so meaning never relies on color alone.
- **FR-008**: Text and meaningful non-text elements MUST meet WCAG 2.1 AA contrast (4.5:1 for
  body text, 3:1 for large text, icons, borders of inputs, and focus indicators).
- **FR-009**: Every interactive element MUST show a visible focus indicator on keyboard focus,
  and hover/active/disabled states MUST be visually distinct.
- **FR-010**: Motion MUST be limited to short state transitions and MUST be disabled when the
  user prefers reduced motion.
- **FR-011**: Icons MUST come from one consistent set and MUST be decorative (hidden from
  assistive technology) unless they are the only label of a control.

**Application shell**

- **FR-012**: The application header MUST show the product name, link to the queue, and the
  live-update indicator. The indicator MUST stay empty while connected and MUST render the
  reconnecting and unavailable states as distinct, styled status pills.
- **FR-013**: Page content MUST use one consistent page container, page header pattern (title,
  description, actions), and responsive grid.

**Queue screen**

- **FR-014**: The queue header MUST show a title, a short description, a segmented status
  switcher (Pending, Approved, Denied), a text search, and a Refresh action. For the Pending
  view it MUST show counts of pending requests, of those whose server summary reports approval
  blocked, and of those with warnings. Counts MUST be exact counts of the records the server
  returned for the selected status; a count MUST NOT be shown for a status whose records were
  not loaded.
- **FR-014a**: The status switcher MUST keep the existing status semantics (Pending default,
  `status` query parameter for Approved/Denied), be a single keyboard tab stop with arrow-key
  movement between options, and expose the selected option to assistive technology.
- **FR-015**: Search MUST filter the already-loaded list on patient name or medication name
  (case-insensitive substring), MUST preserve server ordering (001 FR-018), MUST persist across
  reloads and status changes within the screen, and MUST be clearable.
- **FR-016**: Each row MUST present patient name (primary), medication (secondary), the
  relevant time (requested or decided), the status badge for decided requests, and the
  server-provided safety badges (approval blocked, warnings, no issues found) for pending
  requests. The whole row MUST be one link target to the review screen.
- **FR-017**: First loads MUST show skeleton rows; background reloads MUST keep rows visible
  and show progress on the Refresh action; errors MUST show a retryable error state; empty
  results MUST distinguish "no requests in this status" from "no matches for this search".

**Queue pagination**

- **FR-028**: The request list MUST be paginated with a shared, accessible pagination control
  showing the record range ("Showing x–y of N"), the current and total page ("Page p of P"),
  Previous and Next (disabled at the first/last page), and a page-size selector with 10, 25,
  and 50 (default 10).
- **FR-029**: Totals and page counts MUST be exact. They are computed from the complete list
  the server returns for the selected status, after the search filter; nothing is estimated.
- **FR-030**: Changing the search text, status, or page size MUST return to page 1. A reload
  that changes the total MUST keep the current page when it still exists and otherwise show
  the last page. Server ordering MUST be preserved; every filtered record MUST appear on
  exactly one page (no duplicates, no omissions).
- **FR-031**: The pagination control MUST be hidden when there are no records, MUST keep the
  page-size selector available whenever records exist, and MUST be usable at 360px and by
  keyboard. Moving to another page MUST move focus to the list heading region so screen reader
  and keyboard users land at the start of the new page.
- **FR-032**: The review screen's fixed groups of fields (allergies, prescription facts, other
  active prescriptions) MUST NOT be paginated.

**Review screen**

- **FR-018**: The review screen MUST show, in this priority order on narrow screens: page
  header (medication, status, back link), the safety section (Pending) or decision summary
  (decided), the decision area (Pending), then patient and prescription details. At wide
  widths the safety and decision areas MUST be visible beside the details without scrolling
  past them first.
- **FR-019**: The patient summary MUST show name, date of birth, and recorded allergies as
  separate visual items; "None recorded" when empty.
- **FR-020**: The requested prescription MUST show medication, dosage, days supply, remaining
  refills, and last filled time in a labeled, aligned layout; other active prescriptions MUST
  be listed in a compact, scannable layout.
- **FR-021**: The safety section MUST present: blockers (most prominent, danger style, "Approval
  blocked" heading), then warnings (warning style, heading stating they do not block
  approval), then the evaluation time with the staleness notice and Refresh. When neither
  applies, a compact success confirmation MUST be shown.
- **FR-022**: The decision area MUST show the note field with its hint, character guidance
  toward the 1,000-character limit, inline validation errors from the server, Approve
  (primary) and Deny (destructive-secondary) actions, the disabled-Approve explanation when
  blocked, and in-progress state on the pressed action.
- **FR-023**: Decision outcomes (success, approval blocked, concurrency conflict, already
  decided, validation failure, not found, unexpected error, decided elsewhere) MUST each render
  in the shared alert style with the existing messages, announced to assistive technology.
- **FR-024**: After a successful decision or when a decided request is shown, the decision area
  MUST be replaced by a read-only decision summary (outcome badge, decision time, note) that is
  visually distinct from the pending layout.
- **FR-025**: The review screen MUST show loading (skeleton matching the layout), not-found,
  and retryable error states in the shared style.

**Responsive and accessible**

- **FR-026**: Both screens MUST work without horizontal page scrolling at 360px and 1280px and
  remain usable at 200% zoom.
- **FR-027**: Existing accessible names, labels, landmarks, live regions, and roles MUST be kept
  or improved; heading levels MUST form a logical outline on each screen.

### Key Entities

No new data. The redesign displays the existing entities from 001 (Refill Request, Patient,
Prescription, Evaluation, Request Notification) without changing them.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing automated frontend and backend tests pass after the redesign,
  with no test removed or weakened; any unexecuted suite is reported as not run.
- **SC-002**: Zero changes to server code, API contracts, or live-update contracts (verified by
  diff scope: only frontend presentation files and this feature's documents change).
- **SC-003**: In a check of every seeded scenario, a reviewer correctly identifies whether
  approval is blocked, warned, or clear within 3 seconds of the review screen loading, in 100%
  of scenarios.
- **SC-004**: A clinician can open the correct pending request from a seeded queue of at least
  6 requests using search in under 10 seconds.
- **SC-005**: Both screens show no horizontal scrolling at 360px and 1280px, and every
  interactive element has a visible focus indicator (manual check of each element).
- **SC-006**: All text and meaningful non-text elements meet WCAG 2.1 AA contrast (checked
  against the token palette).
- **SC-007**: Every state listed in FR-017, FR-023, and FR-025 is rendered in the shared style
  (visual audit checklist, 100% coverage).
- **SC-008**: In automated tests over lists of 0, 1, 10, 11, and 26 records with and without
  search, the union of all pages equals the filtered list in server order with no duplicates.
- **SC-009**: Before and after screenshots exist for both screens at 360px and 1280px, and at
  least one refinement pass based on reviewing the after screenshots is recorded.

## Assumptions

- Presentation only: the backend, API, live-update service boundaries, and data services stay
  unchanged; HTTP stays in the data service and live updates stay in the realtime service.
- Search is local to the loaded list because the list is not paginated (001 Assumptions) and
  the API has no search parameter; adding one would change the contract (out of scope).
- Prioritization means visual emphasis of server-provided indicators and counts; the
  server's ordering (oldest pending first) is kept. No priority or risk score is invented.
- Light theme only, as today; dark mode is out of scope.
- No component library or icon package is added; a small set of inline icons and system fonts
  are used to avoid new dependencies and external requests.
- The status filter becomes a segmented switcher (author decision 2026-10-20). Its unit test
  changes with it, keeping equivalent assertions (accessible group name "Show", three options
  in order, current selection, change emitted).
- Pagination is client-side: the list endpoint returns the complete collection for a status
  (001 contract: "Not paginated"), so totals are exact. Lists can grow over time (requests are
  created through the API and decided requests accumulate); server-side pagination is
  proposed as a separate API change and is not part of this feature.
- The "Approved" and "Denied" tabs show no count until selected, because the list response
  covers one status only and no extra requests are made for counts.
- Out of scope: new screens, request creation UI, authentication, charts, dashboards,
  localization.
