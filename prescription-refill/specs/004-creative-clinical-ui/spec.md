# Feature Specification: Creative Clinical UI ("Ambient Atelier")

**Feature Branch**: `004-creative-clinical-ui`

**Created**: 2026-10-20

**Status**: Draft

**Input**: User description: "Creative Clinical UI — Angular 19 Implementation Brief": a distinctive,
editorial-quality visual redesign of the Prescription Refill Requests application with a dark
navigation shell, light clinical content surfaces, an ambient pointer-reactive lighting effect,
restrained motion, and a safety-first review screen, without changing data, behavior, or business
rules.

**Context**: Feature 002 introduced a shared design system (slate/teal tokens, icons, alerts,
badges, skeletons, pagination). Feature 003 moved paging, search, and counts to the server. This
feature replaces the visual identity and adds ambient interaction. It is presentation only: every
behavior covered by the 001, 002, and 003 specifications and their tests stays as it is.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Review a request with safety status impossible to misread (Priority: P1)

A clinician opens a refill request. Before anything else they see whether approval is blocked,
whether there are warnings that need clinical judgment, or whether no issues were found. Each
state has its own label, icon, and explanatory text as well as its own color. Patient identity,
allergies, the requested prescription, other active prescriptions, interaction warnings, and the
refill-timing result are grouped so the clinician can confirm them quickly. The note field and the
Approve and Deny actions stay easy to find without covering any of that information.

**Why this priority**: The review screen is where a decision is made; misreading a blocker or a
warning is the most serious failure the interface can cause.

**Independent Test**: Open one request of each kind from the demo data (blocked, warning only,
blocked plus warning, no issues, decided). For each, confirm that the safety state is identified by
text and icon without relying on color, that all clinical facts listed above are visible, and that
the decision controls behave exactly as before.

**Acceptance Scenarios**:

1. **Given** a request blocked by too-early timing, **When** it is opened, **Then** a blocking
   section labeled as preventing approval shows the reason and the eligible date, and Approve is
   unavailable while Deny remains available (unchanged behavior).
2. **Given** a request with an allergy or interaction warning and no blocker, **When** it is
   opened, **Then** a warning section labeled as requiring clinical review explains each warning,
   and nothing on the screen presents the request as safe.
3. **Given** a request with a blocker and a warning, **When** it is opened, **Then** both
   sections are shown, blockers first, each with its own label and icon.
4. **Given** a request with no blockers or warnings, **When** it is opened, **Then** a "no issues
   found" state is shown in neutral, factual wording that does not claim the approval is safe.
5. **Given** a narrow phone-width screen, **When** the clinician scrolls the review, **Then**
   safety information appears before patient details, and no fixed or sticky element covers
   warnings, blockers, or the note field.
6. **Given** any decision outcome (approved, denied, blocked by the server, concurrency conflict,
   already decided, validation error), **When** it occurs, **Then** the screen shows the same
   messages and state changes as before, in the new visual style.

---

### User Story 2 - Scan and work the queue quickly (Priority: P1)

A clinician opens the queue and immediately understands where they are, how many requests the
selected status contains, and which requests are blocked, have warnings, or have no issues. Rows
are compact and aligned so many requests fit on screen; the row under the pointer and the row with
keyboard focus are clearly marked. Search, the status switcher, pagination, and loading, empty,
error, and retry states all work as before.

**Why this priority**: The queue is the entry point used for every decision; it must be faster to
scan, not just prettier.

**Independent Test**: With demo data, use the queue by mouse, by keyboard only, and on a
phone-width screen: switch statuses, search, page, open a request, trigger an empty search and an
API error. All existing behaviors work; hover and focus states are visible; no horizontal
scrolling.

**Acceptance Scenarios**:

1. **Given** the Pending queue, **When** it loads, **Then** the title, a short description, the
   exact count for the selected status, and the server's blocked / warnings / no-issues summary
   are shown, with no invented figures.
