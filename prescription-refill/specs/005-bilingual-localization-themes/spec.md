# Feature Specification: Bilingual Localization and Theme Preferences

**Feature Branch**: `005-bilingual-localization-themes`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "Bilingual Localization and Theme Preferences: Arabic and English UI
localization with Arabic as the default, RTL/LTR layout switching, SQL Server reference-data
localization using `NameAr` and `NameEn` columns, AutoMapper `ProjectTo<TDto>()` returning a single
localized `Name` selected in the SQL query, ASP.NET Core request culture resolution from the
selected language, Light and Dark themes with independent persisted preferences, accessible and
responsive in both languages and themes. Business rules, error codes, status values, approval,
refill, transaction, concurrency, and live-update behavior unchanged."

**Context**: Feature 001 defines the behavior and safety rules. Feature 002 introduced the shared
design system and recorded "light theme only" and "localization out of scope". Feature 003 moved
paging, search, and counts to the server. Feature 004 introduced the "Ambient Atelier" identity
and recorded "a full dark theme or theme switch is out of scope". This feature lifts those three
exclusions and adds language and theme as presentation preferences. It does not change what the
system decides, only the language and appearance used to show it. Requirements cited as
"001 FR-xxx", "002 FR-xxx", "003 FR-xxx", and "004 FR-xxx" stay in force unless this spec states
an amendment. The conflicts table lists every amendment.

## Clarifications

### Session 2026-10-21

- Q: Should drug-interaction descriptions be bilingual? → A: Yes. Each interaction stores an Arabic
  and an English description, so safety warnings appear entirely in the selected language
  (FR-024).
- Q: Which medication name does queue search match? → A: Only the name in the currently selected
  language, so results match the names shown (FR-025).
- Q: How should the AutoMapper dependency be handled, given it is not installed? → A: AutoMapper
  and `ProjectTo` stay required. Before a version is adopted, its compatibility with the installed
  .NET and EF Core versions and its license terms must be verified. No paid commercial license is
  assumed, and no commercial-license dependency is added without first reporting the exact
  version, terms, and implications. An eligible free license is documented with its conditions.
  If no acceptable compatible option exists, work stops and alternatives are reported before the
  requirement changes (Dependency gate DG-1).
- Q: Which free AutoMapper license does the project use? → A: The Lucky Penny Community License.
  The author confirms eligibility (under US $5M annual gross revenue and no more than US $10M
  outside capital, or a non-profit under US $5M, and not a government body) and that the current
  license agreement has been read. A free key is registered and supplied through configuration or
  user secrets (DG-1 conditions 3 and 4).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Work in Arabic by default (Priority: P1)

A clinician opens the application for the first time and it appears in Arabic, laid out
right-to-left. The navigation, queue, review screen, safety sections, decision area, messages,
notices, and medication names are in Arabic. Patient names, dosage text, and decision notes are
shown exactly as recorded. The clinician reviews and decides a request exactly as before.

**Why this priority**: Arabic is the required default. It is the first experience for every
clinician, and a decision made in Arabic must be as safe as one made in English.

**Independent Test**: In a fresh browser with no saved preferences, open the queue and a review of
each seeded scenario type. Confirm all interface text is Arabic, the layout is right-to-left,
medication names are the Arabic names, patient-entered text is unchanged, and approve and deny
behave exactly as specified in 001.

**Acceptance Scenarios**:

1. **Given** a browser with no saved language preference, **When** any screen loads, **Then** it
   is in Arabic, the document language is Arabic, and the reading direction is right-to-left.
2. **Given** the Arabic interface, **When** a review of a blocked request is shown, **Then** the
   blocking heading, each blocking reason, the eligible-from time, and the disabled-Approve
   explanation are in Arabic, and the same rule outcome is shown as in English.
3. **Given** the Arabic interface, **When** a request has an allergy or interaction warning,
   **Then** the warning text and the medication name it refers to are in Arabic, and the warning
   is still labeled as non-blocking.
4. **Given** a patient named in Latin script, a dosage written in English, or a decision note in
   either language, **When** shown in the Arabic interface, **Then** the text appears exactly as
   stored, untranslated, and reads in its own direction without scrambling punctuation or
   numbers.
5. **Given** the Arabic interface, **When** an approval is blocked by the server, a concurrency
   conflict occurs, the request was already decided, or the note is invalid, **Then** the
   corresponding message is in Arabic and the screen behaves as 001 FR-011 and FR-032 require.

