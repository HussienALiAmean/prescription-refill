# Feature Specification: Server-Side Pagination of Refill Request Lists

**Feature Branch**: `003-backend-pagination`

**Created**: 2026-10-20

**Status**: Draft

**Input**: User description: "we need the pagiantion to be in the Backeend"

**Context**: Feature 001 delivers the request lists unpaginated (001 Assumptions: "Lists are not
paginated (demo scale)"). Feature 002 added pagination, search, and summary counts in the browser
over the complete list, and recorded server-side paging as a proposed separate change (002
research R13). This feature moves paging, together with the search and counts that depend on
it, to the server. Safety rules, decisions, and the review screen are unchanged.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse a long list one page at a time (Priority: P1)

A clinician opens the Pending queue (or the Approved or Denied history) and sees the first page
of requests in the same order as today, with the total number of requests and the page
position. They move to the next or previous page or change the page size, and only the
requests for that page are delivered to the screen.

**Why this priority**: This is the feature. Without it every list load transfers the whole
collection, which keeps growing as requests are created and decided.

**Independent Test**: Seed more requests than one page holds; open each status, page through
to the end, and confirm every request appears exactly once, in the established order, with
correct "x–y of N" and "Page p of P" information, while each load returns only one page.

**Acceptance Scenarios**:

1. **Given** 23 Pending requests and a page size of 10, **When** the clinician opens the queue,
   **Then** the 10 oldest requests are shown, the total reads 23, and the position reads page 1
   of 3.
2. **Given** the same queue, **When** the clinician moves to page 3, **Then** exactly the 3
   newest requests are shown and Next is unavailable.
3. **Given** any page, **When** the clinician changes the page size, **Then** page 1 is shown at
   the new size and the total is unchanged.
4. **Given** the Approved or Denied history, **When** the clinician pages through it, **Then**
   requests appear most recently decided first, as today, each exactly once.
5. **Given** an empty status, **When** it is opened, **Then** the existing empty state is shown
   and no pagination controls appear.

---

### User Story 2 - Search across the whole list, not just the visible page (Priority: P1)

A clinician types part of a patient or medication name. Matches are found across every request
of the selected status, not only those on the current page, and the matching results are paged
the same way.

**Why this priority**: Once only one page reaches the screen, filtering in the browser would
silently miss matches on other pages. Moving search with paging is required for correctness,
so it shares P1.

**Independent Test**: Seed a match that falls on page 3 of the unfiltered list; search for it
from page 1 and confirm it is found, the result count is correct, and paging applies to the
results.

**Acceptance Scenarios**:

1. **Given** 23 Pending requests where one patient appears only on page 3, **When** the
   clinician searches for part of that patient's name from page 1, **Then** that request is
   shown and the result count reads 1.
2. **Given** a search with 12 matches and a page size of 10, **When** results are shown,
   **Then** page 1 shows 10 matches, page 2 shows 2, in the established order.
3. **Given** any search, **When** the search text changes or is cleared, **Then** page 1 is
   shown.
4. **Given** a search with no matches, **When** results load, **Then** the existing "no
   matches" state with Clear search is shown.
5. **Given** search text that differs only in letter case or surrounding spaces, **When**
   searched, **Then** results are identical.

---

### User Story 3 - Exact summary counts while paging (Priority: P2)

On the Pending queue, the clinician still sees how many requests are pending in total and how
many are blocked, have warnings, or have no issues, for the whole status, even though only one
page is loaded. The selected status still shows its exact count.

**Why this priority**: Preserves existing information (002 R6). Without server-provided counts
the summary would either disappear or become wrong.

**Independent Test**: Seed known numbers of blocked, warning, and clear Pending requests across
several pages; confirm the summary shows the exact numbers on every page and with or without a
search.

**Acceptance Scenarios**:

1. **Given** 23 Pending requests of which 7 are blocked and 5 have warnings, **When** any page
   is shown, **Then** the summary reads 23 total, 7 blocked, 5 with warnings, and the matching
   number with no issues.
2. **Given** an active search, **When** results are shown, **Then** the summary still describes
   the whole status, and a separate line shows the number of matching results (as today).
3. **Given** the Approved or Denied history, **When** shown, **Then** the selected status shows
   its exact total and no blocked/warning summary is shown (as today).

---

### User Story 4 - Live updates keep the current page correct (Priority: P3)

While a clinician views a page, another screen creates or decides a request. The current page,
totals, and counts refresh without losing the clinician's place, search text, or page size.

**Why this priority**: Live updates already exist (001 US4); this keeps them correct under
paging.

**Independent Test**: Open page 2 of the queue, decide a request from another screen, and
confirm page 2 reloads with updated contents and totals, and the search text and page size
are kept.

**Acceptance Scenarios**:

1. **Given** page 2 is shown, **When** a request is decided elsewhere, **Then** page 2 reloads
   with current contents and totals, keeping the search text and page size.
2. **Given** the last page holds one request, **When** that request is decided elsewhere,
   **Then** the clinician is shown the new last existing page rather than an empty page.

---

### Edge Cases

- A requested page beyond the last page (for example after other screens decided requests)
  returns no items with correct totals, not an error; the screen then shows the last existing
  page.
- Page number zero, negative, or not a whole number, and page sizes outside the allowed range,
  are rejected with the existing documented validation error.
- Search text longer than the allowed length is rejected with the validation error; empty or
  whitespace-only search text means no search.
- Requests with the same submission or decision time keep a fixed relative order, so no request
  appears on two pages or on none while the data is unchanged.
- Data that changes between page loads (a request decided or created) can shift items between
  pages; a reload always reflects current data. Exactly-once delivery across pages is
  guaranteed only while the data is unchanged.
- Search text may contain patient names; it must never be written to logs.
- A list request without any paging values behaves exactly as today.

## Requirements *(mandatory)*

### Functional Requirements

**Paging**

- **FR-001**: The request list MUST accept an optional page number and page size and return
  only the requests for that page, plus the total number of matching requests, the page number,
  and the page size used.
- **FR-002**: When no page number, page size, or search text is supplied, the list MUST return
  the complete list exactly as today (same items, order, fields, and evaluation time), so existing
  clients and tests keep working. Supplying only one MUST apply the default for the other
  (page 1; page size 10).
- **FR-003**: Page numbers MUST start at 1. Page sizes MUST be whole numbers from 1 to 100; the
  screen offers 10, 25, and 50 as today.
- **FR-004**: Invalid page numbers, page sizes, or search text MUST be rejected with the
  existing documented validation error, identifying the invalid value; nothing else changes.
- **FR-005**: A page number beyond the last page MUST return no items with correct totals and
  MUST NOT be an error.
- **FR-006**: Ordering MUST stay as established in 001 FR-018 (Pending: oldest submission
  first; Approved and Denied: most recently decided first) with a fixed tie-break, so that
  while data is unchanged every request appears on exactly one page.

**Search**

- **FR-007**: The request list MUST accept optional search text and return only requests whose
  patient name or medication name contains it, ignoring letter case and surrounding spaces.
  Paging and totals MUST apply to the matching requests.
- **FR-008**: Search text MUST be at most 100 characters after trimming; empty or
  whitespace-only text means no search.
- **FR-009**: Search text MUST NOT be written to logs (it can contain patient names;
  constitution Principle VI).

**Counts and evaluation**

- **FR-010**: For any status, the list MUST return the exact total for the whole status
  regardless of search. For Pending it MUST also return, for the whole status, the exact
  number of requests that are blocked, that have warnings, and that have neither.
- **FR-011**: Per-request flags (can approve, has warnings) and all counts MUST come from the
  existing server evaluation, computed at the single evaluation time returned with the list.
  No rule logic moves, changes, or is duplicated; the browser MUST NOT compute these.

**Screens**

- **FR-012**: The queue screen MUST request one page at a time from the server and MUST NOT
  filter, count, or slice the list itself.
- **FR-013**: The screen MUST keep today's behavior: page sizes 10/25/50; return to page 1 when
  the status, search text, or page size changes; "x–y of N" and "Page p of P" information;
  focus moves to the results heading after a page change; search, no-match, empty, loading, and
  error states.
- **FR-014**: When a reload returns no items for a page beyond the new last page, the screen
  MUST show the last existing page.
- **FR-015**: Live update hints, reconnects, and returning to the screen MUST reload the current
  page, keeping the status, search text, page number, and page size.
- **FR-016**: Rapid typing or paging MUST NOT show results from an older request after a newer
  one (only the latest request's response is displayed).

**Unchanged**

- **FR-017**: The request detail, decision, and creation operations, all safety rules, error
  codes other than the new validation cases, and live update messages MUST be unchanged.

### Key Entities

- **Request list page**: one page of request summaries for a status, plus: evaluation time,
  page number, page size, number of matching requests, total for the whole status, and (Pending
  only) the blocked / warnings / no-issue counts for the whole status.
- **List query**: status (as today), optional page number, optional page size, optional search
  text.
- **Request summary**: unchanged from 001 (identifier, status, submission and decision times,
  patient name, medication name, and the two evaluation flags for Pending).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With 1,000 requests in a status, opening the queue delivers at most the selected
  page size of requests (10 by default) to the screen, instead of all 1,000.
- **SC-002**: Paging from first to last page through a list of at least 3 pages, with data
  unchanged, shows every request exactly once (0 missing, 0 duplicated) in the established
  order.
- **SC-003**: A search finds 100% of matching requests regardless of which page they would
  appear on unfiltered.
- **SC-004**: Totals and Pending summary counts shown on every page equal the true numbers for
  the whole status (0 discrepancies across the test data set).
- **SC-005**: With 1,000 Pending requests, a page or search result appears within 1 second on
  the development machine.
- **SC-006**: All existing list, queue, and pagination behaviors remain verified by passing
  tests; existing clients that send no paging values see no change.

## Assumptions

- Paging is offset-based (page number and size). Cursor or "load more" paging is out of scope;
  items can shift between pages when data changes, which is acceptable for this demo and covered
  by reloads.
- The page number, page size, and search text are not added to the browser address; only the
  status stays there, as today (002 R5 deferred this).
- Default page size is 10, matching today's screen; the server allows up to 100 to bound
  response size.
- Search keeps today's meaning: a case-insensitive "contains" match on patient name or
  medication name only, not on dates, notes, or identifiers.
- Pending summary counts still require evaluating every Pending request at the list's
  evaluation time; paging reduces what is delivered and displayed, not what is evaluated. At
  demo scale this is acceptable; caching evaluations is excluded (001 FR-028).
- No new information about patients, prescriptions, or requests is stored.
- Feature 002's client-side pagination tests are replaced by equivalent tests against server
  paging; the "proposed separate API change" in 002 research R13 is what this feature delivers.
- No authentication is added; the existing limitations (001 FR-020) still apply.