2. **Given** keyboard-only use, **When** the clinician tabs through the queue, **Then** every row
   link, filter, search field, and pagination control shows a clearly visible focus indicator.
3. **Given** the list is loading, empty, has no search matches, or failed to load, **When** shown,
   **Then** each state has a distinct, readable design and the existing retry and clear-search
   actions work.
4. **Given** a phone-width screen, **When** the queue is shown, **Then** each row keeps patient,
   medication, request time, and safety badges readable without horizontal scrolling.

---

### User Story 3 - Ambient lighting that responds to the pointer without getting in the way (Priority: P2)

On a device with a mouse or trackpad, a soft, diffused light in the background of the main area
follows the pointer, and interactive cards and rows show a subtle local glow where the pointer is.
The effect never sits on top of text in a way that reduces readability, never intercepts clicks,
taps, scrolling, or keyboard focus, and stops entirely when the pointer is still. On touch devices,
or when the user prefers reduced motion, the interface shows a polished static appearance instead.

**Why this priority**: It delivers the distinctive "Ambient Atelier" identity requested, but the
application is fully usable without it.

**Independent Test**: In a real browser, move the pointer across the background and over queue
rows and review cards and observe the light; then keep the pointer still and confirm nothing keeps
animating; then enable reduced motion and use a touch-emulated viewport and confirm a static,
finished look. Throughout, click, scroll, and tab normally.

**Acceptance Scenarios**:

1. **Given** a pointer device, **When** the pointer moves over the main area, **Then** the
   background light follows smoothly and stays diffused and low in intensity, and it stays
   centered under the pointer after the page has been scrolled.
2. **Given** the pointer is over an interactive row or card, **When** it moves, **Then** a subtle
   local glow follows it on that element only; non-interactive panels show no hover glow.
3. **Given** the pointer stops, **When** observed for several seconds, **Then** no visual element
   keeps animating.
4. **Given** the reduced-motion preference or a touch-only device, **When** any screen is shown,
   **Then** the light does not follow input and a static composition with the same colors is shown.
5. **Given** the effect is active, **When** the clinician clicks, taps, scrolls, selects text, or
   uses the keyboard, **Then** every action behaves exactly as without the effect.

---

### User Story 4 - A cohesive, art-directed identity (Priority: P3)

Across the shell, queue, review, and decided screens, the application reads as one deliberate
design: a dark navigation shell, light and opaque content surfaces for clinical information, a
clear type hierarchy, consistent spacing, restrained corners and shadows, and selective use of
gradients and translucency. The fictional-data and no-sign-in notices and the live-update
indicator remain visible in the new design.

**Why this priority**: Visual cohesion is the brief's quality bar, but it depends on stories 1–3
being correct first.

**Independent Test**: Review screenshots of every screen and state at desktop and phone widths
against the requirements below; check that clinical information never sits on a translucent or
gradient surface that lowers its contrast.

**Acceptance Scenarios**:

1. **Given** any screen, **When** shown, **Then** navigation sits in a dark shell and clinical
   information sits on light, opaque surfaces.
2. **Given** any screen, **When** shown, **Then** the fictional-data notice, the no-sign-in notice,
   and the live-update indicator states remain visible and readable.

---

### Edge Cases

- Long patient or medication names, many allergies, and many other active prescriptions wrap
  without overlapping or causing horizontal scrolling.
- At 200% browser zoom, blockers, warnings, the note field, and decision buttons remain visible
  and usable.
- Windows high-contrast (forced colors) mode keeps focus indicators, badges, and safety sections
  distinguishable; decorative lighting disappears without leaving content unreadable.
- A browser without pointer support, or with the pointer outside the window, shows the static
  composition.
- The live-update indicator in its reconnecting and unavailable states remains noticeable on the
  dark shell.
- Very slow devices: the lighting effect must not make scrolling or typing in the note field feel
  sluggish.
- Printing or screenshots: decorative layers must not obscure content.