---

### User Story 2 - Switch to English and keep it (Priority: P1)

A clinician who prefers English selects English from a language control in the header. The
interface switches to English and a left-to-right layout without losing their place. After a
reload or a later visit in the same browser, it stays in English until they switch back.

**Why this priority**: The bilingual requirement is only met when English is fully usable and the
choice persists. Shares P1 with the default because both directions must be correct from the
start.

**Independent Test**: From the Arabic default, switch to English on the queue (with a search, a
status, and page 2 selected) and on a review (with a note typed). Confirm the layout flips, place
and typed text are kept, server-provided text and names reload in English, and the choice
survives a reload.

**Acceptance Scenarios**:

1. **Given** the Arabic interface, **When** the clinician selects English, **Then** all interface
   text becomes English, the document language is English, the direction is left-to-right, and
   medication names and server messages reload in English.
2. **Given** the queue with a status, search text, page number, and page size selected, **When**
   the language is switched, **Then** the same status, search text, page, and page size remain
   selected.
3. **Given** a review with a note typed and not submitted, **When** the language is switched,
   **Then** the typed note is preserved unchanged and the request's data and evaluation are
   reloaded from the server in the new language.
4. **Given** English was selected, **When** the page is reloaded or reopened later in the same
   browser, **Then** it opens in English, left-to-right.
5. **Given** the language control, **When** it is reached by keyboard or assistive technology,
   **Then** it has an accessible name, shows which language is active, and lists each language
   by its own name ("العربية", "English").

---

### User Story 3 - Choose Light or Dark theme independently (Priority: P2)

A clinician selects Light or Dark from a theme control. The whole application, including content
surfaces holding clinical information, switches to the chosen theme. Safety states stay as
distinguishable and readable as in Light. The theme is remembered separately from the language:
changing one never changes the other.

**Why this priority**: Requested, and valuable for long shifts and low-light settings, but the
workflow is complete without it.

**Independent Test**: Try all four combinations (Arabic/Light, Arabic/Dark, English/Light,
English/Dark). Confirm each survives a reload, that switching language leaves the theme alone and
switching theme leaves the language alone, and that every safety state passes the contrast and
non-color checks in both themes.

**Acceptance Scenarios**:

1. **Given** no saved theme preference, **When** the application loads, **Then** the Light theme
   is shown.
2. **Given** the Light theme, **When** the clinician selects Dark, **Then** the shell, content
   surfaces, controls, badges, alerts, and ambient lighting all switch to their dark variants
   without a page reload and without losing place or typed text.
3. **Given** Dark is selected and English is selected, **When** the clinician switches to Arabic,
   **Then** the theme remains Dark; **When** they then switch to Light, **Then** the language
   remains Arabic.
4. **Given** any saved combination of language and theme, **When** the page is reloaded,
   **Then** both are restored, and the page does not briefly show the other theme or direction
   first.
5. **Given** the Dark theme, **When** blocked, warning, no-issues, and decided states are shown,
   **Then** each keeps its explicit label and icon (004 FR-014) and meets the contrast
   requirements.

---

### User Story 4 - Accurate, readable bilingual clinical data (Priority: P2)

Medication names on every screen (queue rows, requested prescription, other active
prescriptions, allergies, warnings) come from the stored Arabic or English name for the active
language. A clinician never sees a name in one language while the rest of the row or warning is
in the other, and never sees a machine-translated clinical term.

**Why this priority**: Wrong or mixed-language medication names on a review screen are a safety
risk. They also show that reference-data localization works end to end.

**Independent Test**: For every seeded medication, check the Arabic and English interfaces show
the stored Arabic and English names respectively in each place a medication name appears, and
that API responses carry one name per medication in the requested language.

**Acceptance Scenarios**:

1. **Given** a medication with an Arabic and an English name, **When** a list or detail is
   requested in Arabic, **Then** the response carries only the Arabic name in that medication's
   single name field; **When** requested in English, **Then** only the English name.
2. **Given** a patient's allergies, **When** shown, **Then** they are listed by the active
   language's medication names and ordered by those names.
3. **Given** a request that asks for an unsupported language or no language, **When** it is
   processed, **Then** Arabic names and messages are returned.

---

### Edge Cases

- No saved preference, a saved value that is not a supported language or theme, or saved data
  that cannot be read (storage disabled or cleared): Arabic and Light are used without an error
  being shown.
- A browser whose own language is English or another language: the first visit still opens in
  Arabic. The browser language does not decide the default.
- A language request such as a regional variant of a supported language ("ar-SA", "en-GB"): it
  resolves to that supported language. Any other language ("fr") resolves to Arabic.
- The language is switched while a list or detail load is in flight: only the response for the
  latest language is displayed (as 003 FR-016 for paging).
- The language is switched while a decision is being submitted: the submission completes once and
  its outcome is shown in the new language. The switch never resubmits or cancels a decision.
- A live update arrives while in either language: the reload it triggers uses the active language.
- Two tabs in the same browser with different selections at the moment one changes: each tab keeps
  showing its current selection until it reloads. On reload it uses the most recently saved
  values.
- Long Arabic medication names and long English names at 360 px width, in both directions, wrap
  without overlap or horizontal scrolling.
- Mixed-direction content: a Latin-script patient name, a dosage like "500 mg twice daily", a UTC
  offset "UTC+03:00", or a note mixing both scripts inside right-to-left text keeps its characters
  and numbers in the correct order.
- Directional icons and controls (back link, Previous/Next pagination, chevrons) point the right
  way in each direction. Non-directional icons (warning, check) are not mirrored.
- Dark theme in forced-colors (high-contrast) mode: safety sections, badges, and focus indicators
  stay distinguishable (004 FR-029).
- Dark theme with reduced motion or touch input: the static composition (004 FR-021) uses dark
  colors.
- Printing in Dark theme: content prints legibly on a light background.
- A medication missing its name in one language cannot exist: both names are required for every
  medication.

## Requirements *(mandatory)*

### Functional Requirements

**Preserved behavior (non-negotiable)**

- **FR-001**: Language and theme MUST NOT change any business rule, rule outcome, request status
  value, refill count, decision, transaction, concurrency behavior, or live-update message. The
  same stored data and clock produce the same evaluation in either language (constitution
  Principle II).
- **FR-002**: Error codes, blocking-reason codes, warning codes, status values, and every other
  machine-readable value in the API MUST be identical in both languages. Only human-readable text
  changes. Clients MUST be able to rely on codes alone.
- **FR-003**: Live-update notifications MUST stay as defined in 001 FR-029: request identifier and
  kind of change only, with no language-specific content. Screens reload in their own active
  language.
- **FR-004**: Patient-entered and prescription-recorded text MUST NOT be translated,
  transliterated, or altered: patient names, dosage text, and decision notes are shown and
  returned exactly as stored, in both languages.
- **FR-005**: No text MAY be produced by automatic or machine translation at runtime. All
  translated interface text and all localized reference-data names MUST be authored in advance.
- **FR-006**: All existing behaviors in 001–004 MUST keep working in both languages and both
  themes.

**Language selection and resolution**

- **FR-007**: The supported languages MUST be Arabic and English. Arabic MUST be the default.
- **FR-008**: The interface MUST offer a language control in the application header on every
  screen. It MUST be keyboard-operable, have an accessible name, indicate the active language, and
  name each language in that language.
- **FR-009**: The selected language MUST persist in the browser across reloads and later visits,
  independently of the theme. Without a valid saved value, Arabic MUST be used. The browser's own
  language setting MUST NOT determine the default.
- **FR-010**: Every request from the interface to the server MUST state the active language. The
  server MUST resolve each request to exactly one supported language as follows: a supported
  language or a regional variant of one resolves to that language. A missing, empty, malformed, or
  unsupported value resolves to Arabic. The same input MUST always resolve to the same language.
- **FR-011**: The resolved language MUST govern all human-readable text in that response: blocking
  reason and warning messages, validation messages, error titles and details, and localized
  reference-data names. It MUST NOT affect parsing of request values, which stays
  language-independent (numbers, dates, identifiers, status values).
- **FR-012**: Switching language MUST NOT require a full page reload. It MUST keep the current
  route, queue status, search text, page number, page size, and any typed, unsubmitted note. It
  MUST reload the displayed data from the server in the new language (amends 001 FR-023: a
  language switch is an additional reload trigger).

**Direction and layout**

- **FR-013**: Arabic MUST render right-to-left and English left-to-right. The document language and
  direction MUST be set accordingly so assistive technology announces content in the right
  language.