## Requirements *(mandatory)*

### Functional Requirements

**Preserved behavior (non-negotiable)**

- **FR-001**: All existing behaviors MUST be unchanged: data loaded and shown, routing, status
  switching, search, pagination, live updates and reloads, Refresh, approve and deny flows, server
  note validation and its messages, blocked-approval and conflict handling, decided read-only
  views, and every message text the existing tests check.
- **FR-002**: The redesign MUST NOT evaluate any safety rule in the browser, add client-side note
  validation, or rely on visuals or disabled buttons in place of server enforcement.
- **FR-003**: No invented data MAY be shown: no analytics, counts, patients, or medication details
  beyond what the application already receives.
- **FR-004**: The fictional-data notice and no-sign-in notice MUST remain visible on every screen.

**Visual identity**

- **FR-005**: The design MUST use named semantic tokens for background, surface, text, border,
  focus, and status roles, derived from the brief's palette (midnight navy, clinical mist, clinical
  teal, creative indigo, success, warning, danger). Where a palette color fails contrast for its
  role, a darker or lighter variant of the same hue MUST be used for that role.
- **FR-006**: All text and essential icons MUST meet WCAG 2.1 AA contrast (4.5:1 normal text, 3:1
  large text and non-text indicators) against the surface they sit on, including on the dark shell.
- **FR-007**: Navigation and the app shell MUST use a dark surface; clinical information MUST sit
  on light, opaque surfaces. Gradients and translucency MAY be used only for decoration and
  non-clinical framing.
- **FR-008**: Spacing MUST follow a 4-pixel scale; typography MUST define distinct levels for page
  title, section heading, card title, body, and metadata, sized so the queue and review show at
  least as much clinical information per screen as before.

**Queue**

- **FR-009**: The queue MUST present title, description, status switcher with the exact count for
  the selected status, search, the Pending safety summary, rows, and pagination in a clear hierarchy
  with compact, aligned rows.
- **FR-010**: Rows MUST have a subtle hover state and a clearly visible keyboard-focus state; the
  whole row remains the click target as today.
- **FR-011**: Loading, empty, no-match, and error-with-retry states MUST each have a distinct
  design.
- **FR-012**: Only the selected status's exact count MUST be shown; counts for the other
  statuses MUST NOT be shown or estimated, because they are not loaded (current behavior; no new
  requests). *Default adopted at planning (2026-10-20): the clarification question was not
  answered, so the recommended option A was used; fetching all three totals can be added later as
  a separate data change.*

**Review**

- **FR-013**: The review MUST show, in a safety-first order, the safety state (blocking issues,
  nonblocking warnings, or no issues found), refill-timing eligibility, interaction warnings,
  patient identity and allergies, the requested prescription (drug, dose, remaining refills, last
  filled), and other active prescriptions.
- **FR-014**: Blocking, warning, and no-issues states MUST each be distinguishable by an explicit
  text label and an icon, not color alone, with readable explanatory text.
- **FR-015**: The no-issues state MUST use neutral wording and styling that does not claim approval
  is safe; warnings MUST NOT be styled to look minor or dismissible.
- **FR-016**: Decision controls MUST be easy to find on wide screens and MUST follow the content in
  a logical vertical order on narrow screens; no sticky or fixed element MAY cover warnings,
  blockers, or the note field on narrow screens.

**Ambient lighting**

- **FR-017**: The main area MUST show one or a few soft, diffused background light layers in teal
  and indigo hues whose position follows the pointer on pointer devices and stays aligned with the
  pointer at any scroll position.
- **FR-018**: Selected interactive elements (queue rows and interactive cards only) MUST show a
  subtle local glow under the pointer and MAY lift very slightly; non-interactive panels MUST NOT.
- **FR-019**: The effect MUST NOT intercept clicks, taps, scrolling, text selection, or focus, and
  MUST NOT reduce the contrast of any text, badge, or icon below FR-006.