- **FR-014**: Layout MUST mirror between directions: reading order, alignment, the header, the
  review screen's two-column arrangement, row layouts, pagination, and directional icons. Icons
  without a direction MUST NOT be mirrored.
- **FR-015**: Embedded text in the other direction MUST keep its own correct order. This covers
  patient names, dosage text, notes, medication names, numbers, and time offsets. Neighboring
  punctuation and numbers MUST NOT be reordered.
- **FR-016**: Numbers, dates, and times MUST be shown with Western digits (0–9) in both languages
  so doses, refill counts, and dates read the same in either language. Month names and labels
  follow the active language. The 001 FR-024 format (date, 24-hour time, UTC offset, exact UTC on
  hover) is kept, with localized month names (amends 001 FR-024 example only).

**Interface text**

- **FR-017**: Every piece of interface text MUST exist in both languages: labels, headings,
  buttons, hints, empty/error/loading states, alert messages, accessible names, live-region
  announcements, page titles, and the fictional-data and no-sign-in notices (002 FR-004, 004
  FR-004). The English wording stays as it is today. The Arabic wording MUST carry the same
  meaning, including that warnings do not block approval and that "no issues found" does not
  claim the request is safe (004 FR-015).
- **FR-018**: When the interface displays a server message tied to a known code, the meaning shown
  MUST be the same in both languages. No message MAY be shown in a language other than the active
  one, except patient-entered text (FR-004).

**Reference-data localization**

- **FR-019**: Every medication MUST store a required Arabic name and a required English name. Each
  name MUST be unique within its language. The existing medication names become the English names.
  Every seeded medication MUST receive an authored, clearly synthetic Arabic name (001 FR-019).
- **FR-020**: Bilingual reference data MUST be stored as one Arabic and one English value on the
  reference record itself. There MUST be no separate translation table.
- **FR-021**: Each API response MUST expose a single name per reference item in the resolved
  language. Responses MUST NOT expose both language variants. Existing response field names and
  shapes MUST stay as they are; only their values become language-dependent.
- **FR-022**: The localized name MUST be chosen while the data is read from the database, so only
  the resolved language's name is retrieved for list and detail responses. It MUST NOT be chosen
  by loading both and discarding one.
- **FR-023**: Medication names inside warnings and allergy lists MUST use the same resolved-language
  name as everywhere else in the response. Allergy lists MUST be ordered by the resolved-language
  name.
- **FR-024**: Every drug interaction MUST store a required Arabic description and a required
  English description on the interaction record itself, each at most the existing description
  length (300 characters). The existing descriptions become the English descriptions. Every seeded
  interaction MUST receive an authored, clearly synthetic Arabic description. A warning MUST carry
  a single description in the resolved language and never both, chosen while the data is read
  (the same rules as FR-021 and FR-022). A warning MUST contain no text in the other language,
  except patient-entered text (FR-004).
- **FR-025**: Queue search (003 FR-007) MUST match the medication name in the resolved language
  only. A medication MUST NOT be found by its name in the other language. Patient-name matching is
  unchanged. Case and surrounding-space rules (003 FR-007, FR-008), paging and totals, and logging
  restrictions (003 FR-009) are unchanged (amends 003 FR-007).

**Themes**

- **FR-026**: The interface MUST offer Light and Dark themes through a theme control in the header
  on every screen. The control MUST be keyboard-operable, have an accessible name in the active
  language, and indicate the active theme.
- **FR-027**: The selected theme MUST persist in the browser across reloads and later visits,
  independently of the language. Without a valid saved value, Light MUST be used.
- **FR-028**: On load, the saved language, direction, and theme MUST be applied before content is
  first shown. No flash of the other theme or direction is allowed.
- **FR-029**: Both themes MUST be defined through the shared semantic tokens (002 FR-006, 004
  FR-005). Screens MUST NOT hard-code theme colors. Each theme MUST give every role (background,
  surface, text, border, focus, accent, and the danger, warning, success, and info states) a value.
- **FR-030**: In the Dark theme, clinical information MUST sit on opaque surfaces with contrast at
  least equal to the Light theme's requirements. Decorative gradients, translucency, and ambient
  lighting MUST NOT reduce text, badge, or icon contrast below FR-031 (amends 004 FR-007 "light,
  opaque surfaces" to "opaque surfaces" in Dark).
- **FR-031**: In both themes, all text and meaningful non-text elements MUST meet WCAG 2.1 AA
  contrast: 4.5:1 for normal text, and 3:1 for large text, icons, input borders, and focus
  indicators (002 FR-008, 004 FR-006 apply to each theme).
- **FR-032**: In both themes, blocked, warning, no-issues, success, and info states MUST stay
  distinguishable by label and icon, not color alone (004 FR-014).
- **FR-033**: Printed output MUST be legible regardless of the selected theme.

**Accessibility and responsive behavior**

- **FR-034**: In every language and theme combination, both screens MUST have no horizontal
  scrolling at 360 px and 1280 px and at 200% zoom on a 1280 px window. Touch targets stay at least
  44 × 44 px on narrow screens, and keyboard focus order follows the visual reading order of the
  active direction (constitution Principle VII; 004 FR-027, FR-028).
- **FR-035**: Language and theme changes MUST be announced to assistive technology in the newly
  active language. Focus MUST stay on the control that was used.
- **FR-036**: Reduced-motion, touch, and forced-colors behaviors (004 FR-021, FR-025, FR-029) MUST
  hold in both themes and both directions. Ambient pointer-following MUST track the pointer
  correctly in right-to-left layouts.

### Key Entities

- **Medication** (changed): a synthetic drug with a required Arabic name and a required English
  name, each unique within its language. It replaces the single name.
- **Drug Interaction** (changed): a synthetic pair of medications with a required Arabic
  description and a required English description. It replaces the single description (FR-024).
- **Language Preference** (browser-held, not stored on the server): Arabic or English. The default
  is Arabic.
- **Theme Preference** (browser-held, not stored on the server): Light or Dark. The default is
  Light. It is independent of Language Preference.
- **Resolved Request Language** (per request, transient): the one supported language the server
  uses for a request's human-readable text and names.
- Unchanged: Patient, Prescription, Allergy, Refill Request, Evaluation, and Request Notification
  keep their data. Evaluation messages are rendered in the resolved language. Their codes are
  unchanged.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a fresh browser, 100% of first visits open in Arabic, right-to-left, Light,
  whatever the browser's language.
- **SC-002**: Across all four language × theme combinations, each selection is restored after a
  reload in 100% of attempts. Changing one preference leaves the other unchanged in 100% of
  attempts.
- **SC-003**: For every seeded scenario, the rule outcome, codes, statuses, refill counts, and
  decision results are identical in Arabic and English (automated comparison: 0 differences
  outside human-readable text).
- **SC-004**: A text audit of every screen and state finds 0 untranslated interface strings in
  either language, and 0 altered patient names, dosages, or notes.
- **SC-005**: Every API response sampled for each language contains exactly one name per
  medication and one description per interaction warning, in the requested language. A search
  by a medication's name in the other language returns no match for that medication. Requests with no language, an unsupported language, or
  a malformed language return Arabic in 100% of test cases.
- **SC-006**: Every text and non-text token pair in both themes meets WCAG 2.1 AA, verified by
  calculation per pair and by an automated audit of the queue and review screens in all four
  combinations.
- **SC-007**: At 360 px, 1280 px, and 200% zoom, all four combinations show no horizontal
  scrolling and keep blockers, warnings, the note field, and decision buttons visible.
- **SC-008**: A clinician can switch language or theme in one action from any screen, and the
  change is visible within 1 second without losing place or typed text.
- **SC-009**: Producing a localized list or detail response is no slower than today by more than
  10% at the seeded data size.
- **SC-010**: All existing automated tests pass without weakened assertions. Tests that check
  English text either select English explicitly or gain an Arabic equivalent, and each change is
  documented.

## Constraints and Dependencies

Mandated by the request. These are recorded here for planning rather than restated as
requirements.

- **Architecture**: The existing ASP.NET Core API, Angular frontend, EF Core with the SQL Server
  provider, and Microsoft SQL Server stay as they are (constitution Principles I, IV, VII). Schema
  changes ship as EF Core migrations, and tests run against SQL Server (Principle V).
- **Request culture**: Language resolution (FR-010) uses ASP.NET Core request localization with
  Arabic as the default culture and Arabic and English as the only supported cultures.
- **Projection**: Localized names (FR-021, FR-022) use AutoMapper `ProjectTo<TDto>()`. The
  mapping selects `NameAr` or `NameEn` for the resolved culture inside the SQL query, and must
  translate to SQL in EF Core without client evaluation.
- **Columns**: Bilingual reference data uses `NameAr` and `NameEn` columns on `Medications`, and
  `DescriptionAr` and `DescriptionEn` columns on `DrugInteractions`. Both follow the same pattern
  (FR-019, FR-020, FR-024).
- **Package versions**: Installed framework and package versions are reused: .NET 10, EF Core
  10.0.13, Angular 19, @microsoft/signalr 10. AutoMapper is the only new package, and DG-1
  applies to it.

### Dependency gate DG-1: AutoMapper

AutoMapper MUST NOT be added until every condition below is met and recorded in the plan's
constitution check.

1. **Version**: The version targets `net10.0`, has no open security advisory, and its
   dependencies resolve against the installed central package versions without downgrading
   them.
2. **EF Core compatibility**: A `ProjectTo` test against SQL Server shows that the
   localized-name and localized-description selection translates to SQL with no client
   evaluation. Only the resolved language's column is selected.
3. **License**: The author confirms in writing which license the project uses and that the
   project is eligible for it. No paid commercial license is assumed. **Decision recorded
   2026-10-21**: the Community License, with eligibility and the current license agreement
   confirmed by the author (see Clarifications). **Met 2026-10-21**: the 16.2.0 license
   metadata, the current terms (License Agreement version 2.0, section 4.2(g)), and the key
   configuration were verified from the sources and recorded in the plan's research R1 (task
   T001).