- **FR-020**: Nothing MAY animate while the pointer is still; the effect MUST NOT run continuous
  animation.
- **FR-021**: With the reduced-motion preference, on touch-only devices, or without pointer
  support, the light MUST NOT follow input and a static composition MUST be shown.
- **FR-022**: The effect MUST be one reusable mechanism shared by the shell and all interactive
  elements, not separate per-element handling, and MUST release all its listeners and pending work
  when its host is destroyed.
- **FR-023**: No third-party package MAY be added for the effect.

**Motion**

- **FR-024**: Interactive transitions MUST last about 180–300 ms with consistent easing; entrance
  animation MAY be used sparingly and MUST NOT be applied to each queue row individually.
- **FR-025**: No information MAY be revealed only through animation; with reduced motion, all
  non-essential motion MUST be removed while every state remains visible.
- **FR-026**: No bouncing, flashing, perpetual motion, parallax, or animated gradients behind
  content.

**Accessibility and responsive behavior**

- **FR-027**: Every control MUST remain keyboard-operable with a visible focus indicator and an
  accessible name; nothing MAY require a mouse.
- **FR-028**: Screens MUST have no horizontal scrolling at 360 px width and at 200% zoom on a
  1280 px window; touch targets MUST be at least 44 × 44 px on narrow screens.
- **FR-029**: Forced-colors (high contrast) mode MUST keep focus indicators, badges, and safety
  sections distinguishable.

### Key Entities

No data entities change. The redesign displays the existing request list, request detail,
evaluation (blocking reasons and warnings), patient profile, prescriptions, and decision outcomes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing frontend and backend tests pass without weakening any assertion;
  any test whose markup expectation must change is updated with equal or stronger assertions and
  the change is documented.
- **SC-002**: Every text/background pair used in the design meets WCAG 2.1 AA, verified by
  calculation for each token pair and by an automated audit of the queue and review screens.
- **SC-003**: For each of the five demo review states, a grayscale screenshot shows the safety
  state's text label and its icon (blocked, warnings, no issues), so the state is identifiable
  without color. The result is recorded per scenario (label seen, icon seen, pass/fail) in the
  quickstart Results table.
- **SC-004**: At 360 px and 1280 px widths and at 200% zoom, the queue and all review states show
  no horizontal scrolling and keep blockers, warnings, the note field, and decision buttons visible.
- **SC-005**: With the pointer still for 5 seconds, no element is animating; with reduced motion or
  touch emulation, the light does not move at all.
- **SC-006**: Clicking, tapping, scrolling, and keyboard navigation succeed on every interactive
  element with the lighting active, verified in a real browser.
- **SC-007**: The queue shows at least as many request rows within a 1280 × 800 view as before the
  redesign.
- **SC-008**: Screenshots of the queue and every review state at desktop and phone widths, with
  the lighting visible, are captured from the running application and reviewed; visible weaknesses
  are fixed before completion.

## Assumptions

- Presentation only: no API, data, routing, or business-rule changes, building on the 002
  design-system files rather than replacing the project structure.
- Typography uses Inter when it is installed on the device, falling back to the existing system
  font stack. No web fonts, icon fonts, or other files are loaded from external servers (002
  decision: no third-party network calls).
- "Hybrid light/dark" means a dark navigation shell with light content surfaces; a full dark theme
  or theme switch is out of scope. The page stays light-only for the content area.
- The application has no dialogs or menus today; "dialogs and menus keep working" is satisfied
  trivially and no new ones are added.
- Pointer-following applies to the main content region and app shell only, not to form fields or
  text.
- Browser-based visual verification uses the locally installed Edge with automated screenshots
  (as in 002); if live pointer movement cannot be automated, it is checked manually and reported as
  such.
- Feature 003's final verification (T038) is still open; this feature does not depend on it,
  because it changes only presentation, but its own verification must include the paged queue as
  delivered by 003.