4. **License key**: The Community License key comes from configuration or user secrets. It is
   never committed to the repository or exposed to the browser. The behavior when the key is
   missing in development and tests is documented.

If no version satisfies conditions 1–3, work stops and the alternatives are reported to the
author before this requirement changes.

**Findings recorded 2026-10-21** (verify again at planning; terms can change):

| Version | Targets net10.0 | Advisory status | License |
|---------|-----------------|-----------------|---------|
| 14.0.0 (last MIT release) | No net10.0-specific target listed | Affected by GHSA-rvv3-g6hj-g44x / CVE-2026-32933 (high severity, uncontrolled recursion causing process crash). Reported as unpatched in 14.x. | MIT. **Rejected** under condition 1. |
| 15.1.3 | Yes | Patched (fixed from 15.1.1) | Dual RPL-1.5 / Lucky Penny commercial |
| 16.2.0 (latest, 2026-07-02) | Yes (net8.0 / net9.0 / net10.0) | No advisory shown (fixed from 16.1.1) | Dual RPL-1.5 / Lucky Penny commercial |

- **Candidate**: 16.2.0. Its dependencies are Microsoft.Extensions.Logging.Abstractions ≥ 10.0.0
  (10.0.13 is installed), Microsoft.Extensions.Options ≥ 10.0.0, and
  Microsoft.IdentityModel.JsonWebTokens ≥ 8.14.0. The last two are new transitive packages. The
  JsonWebTokens package is used for license-key validation.
- **License options** (Community License selected 2026-10-21; the others are not used):
  - **Community License (free, selected)**: Requires annual gross revenue under US $5M and no more than
    US $10M of outside capital, or a registered non-profit with an annual budget under US $5M.
    Government and quasi-government agencies are excluded. Registration for a license key is
    required. Earlier published terms also listed educational and non-production use. Whether
    those categories still apply must be confirmed against the current license agreement.
  - **RPL-1.5 (free, reciprocal)**: Requires releasing the source of software built with the
    library under compatible terms. It is acceptable only if the author accepts that obligation
    for this project.
  - **Commercial (paid)**: Tiered by team size. Not approved.
- **Missing key**: AutoMapper keeps working. It writes license warnings to the logs under the
  `LuckyPennySoftware.AutoMapper.License` category.
- **Agreement text**: It could not be read during specification. The author has confirmed reading
  the current agreement (Clarifications, 2026-10-21).

## Conflicts and Dependencies on Existing Features

| # | Existing item | Conflict or dependency | Resolution in this spec |
|---|---------------|------------------------|-------------------------|
| C1 | Request: "reuse installed packages" vs. "use AutoMapper `ProjectTo`" | **AutoMapper is not installed** (`Directory.Packages.props`). DTOs are mapped by a hand-written `DtoMapper`. The last MIT release (14.0.0) has an unpatched high-severity advisory. Patched releases (15.1.1+, 16.1.1+) are dual RPL-1.5 / commercial. | Resolved 2026-10-21. AutoMapper stays required, as the one new package, subject to DG-1 (version, EF Core translation, license eligibility, key handling). The plan records it in the constitution check (Principle I: new dependency with a current consumer). |
| C2 | 001/003 evaluation path | List and detail responses run Domain rules on loaded entities (Principle II), so they cannot be pure database projections. Warnings carry a medication name that the Domain produces. | `ProjectTo` covers the name and display fields. Evaluation still loads the entities the rules need. Localized names in warnings follow FR-023. Rule logic stays in Domain. |
| C3 | Domain-built message text (`BlockingReason.Message`, `Warning.Message`, validation messages) | The messages are English strings created in Domain. Localization is a presentation concern (Principle I). | The plan moves message text to the API/Application edge, keyed by the existing codes, with no change to rule logic or codes (FR-002, FR-011). |
| C4 | 002 Assumptions ("light theme only", "localization out of scope"); 004 Assumptions ("full dark theme or theme switch is out of scope") | Directly lifted by this feature. | Superseded for 005 only. 002 and 004 design-system rules otherwise apply to both themes (FR-029–FR-032). |
| C5 | 004 FR-007 ("clinical information MUST sit on light, opaque surfaces") | Incompatible with a Dark theme. | Amended: in Dark, "opaque surfaces" with equal contrast requirements (FR-030). |
| C6 | 002 FR-004, 004 FR-001 ("current wording", "every message text the existing tests check") | With Arabic as the default, tests and notices no longer show English unless English is selected. | English wording kept unchanged. Arabic equivalents added. Tests select a language explicitly (FR-017, SC-010). |
| C7 | 003 FR-002 (a request without paging values behaves "exactly as today") and existing API clients | A client sending no language now receives Arabic names and messages instead of English. Shapes, codes, and order are unchanged. | Accepted consequence of "Arabic default" (FR-010). Documented in the API contract and README. |
| C8 | 003 FR-007 search on medication name | With two names, the meaning of "medication name" is ambiguous. | Amended: resolved-language name only (FR-025). |
| C14 | `DrugInteraction` single `Description` (max 300) | The schema and entity change to two required descriptions. | Migration maps the existing description to the English description and adds authored Arabic descriptions (FR-024). |
| C9 | 001 FR-023 evaluation reload triggers | A language switch must refresh server-provided text. | A language switch is added as a reload trigger (FR-012). Rules and polling are unchanged. |
| C10 | 001 FR-024 time format example (`20 Oct 2026, 14:05 (UTC+03:00)`) | English month abbreviation. | Format kept. Month names localized. Western digits in both languages (FR-016). |
| C11 | 001 FR-020 (no authentication or user identity) | "Persisted user preferences" cannot be per-user on the server. | Preferences are per browser (FR-009, FR-027). No server-side preference storage. |
| C12 | Medication unique index on the single name; `Medication` constructor takes one name | The schema and entity change to two required names. | Migration maps the existing name to the English name and adds authored Arabic names (FR-019). Uniqueness applies per language. |
| C13 | `index.html` declares `lang="en"` | It contradicts the Arabic default. | Covered by FR-013 and FR-028. |

No constitution principle is violated. The constitution needs no amendment: language and theme
are presentation concerns, and every rule stays in Domain.

## Assumptions

- Preferences are stored per browser because there is no user identity (001 FR-020). They are not
  synced across browsers or devices.
- The Light theme is the default for determinism and matches today's appearance. Following the
  operating system's color scheme is out of scope.
- Western digits are used in Arabic for safety-critical numbers, because doses, counts, and dates
  must read identically in both languages.
- Arabic medication names in seed data are authored, synthetic demo names, clearly marked as not
  clinical guidance (001 FR-019).
- Arabic search matching is a literal "contains" match. Arabic has no letter case, and diacritic
  or letter-form normalization (for example, alef variants) is out of scope.
- Patients, prescriptions, and allergies have no bilingual fields. Patient name and dosage are
  recorded text (FR-004).
- Request status values are shown with translated labels. The underlying values are unchanged
  (FR-002).
- No new fonts are loaded from external servers (002 and 004 decision). Arabic text uses
  installed system fonts that support Arabic.
- Out of scope: languages other than Arabic and English, per-user server-side preferences,
  translating patient-entered data, an interface for editing translations or medication names,
  automatic theme switching by time of day or operating-system setting, and right-to-left
  variants of seeded patient names.
