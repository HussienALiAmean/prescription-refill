---

description: "Task list for 005 Bilingual Localization and Theme Preferences"
---

# Tasks: Bilingual Localization and Theme Preferences

**Input**: Design documents from `/specs/005-bilingual-localization-themes/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/http-api.md](contracts/http-api.md),
[contracts/ui-contract.md](contracts/ui-contract.md), [quickstart.md](quickstart.md)

**Tests**: Required. Spec SC-003, SC-005, SC-006, and SC-010, constitution Principle V, and
research R15 call for them. Within each phase, test tasks come first and must fail before the
implementation they cover.

**Organization**: Tasks are grouped by user story. The localization plumbing is shared, so the
backend catalog, culture resolution, schema, projections, and the frontend i18n core are in
Phase 2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task).
- **[Story]**: US1–US4 from spec.md.
- Paths are repository-relative. Backend code is in `src/` and `tests/`, and the frontend is in
  `frontend/src/`.

## Standing rules for every task

- English output stays byte-identical when English is selected. Copy English strings verbatim
  from the current code. Never re-type them.
- No rule, code, status, transaction, `rowversion`, or SignalR change (spec FR-001–FR-003).
- Arabic texts come from [contracts/http-api.md](contracts/http-api.md) (server) and the glossary
  in [contracts/ui-contract.md](contracts/ui-contract.md) (UI).
- Never commit the AutoMapper license key or expose it to the browser. Use only fictional data.
- Report an unrun suite or check as **not run**, never as passed.

---

## Phase 1: Setup

**Purpose**: Record the dependency decision, take the baseline, and add the one new package.

- [X] T001 Verify DG-1 condition 3 before any AutoMapper task. The author's decision (Community
  License, eligibility confirmed) is recorded in spec Clarifications 2026-10-21, but verification
  is **pending**. Do all of the following and record the evidence in [research.md](research.md) R1:
  - The exact package version, 16.2.0, from its nuget.org page: its target frameworks, its
    dependencies, and the license expression or license file it ships with.
  - The Lucky Penny license terms that apply to 16.2.0: the URL, the retrieval date, and the
    Community License conditions as written (revenue, outside capital, non-profit, government
    exclusion, and anything else listed).
  - The configuration that 16.2.0 expects for the key (property name and registration call)
    from the official AutoMapper 16 documentation, confirming or correcting the
    `AutoMapper:LicenseKey` design in R1, and the documented behavior when no key is set.
  
  Then update the status of condition 3 consistently in research.md R1 (table row 3),
  [plan.md](plan.md) (Summary "Gate" paragraph, "Open gate" paragraph, and Implementation Order
  step 5), [quickstart.md](quickstart.md) (gate step 1), and spec.md DG-1 condition 3. Mark it
  **met** only if all three items were actually confirmed from the sources. If any item cannot be
  confirmed, or the terms conflict with the author's stated eligibility, leave it **open**,
  record what is missing, and stop T003–T004 and T034–T040. Do not change any other decision.
- [X] T002 Run the baseline: `dotnet test`, then `cd frontend && npm run test:ci && npm run build`.
  Record the pass counts, and take the V16 "before" timing: the median of 20 list and 20 detail
  calls against a throwaway seeded database on a free port (quickstart Prerequisites). Record the
  results in a new `## Baseline` section of `specs/005-bilingual-localization-themes/quickstart.md`.
- [X] T003 Add `<PackageVersion Include="AutoMapper" Version="16.2.0" />` to
  `Directory.Packages.props` and `<PackageReference Include="AutoMapper" />` to
  `src/PrescriptionRefill.Application/PrescriptionRefill.Application.csproj` only. Run
  `dotnet restore` and `dotnet list package --vulnerable --include-transitive`. Confirm there are
  no vulnerabilities and no downgrade of a centrally pinned version (DG-1 condition 1). Record the
  output in research.md R1.
- [X] T004 Keep the license key out of the repository and out of test logs (DG-1 condition 4).
  Loading the key from configuration is T035; this task does not read it.
  - Add no key or placeholder value to `src/PrescriptionRefill.Api/appsettings.json` or
    `src/PrescriptionRefill.Api/appsettings.Development.json`.
  - Filter the `LuckyPennySoftware.AutoMapper.License` log category in
    `tests/PrescriptionRefill.IntegrationTests/Infrastructure/RefillApiFactory.cs`, so a missing
    key in tests produces no captured warnings.
  - The user-secrets setup is documented in T091, and the final leak check is T087.

**Checkpoint**: The baseline is recorded. DG-1 conditions 1, 3, and 4 are recorded as verified in
research R1 and the package restores cleanly, or T001 has left condition 3 open and the
AutoMapper tasks are stopped.

---

## Phase 2: Foundational (blocking prerequisites)

**Purpose**: Codes-only Domain, the server catalog, culture resolution, the bilingual schema,
per-language projections, and the frontend i18n core. Every user story depends on this phase.

**⚠️ CRITICAL**: No user story work begins until this phase is complete. The English API output
must be unchanged when this phase ends.

### Tests for Phase 2 (write first)

- [X] T005 [P] Update `tests/PrescriptionRefill.Domain.Tests/Evaluation/RefillEvaluatorTests.cs` to
  assert codes, `InvalidData` kinds (`DaysSupplyNotPositive`, `LastFilledMissing`,
  `LastFilledAfterEvaluation`), and the referenced `Medication`/`DrugInteraction` instead of
  message strings. Every existing outcome assertion stays as it is (research R6).
- [X] T006 [P] Update `tests/PrescriptionRefill.Domain.Tests/RefillRequests/RefillRequestDecisionTests.cs`
  to assert `DecisionResult.InvalidNote` with `NoteError.Required`/`NoteError.TooLong`. Update
  `tests/PrescriptionRefill.Domain.Tests/RefillRequests/CreateRefillRequestTests.cs` to assert
  `CreateRequestResult.PrescriptionInactive()` with no message.
- [X] T007 [P] Update `tests/PrescriptionRefill.Domain.Tests/TestSupport/Builders.cs`. Medications
  are built as `new Medication(nameAr, nameEn)` and interactions with both descriptions, each
  name unique per language.
- [X] T008 [P] Create `tests/PrescriptionRefill.IntegrationTests/Localization/AppTextCatalogTests.cs`.
  - Every `IAppText` member returns non-empty text for both `Language` values.
  - Each English text equals today's literal (copy it from `RefillEvaluator.cs`,
    `RefillRequest.cs`, `ProblemResults.cs`, `RefillRequestEndpoints.cs`, and
    `UnexpectedErrorHandler.cs` before they are edited).
  - Numbers render with Western digits in both languages.
- [X] T009 [P] Make `RefillApiFactory.CreateClient` in
  `tests/PrescriptionRefill.IntegrationTests/Infrastructure/RefillApiFactory.cs` add
  `Accept-Language: en` to every client by default, with an opt-out overload for language tests
  (spec SC-010). Note this change in its XML doc comment.
- [X] T010 [P] Update `tests/PrescriptionRefill.IntegrationTests/Infrastructure/TestData.cs` and
  `tests/PrescriptionRefill.IntegrationTests/Api/PagedListPerformanceTests.cs` to generate unique
  Arabic and English medication names and both interaction descriptions.
- [X] T011 Create `tests/PrescriptionRefill.IntegrationTests/Persistence/BilingualMigrationTests.cs`
  (quickstart V7), running on SQL Server.
  - Migrate to `InitialCreate` and insert the seeded medications and interaction plus one
    unseeded medication, then migrate to latest.
  - Assert `NameEn` holds the old names, `NameAr` holds the data-model seed map, and the unseeded
    row has `NameAr = NameEn`. Assert the descriptions likewise.
  - Assert `IX_Medications_NameAr` and `IX_Medications_NameEn` exist and are unique, and that
    `Down` restores `Name`/`Description`.
  - Assert the expected Arabic values against literal strings in the test, not against
    `SeedNames`.
- [X] T012 Update `tests/PrescriptionRefill.IntegrationTests/Persistence/SchemaTests.cs` and
  `tests/PrescriptionRefill.IntegrationTests/Persistence/SeederTests.cs` for the new columns,
  indexes, and seeded Arabic names and description.
- [X] T013 Create `tests/PrescriptionRefill.IntegrationTests/Persistence/ProjectionSqlTests.cs`
  (DG-1 condition 2, quickstart V3). It runs on SQL Server, **before any projection code**. It is
  expected not to compile until T034–T035 add `RefillRequestProfile` and `LocalizedProjections`.
  That is the test-first "red" state; it must compile and pass at T040, and must not be weakened
  to get there.
  - For each projection (list item, detail, allergies, active prescriptions, warning medication
    name, warning description) and each `Language`, call `ToQueryString()`.
  - Assert that Arabic SQL contains `NameAr`/`DescriptionAr` and not `NameEn`/`DescriptionEn`, and
    that English SQL is the reverse.
  - Execute each query and assert that no `Microsoft.EntityFrameworkCore.Query` client-evaluation
    warning is captured (`CapturingLoggerProvider`).

### Domain: codes and facts only (research R6)

- [X] T014 Change `src/PrescriptionRefill.Domain/Evaluation/RefillEvaluation.cs`:
  - `BlockingReason(string Code, DateTimeOffset? EligibleAt, InvalidPrescriptionData? InvalidData)`
    with a new enum `InvalidPrescriptionData`.
  - `Warning(string Code, Medication Medication, DrugInteraction? Interaction)`.
  - Keep the codes classes and `CanApprove` unchanged.
- [X] T015 Update `src/PrescriptionRefill.Domain/Evaluation/RefillEvaluator.cs` to emit the new
  shapes with no strings. Every condition, comparison, and the order of reasons and warnings stay
  exactly as they are. For `ALLERGY_CONFLICT` the medication is the requested one, and for
  `DRUG_INTERACTION` it is the other active medication plus the interaction (same choice as
  today).
- [X] T016 [P] Change `src/PrescriptionRefill.Domain/RefillRequests/DecisionResult.cs` to
  `InvalidNote(NoteError Error)` with a new `NoteError { Required, TooLong }`, and
  `src/PrescriptionRefill.Domain/RefillRequests/CreateRequestResult.cs` to `PrescriptionInactive()`.
  Update the producers in `src/PrescriptionRefill.Domain/RefillRequests/RefillRequest.cs`.
- [X] T017 [P] Change `src/PrescriptionRefill.Domain/Medications/Medication.cs` to
  `Medication(string nameAr, string nameEn)` with `NameAr`/`NameEn`, each validated as `Name` is
  today (trim, 1–`MaxNameLength`). Leave `IsSameAs` unchanged and remove `Name`.
- [X] T018 [P] Change `src/PrescriptionRefill.Domain/Medications/DrugInteraction.cs` to
  `DrugInteraction(Medication first, Medication second, string descriptionAr, string descriptionEn)`
  with `DescriptionAr`/`DescriptionEn`, each validated as `Description` is today. Leave the pair
  check unchanged.

### Server text catalog and request language (research R4, R5)

- [X] T019 [P] Create `src/PrescriptionRefill.Application/Localization/Language.cs`:
  `public enum Language { Arabic, English }`.
- [X] T020 Create `src/PrescriptionRefill.Application/Localization/IAppText.cs`. It needs one
  typed member per message in the contract catalog:
  - Evaluation: `TooEarly(int daysSupply)`, `NoRefillsRemaining`, `InvalidData(InvalidPrescriptionData)`,
    `AllergyConflict(string med)`, `DrugInteraction(string med, string other)`.
  - Errors: the validation title, note Required/TooLong(max), prescription inactive/missing,
    status, page, pageSize(max), search(max), not-found (request, prescription), already-decided
    title/detail, pending-exists title/detail, concurrency title/detail, approval-blocked
    title/detail, unexpected error, and framework 400/404.
- [X] T021 [P] Create `src/PrescriptionRefill.Application/Localization/EnglishText.cs`. Every text
  is copied verbatim from the current literals, with numbers formatted with
  `CultureInfo.InvariantCulture`.
- [X] T022 [P] Create `src/PrescriptionRefill.Application/Localization/ArabicText.cs` with the
  Arabic column of [contracts/http-api.md](contracts/http-api.md). Numbers use
  `CultureInfo.InvariantCulture`.
- [X] T023 Create `src/PrescriptionRefill.Application/Localization/AppText.cs`, a static
  `For(Language)` that returns singleton `EnglishText`/`ArabicText`. Register `IAppText` access in
  `src/PrescriptionRefill.Application/DependencyInjection.cs` only if a consumer needs DI;
  otherwise use `AppText.For` directly.
- [X] T024 Create `src/PrescriptionRefill.Api/Localization/RequestLanguage.cs` with
  `Language From(HttpContext)`, which maps the resolved `IRequestCultureFeature` UI culture
  (`ar`/`en`) to `Language`.
- [X] T025 In `src/PrescriptionRefill.Api/Program.cs`, configure `RequestLocalizationOptions`:
  - Supported cultures and UI cultures `ar`, `en`; default `ar`.
  - Providers: only `AcceptLanguageHeaderRequestCultureProvider`.
  - `FallBackToParentCultures` and `FallBackToParentUICultures` = true;
    `ApplyCurrentCultureToResponseHeaders` = true.
  - Call `app.UseRequestLocalization()` as the **first** middleware.
  - In `CustomizeProblemDetails`, set the framework 400/404 titles from `AppText.For(...)` by
    status (research R4).
- [X] T026 Update `src/PrescriptionRefill.Api/Errors/ProblemResults.cs` and
  `src/PrescriptionRefill.Api/Errors/UnexpectedErrorHandler.cs` to take an `IAppText` and read
  every `title`, `detail`, and validation text from it. Codes, status codes, and extension members
  stay unchanged.
- [X] T027 Update `src/PrescriptionRefill.Api/Endpoints/RefillRequestEndpoints.cs`:
  - Resolve `RequestLanguage.From(http)` once per request and pass it to every query and command.
  - Replace the inline status, page, pageSize, search, and prescriptionId texts with catalog
    calls.
  - Map `NoteError` and `PrescriptionInactive` to catalog texts.
- [X] T028 Update `src/PrescriptionRefill.Application/RefillRequests/Commands/DecisionOutcome.cs`,
  `DecisionCommand.cs`, and `CreateRefillRequest.cs` (same folder) to accept `Language` and carry
  error *kinds* (`NoteError`, inactive) instead of message strings. The transaction, the
  `rowversion` handling, and the notification order stay unchanged.

### Bilingual schema (research R7)

- [X] T029 [P] Update
  `src/PrescriptionRefill.Infrastructure/Persistence/Configurations/MedicationConfiguration.cs`:
  `NameAr` and `NameEn` are required nvarchar(100), with unique indexes `IX_Medications_NameAr` and
  `IX_Medications_NameEn`.
- [X] T030 [P] Update
  `src/PrescriptionRefill.Infrastructure/Persistence/Configurations/DrugInteractionConfiguration.cs`:
  `DescriptionAr` and `DescriptionEn` are required nvarchar(300).
- [X] T031 Create a shared seed map, `src/PrescriptionRefill.Infrastructure/Seeding/SeedNames.cs`,
  with the 11 `NameEn → NameAr` pairs and both interaction descriptions from
  [data-model.md](data-model.md). Update
  `src/PrescriptionRefill.Infrastructure/Seeding/DemoDataSeeder.cs` to construct medications and
  the interaction from it. The seeded patient names, dosages, and notes stay unchanged.
  `SeedNames` is used **only** by the seeder and tests of the seeder. The migration (T032) and the
  migration test (T011) never reference it.
- [X] T032 Generate the migration with
  `dotnet ef migrations add BilingualReferenceData --project src/PrescriptionRefill.Infrastructure --startup-project src/PrescriptionRefill.Api`.
  Then hand-edit
  `src/PrescriptionRefill.Infrastructure/Persistence/Migrations/<timestamp>_BilingualReferenceData.cs`
  to follow data-model steps 1–5:
  - Use `RenameColumn` (never drop and add).
  - Add the Arabic columns as nullable. Run `UPDATE` statements with the Arabic names and
    description written as literal values in the migration (do **not** reference `SeedNames`).
    Then copy the English value into any row that is still null.
  - Alter both columns to NOT NULL, then create the indexes.
  - Write a full `Down`.
  - Commit the snapshot.
- [X] T033 Run T011 and T012 on SQL Server and make them pass.

### Per-language projections (research R2, R3, R8; DG-1 condition 2)

- [X] T034 Create `src/PrescriptionRefill.Application/Mapping/RefillRequestProfile.cs`. It is a
  `Profile` whose constructor takes `Language`, with `MapFrom` lambdas that use **only** that
  language's member (`m => m.NameAr` or `m => m.NameEn`, and the same for descriptions). It maps:
  - `RefillRequest → RefillRequestSummaryDto` (evaluation flags ignored);
  - `RefillRequest → RefillRequestDetailDto` (without `ActivePrescriptions` and `Evaluation`);
  - `Patient → PatientDto` (allergies as localized names ordered by the localized name);
  - `Prescription → ActivePrescriptionDto`;
  - `Medication → LocalizedNameDto(Id, Name)` and `DrugInteraction → LocalizedTextDto(Id, Description)`.
  
  Put the two internal lookup records in
  `src/PrescriptionRefill.Application/RefillRequests/Dtos/RefillRequestDtos.cs`. Leave the public
  DTO shapes unchanged (spec FR-021).
- [X] T035 Create `src/PrescriptionRefill.Application/Mapping/LocalizedProjections.cs`, a singleton
  holding one `MapperConfiguration` per `Language`. It reads the license key from
  `IConfiguration["AutoMapper:LicenseKey"]`, calls `AssertConfigurationIsValid()` for both at
  construction, and exposes `IConfigurationProvider For(Language)`. Register it in
  `src/PrescriptionRefill.Application/DependencyInjection.cs`.
- [X] T036 Update `src/PrescriptionRefill.Application/RefillRequests/Dtos/DtoMapper.cs`. It keeps
  only evaluation composition: `ToDto(RefillEvaluation, IAppText, names, descriptions, daysSupply)`
  builds each `BlockingReasonDto`/`WarningDto` message from the catalog and looks up names and
  descriptions by id from the localized lookups. Remove `ToDetailDto`, which the T037 projection
  replaces.
- [X] T037 Update `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequestDetail.cs`
  to take `Language`.
  - The rule read through `RefillDecisionContextLoader` and the evaluation stay unchanged.
  - Display read: `ProjectTo<RefillRequestDetailDto>(projections.For(language))`.
  - Active prescriptions: project the ids that `ActiveMedications.For` selects, then restore the
    Domain's order.
  - Warning names and descriptions: one lookup projection over the referenced ids.
  - **Single source for safety facts**: `remainingRefills`, `lastFilledAt`, `daysSupply`,
    `isActive`, the request `status`, and every evaluation field (including `eligibleAt`) come
    from the rule read
    (`RefillDecisionContext`), not from the display projection. The projection supplies only
    localized names, allergies, and descriptions, plus fields that never change after creation
    (ids, `fullName`, `dateOfBirth`, `dosage`, `requestedAt`). Merge the two by id.
  - Tag the rule-read query in
    `src/PrescriptionRefill.Application/RefillRequests/RefillDecisionContextLoader.cs` with
    `TagWith("refill:rule-read")`. This only adds an SQL comment; the query is otherwise
    unchanged.
  - Create `tests/PrescriptionRefill.IntegrationTests/Infrastructure/BetweenReadsInterceptor.cs`.
    It is a test-only `DbCommandInterceptor` that fires once, in `DataReaderClosingAsync`, on the
    first command whose text contains `refill:rule-read`. It then runs a caller-supplied SQL
    statement on a **separate** `SqlConnection` to the same test database. Running it when the
    reader closes, rather than when it is opened, avoids blocking on the rule read's shared
    locks.
  - Register the interceptor for a single test through a `RefillApiFactory` option. In
    `tests/PrescriptionRefill.IntegrationTests/Api/QueueAndDetailTests.cs`, add a test with a
    Pending request whose prescription has `RemainingRefills = 2`:
    - The interceptor sets `RemainingRefills = 0` for that prescription.
    - `GET /api/refill-requests/{id}` must return `remainingRefills = 2` with no
      `NO_REFILLS_REMAINING` reason, which matches the rule read.
    - A second `GET` must return `0` with `NO_REFILLS_REMAINING`, which proves the update
      happened between the two reads.
    - Run it with `Accept-Language: ar` and `en`.
- [X] T038 Update `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs`
  to take `Language`.
  - Run the item projection with `ProjectTo<RefillRequestSummaryDto>(projections.For(language))`
    over the page's ids (paged Pending) or as the ordered list (unpaged and decided).
  - Merge the evaluation flags by id. Status and every value that feeds a flag also come from
    the rule read for Pending items (the T037 rule).
  - Paging, ordering, totals, and `EvaluatePendingAsync` stay unchanged.
- [X] T039 Update `src/PrescriptionRefill.Application/RefillRequests/Commands/DecisionCommand.cs` so
  the success response re-reads the detail through the T037 display path **after** commit and
  notification. The transaction contents and the notification order stay unchanged (research R3).
  Approval still re-evaluates every rule against current persisted data inside the decision's
  unit of work, exactly as today (constitution Principle III); the re-read is presentation only.
  Document in the method's XML comment that the success response shows the persisted state as
  read after commit. If another change to the same prescription commits between this commit and
  the re-read, the response reflects that later state.
- [X] T040 Run T013 (ProjectionSqlTests) and the full `dotnet test` with English pinned. Every
  pre-existing integration test passes with unchanged assertions. If T013 cannot pass, stop and
  report the alternatives (spec DG-1), then follow "If the DG-1 gate stays open" in
  Dependencies for what may still proceed.

### Frontend i18n and preferences core (research R9–R11)

- [X] T041 [P] Create `frontend/src/app/core/i18n/language.ts` (`type Language = 'ar' | 'en'`,
  `DEFAULT_LANGUAGE = 'ar'`, `isLanguage()`) and `frontend/src/app/core/i18n/messages.ts`
  (`interface Messages`, a nested typed tree of every UI string, with parameterized entries as
  functions).
- [X] T042 Create `frontend/src/app/core/i18n/en.ts` (`const en: Messages`). Copy every string
  verbatim from the current templates and components: `app.component.html`, everything under
  `features/refill-requests/**` and `shared/ui/**`, `shared/http/problem-details.ts`, and the
  live-region texts.
- [X] T042a Create `frontend/src/app/core/i18n/ar.ts` (`const ar: Messages`) with a first-pass
  Arabic value for every key, using the glossary in
  [contracts/ui-contract.md](contracts/ui-contract.md). Every key in `Messages` must have a value,
  or the build fails. Finishing the wording and adding the notices is T054.
- [X] T043 [P] Create `frontend/src/app/core/preferences/preferences.store.ts` and
  `preferences.store.spec.ts`. Keys are `prx.language` and `prx.theme`, every access is in
  `try/catch`, and invalid values or storage errors return the default without rewriting. Writes
  are independent per key (spec FR-009, FR-027). The spec covers defaults, invalid values, a
  throwing storage, and independence.
- [X] T044 Create `frontend/src/app/core/i18n/i18n.service.ts` and `i18n.service.spec.ts`:
  - `language` signal, `t` computed dictionary, `setLanguage()` (writes the store; sets `html[lang]`,
    `html[dir]` and `document.title`), and `languageChanged$`, which emits on change only.
  - The spec covers the default `ar`, the restored value, attributes, and no emission at startup.
- [X] T045 [P] Create `frontend/src/app/core/i18n/accept-language.interceptor.ts` and its spec. It
  is an `HttpInterceptorFn` that sets `Accept-Language` to the active language on `/api/` requests
  only. Register it in `frontend/src/app/app.config.ts` with
  `provideHttpClient(withInterceptors([acceptLanguageInterceptor]))`.
- [X] T046 [P] Create `frontend/src/app/core/i18n/testing.ts` with
  `provideI18nTesting(language = 'en')`, and add it to every existing `*.spec.ts` under
  `frontend/src/app/` so the existing English assertions are unchanged (spec SC-010).
- [X] T047 Replace every hard-coded UI string with `i18n.t()` reads, keeping the markup, test ids,
  roles, and live regions identical. Files: `frontend/src/app/app.component.{ts,html}`,
  `features/refill-requests/queue/*`, `features/refill-requests/review/*`, `shared/ui/*.ts`, and
  `shared/http/problem-details.ts`. Run `npm run test:ci`; every existing spec must pass
  unchanged in English.

**Checkpoint**: With English selected, the API output and the UI are byte-identical to the
baseline. Arabic text exists on both server and client (first pass), and the app builds and runs
with the Arabic default. If the DG-1 gate is still open, this checkpoint is only partly met (see
"If the DG-1 gate stays open" in Dependencies).

---

## Phase 3: User Story 1 - Work in Arabic by default (Priority: P1) 🎯 MVP

**Goal**: The first visit opens in Arabic, right-to-left, with Arabic interface text, Arabic
medication names, and Arabic server messages. Recorded text is untouched, and decisions behave as
in 001.

**Independent Test**: In a fresh profile, open the queue and a review of each seeded scenario. All
text is Arabic, the layout is RTL, names are the Arabic seed names, and patient-entered text is
unchanged. Approve and deny behave as in 001 (quickstart V2, V4, V5, V8).

### Tests for User Story 1

- [X] T048 [P] [US1] Create `tests/PrescriptionRefill.IntegrationTests/Api/LocalizationTests.cs`
  (quickstart V1).
  - For no header, `ar`, `ar-SA`, `en`, `en-GB`, `fr`, `x;;`, and `fr, en;q=0.8`, assert
    `Content-Language` is `ar, ar, ar, en, en, ar, ar, en`.
  - Assert identical ids, codes, order, and flags across all of them.
- [X] T049 [P] [US1] In the same file, add a parity test (spec SC-003, quickstart V2). For seed
  scenarios 1–6, compare the detail JSON for `ar` and `en` after removing `message`, `title`,
  `detail`, `errors`, `medicationName`, `allergies`, and `description`. Assert zero differences,
  and that `fullName`, `dosage`, and `decisionNote` are equal.
- [X] T050 [P] [US1] In the same file, add Arabic error texts (quickstart V4, V5). Cover the
  blocked approval of scenario 6, matching the contract example; an invalid note (Required,
  TooLong); an inactive prescription; a bad page and page size; an unknown id; an already-decided
  request; a pending request that exists; a forced concurrency conflict; and malformed JSON. In
  each, the `code` and status are equal to English, the texts equal the catalog, and the database
  is unchanged after a 422.
- [X] T051 [P] [US1] Extend `frontend/src/app/shared/time/format-time.spec.ts` with Arabic cases.
  Expect `20 أكتوبر 2026، 14:05 (UTC+03:00)` with Western digits and the time and offset in an LTR
  isolate. The English cases stay unchanged.
- [X] T052 [P] [US1] Create `frontend/src/app/features/refill-requests/review/refill-review.i18n.spec.ts`.
  With `provideI18nTesting('ar')`, render the blocked, warning, no-issues, and decided states and
  assert:
  - Arabic headings and labels from the glossary;
  - "لم يُعثر على مشكلات", never "آمن";
  - the non-blocking warning label;
  - recorded text with `dir="auto"` or `<bdi>`, and the patient name and note untranslated.
- [X] T053 [P] [US1] Create `frontend/src/app/core/i18n/dictionary-parity.spec.ts`. Walk `en` and
  `ar` recursively. Assert the same keys, no empty `ar` values, and no `ar` value equal to `en`
  except an explicit allow-list (`UTC`, `English`).

### Implementation for User Story 1

- [X] T054 [US1] Complete `frontend/src/app/core/i18n/ar.ts`: finish the Arabic wording of every
  entry against the glossary, including the fictional-data and no-sign-in notices, the
  live-region texts, the page titles, and the status labels (values stay English). T053
  (dictionary parity) must pass.
- [X] T055 [US1] Update `frontend/src/index.html`:
  - Set `<html lang="ar" dir="rtl" data-theme="light">` and
    `<meta name="color-scheme" content="light">`.
  - Add an inline boot script before `<app-root>` that reads `prx.language` (and `prx.theme`,
    finished in T074) with the same validation and `try/catch`, then sets `lang`/`dir` before the
    first paint (spec FR-028).
- [X] T056 [US1] Add a language argument to `frontend/src/app/shared/time/format-time.ts` and
  `frontend/src/app/shared/time/instant.component.ts`. Use Arabic month names, the Arabic comma,
  Western digits, and an LTR isolate around the time and offset. `formatDateOnly` follows the
  same pattern (research R14).
- [X] T057 [P] [US1] Convert the physical-direction declarations to logical properties
  (`margin-inline-start`, `padding-inline-*`, `inset-inline-*`, `border-inline-*`,
  `text-align: start|end`) in `frontend/src/styles.scss`, `frontend/src/app/app.component.scss`,
  `features/refill-requests/queue/refill-queue.page.scss`,
  `features/refill-requests/review/{refill-review.page,review-panels,decision-form.component}.scss`,
  and `shared/ui/alert.component.scss`. Find them with
  `grep -rnE "(margin|padding|border)-(left|right)|\b(left|right):|text-align: (left|right)" frontend/src`.
  Keep only the ambient light's pointer coordinates physical (research R13).
- [X] T058 [P] [US1] Add a `directional` input to `frontend/src/app/shared/ui/icon.component.ts` that
  mirrors with `scaleX(-1)` under `[dir=rtl]`. Set it on the back arrow, chevrons, and
  `shared/ui/pagination.component.ts` Previous/Next. Extend `icon.component.spec.ts` to cover a
  mirrored and a non-mirrored icon.
- [X] T059 [US1] Isolate recorded and mixed text with `dir="auto"` or `<bdi>`: the patient name,
  dosage, decision note (input and read-only), medication names, and allergy items. Files:
  `review/patient-panel.component.html`, `review/evaluation-panel.component.html`,
  `review/decision-form.component.html`, `review/decided-view.component.ts`, and
  `queue/refill-queue.page.html` (spec FR-015).
- [X] T060 [P] [US1] Add Arabic typography to `frontend/src/styles.scss` and
  `frontend/src/styles/_variables.scss`: `:lang(ar)` sets `letter-spacing: 0` (overriding the 9
  current uses in `styles.scss`, `_variables.scss`, `app.component.scss`, and `review-panels.scss`),
  uses an Arabic-capable system font stack first, and keeps line-height ≥ 1.5. No web fonts.
- [X] T061 [US1] Run T048–T053, `dotnet test`, and `npm run test:ci`. Then run quickstart V8
  (fresh profile with the browser in English: `ar`/`rtl`/Light with no first-paint flash) and
  save screenshots to `specs/005-bilingual-localization-themes/screenshots/`.

**Checkpoint**: The MVP works. Arabic is the default end to end, and English is still
byte-identical when selected through the API.

---

## Phase 4: User Story 2 - Switch to English and keep it (Priority: P1)

**Goal**: A header language control switches between العربية and English without a reload. It
keeps the user's place and typed note, reloads the server data in the new language, and the
choice persists.

**Independent Test**: On queue page 2 with a status and search, and on a review with a typed note,
switch language. The layout flips, place and note are kept, data reloads in English, and a reload
keeps English (quickstart V10, V14).

### Tests for User Story 2

- [X] T062 [P] [US2] Create `frontend/src/app/shared/ui/preference-switch.component.spec.ts`.
  - It is a two-option radio group with one tab stop, and the arrow keys follow the visual
    direction in both `ltr` and `rtl`.
  - It has an accessible name and `aria-checked` on the active option.
  - Focus stays on the selected option, and the change is announced through a polite live region
    (spec FR-035).
- [X] T063 [P] [US2] Extend `frontend/src/app/features/refill-requests/queue/refill-queue.page.spec.ts`.
  A language change triggers exactly one reload that keeps the status, search, page, and page
  size, and only the latest response is rendered when a reload is in flight (003 FR-016).
- [X] T064 [P] [US2] Extend `frontend/src/app/features/refill-requests/review/refill-review.page.spec.ts`.
  - A language change reloads the detail and evaluation and keeps the typed note.
  - A decision in flight completes once, without resubmission or cancellation, and the page
    reloads afterwards in the new language.
  - With the language set to `ar`, then switched to `en`, a `RequestUpdated` event from
    `core/realtime/testing/fake-refill-events.ts` triggers a detail reload whose request
    (`HttpTestingController`) carries `Accept-Language: en`, matching the active language at the
    time of the event (spec edge case "A live update arrives while in either language").
- [X] T065 [P] [US2] Extend `frontend/src/app/app.component.spec.ts`. The header shows the language
  control with `data-testid="language-switch"`. Its options have `data-value="ar|en"`, are labeled
  "العربية" and "English" in their own `lang`, and appear on every route.

### Implementation for User Story 2

- [X] T066 [US2] Create `frontend/src/app/shared/ui/preference-switch.component.ts`, a reusable
  OnPush two-option radio group following the 002 FR-014a pattern. Its inputs are the options,
  value, accessible name, and test id, and it emits a change event. Each option is at least
  44 × 44 px.
- [X] T067 [US2] Add the language control to `frontend/src/app/app.component.{ts,html,scss}` next
  to the connection indicator, wrapping to a second row below 768 px. Wire it to
  `I18nService.setLanguage()` and the polite announcement (`تم التبديل إلى العربية` /
  `Switched to English`).
- [X] T068 [US2] Add `I18nService.languageChanged$` as a fourth trigger in
  `frontend/src/app/core/realtime/reload-triggers.ts`. Confirm that
  `queue/refill-queue.page.ts` and `review/refill-review.page.ts` route it through their existing
  `switchMap` reload pipelines, and that decision submissions stay outside the pipeline
  (research R10).
- [ ] T069 [US2] Run T062–T065 and `npm run test:ci`. Then run quickstart V10, V14, and V17 (language half) in the
  browser, including Narrator spot checks, and record the results.

**Checkpoint**: Both languages are fully usable, switchable at runtime, and persisted.

---

## Phase 5: User Story 3 - Choose Light or Dark theme independently (Priority: P2)

**Goal**: A header theme control switches Light and Dark at runtime. Every surface, including
clinical content, uses contrast-verified tokens. The theme persists independently of the
language, and printing stays legible.

**Independent Test**: In all four combinations, each survives a reload and changing one
preference leaves the other alone. Contrast and non-color checks pass in both themes
(quickstart V11, V12, V15).

### Tests for User Story 3

- [X] T070 [P] [US3] Create `frontend/src/app/core/theme/theme.service.spec.ts`. Cover the default
  `light`, the restored value, invalid and throwing storage, that `html[data-theme]` and
  `meta[name=color-scheme]` are set, and that setting the theme never touches `prx.language`,
  `lang`, or `dir` (and the reverse through `I18nService`).
- [X] T071 [P] [US3] Create `specs/005-bilingual-localization-themes/scripts/contrast-check.mjs`, a
  plain Node script with no package. It reads `frontend/src/styles/_themes.scss` in this **token
  format**, and fails with a clear message if the file doesn't match it:
  - Each theme block is delimited by the marker comments `/* tokens:light:start */` …
    `/* tokens:light:end */` and `/* tokens:dark:start */` … `/* tokens:dark:end */`.
  - Inside a block, each color token is on its own line as `--token-name: #rrggbb;`: a 6-digit
    hex only, with no `var()`, color functions, or alpha. Other declarations, such as
    `--glow-opacity: 0.4;`, are allowed but are not used in pairs.
  - The pairs to check are a constant array in the script, each with `fg`, `bg`, and a minimum
    (`4.5` for text, `3` for non-text: icons, input borders, focus). These are the R12 pairs
    plus the matching Light pairs.
  - A pair whose token is missing from either block is a failure, not a skip.
  
  The script computes the WCAG 2.1 ratio per pair per theme, prints a table, and exits non-zero
  on any failure (spec SC-006).

### Implementation for User Story 3

- [X] T072 [US3] Create `frontend/src/styles/_themes.scss`, writing both color sets in the T071
  token format (marker comments, one `--token: #rrggbb;` per line):
  - The Light set (today's 004 values, unchanged) under `:root, :root[data-theme='light']`.
  - The Dark set under `:root[data-theme='dark']`, using the research R12 table.
  - Explicit tokens per theme for `--focus-ring`, `--shadow-color`, `--canvas-border`,
    `--glow-opacity`, and the sheet border.
  - `@media print` forcing the Light set.
  
  Point the color variables in `frontend/src/styles/_variables.scss` at the custom properties
  (`$color-text: var(--color-text)`, and so on), keeping their names.
- [X] T073 [US3] Replace the 12 SCSS color-function uses (`rgba($…)`, `mix(…)`, `lighten`/`darken`)
  with the named tokens from T072. Find them with
  `grep -rnE "rgba\(\\$|mix\(|lighten\(|darken\(" frontend/src`. Keep clinical content on opaque
  `--color-surface` in Dark, and keep the ambient layer behind opaque surfaces (spec FR-030). Check
  `frontend/src/app/shared/ambient/ambient-light.directive.ts` for inline colors.
- [X] T074 [US3] Create `frontend/src/app/core/theme/theme.service.ts` (a `theme` signal and
  `setTheme()`, which writes only `prx.theme` and sets `data-theme` and `color-scheme`). Finish the
  theme half of the boot script in `frontend/src/index.html`.
- [X] T075 [US3] Add the theme control (`preference-switch`, `data-testid="theme-switch"`,
  `data-value="light|dark"`, decorative sun and moon icons with the text label visible) to
  `frontend/src/app/app.component.{ts,html}`, with the announcement (`تم تفعيل المظهر الداكن` /
  `Dark theme on`, and the Light equivalents). Add the `sun` and `moon` icons to
  `frontend/src/app/shared/ui/icon.component.ts` as non-directional.
- [X] T076 [US3] Verify the forced-colors and reduced-motion rules from 004 in Dark, in
  `frontend/src/styles.scss` and `shared/ambient/ambient-light.directive.ts`. Safety sections,
  badges, and focus must stay distinguishable, and the static composition must use dark tokens
  (spec FR-036).
- [ ] T077 [US3] Run T070, T071, and `npm run test:ci`. Then run quickstart V11, V12 (axe or an
  equivalent audit of the queue and review in all four combinations), and V15 (forced colors,
  reduced motion, and print preview in Dark), and V17 (theme half). Record the results and
  screenshots.

**Checkpoint**: Four independent combinations work, and AA passes in both themes.

---

## Phase 6: User Story 4 - Accurate, readable bilingual clinical data (Priority: P2)

**Goal**: Every medication name and interaction description comes from the stored value for the
active language. Search matches the active language only, and no response mixes languages.

**Independent Test**: For every seeded medication, both interfaces show the stored name wherever
a name appears. The API returns one name per medication in the requested language, and a search
by the other language's name finds nothing (quickstart V6, SC-005).

### Tests for User Story 4

- [X] T078 [P] [US4] Create `tests/PrescriptionRefill.IntegrationTests/Api/LocalizedSearchTests.cs`
  (quickstart V6, spec FR-025):
  - An Arabic name with `ar` matches.
  - The same Arabic name with `en` finds nothing for that medication, and an English name with
    `ar` finds nothing.
  - Patient-name matching is unchanged in both languages.
  - Paging totals are consistent with the matches.
  - No search text is logged (`CapturingLoggerProvider`).
- [X] T079 [P] [US4] Add to `tests/PrescriptionRefill.IntegrationTests/Api/LocalizationTests.cs`
  (spec SC-005, FR-021, FR-023, FR-024):
  - Each list item and detail in each language contains exactly the stored name for that language
    and never the other language's name; the raw JSON has no other-language name or description.
  - Allergies are ordered by the localized name (ordinal).
  - The `DRUG_INTERACTION` warning `description` and `medicationName` are in the requested
    language, and `description` stays `null` for `ALLERGY_CONFLICT`.

### Implementation for User Story 4

- [X] T080 [US4] Change `Matching` in
  `src/PrescriptionRefill.Application/RefillRequests/Queries/GetRefillRequests.cs` to take
  `Language`. Select the member expression **outside** the query (`r.Prescription.Medication.NameAr`
  or `...NameEn`), keeping the lower-casing, trim rules, and patient-name branch. Add a
  `ProjectionSqlTests` case asserting that the search SQL contains only the active language's
  column (research R8).
- [X] T081 [US4] Make every medication name in the UI read from the API's localized field. There
  must be no client-side name mapping or fallback in
  `frontend/src/app/features/refill-requests/data/models.ts` or `testing/fixtures.ts`. Add Arabic
  fixture names to `testing/fixtures.ts` for the T052 spec.
- [ ] T082 [US4] Run T078 and T079 and the full `dotnet test` on SQL Server, then quickstart V6.
  Check long Arabic and English names at 360 px in both directions for wrapping without overlap.

**Checkpoint**: All four stories pass independently.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T083 [P] Run quickstart V9 in Edge with a DevTools-protocol script kept outside the repo
  (as in 002/004). Cover four combinations × queue, each review state, and a decided request ×
  360 px, 1280 px, and 200% zoom. Save the screenshots to
  `specs/005-bilingual-localization-themes/screenshots/`.
- [X] T084 Run quickstart V16 (the "after" timing, each language, against the T002 baseline) and
  `PagedListPerformanceTests`. If the time is more than 10% slower, apply research R16 (merge the
  page projection into the page-id query) and re-measure.
- [ ] T085 Run quickstart V13. An Arabic speaker reviews `frontend/src/app/core/i18n/ar.ts`,
  `src/PrescriptionRefill.Application/Localization/ArabicText.cs`, and the seed names against the
  glossary. Apply the fixes to the dictionary, the catalog, and
  [contracts/http-api.md](contracts/http-api.md) together. If a seed name or the seed description
  changes, update `SeedNames.cs` and add a new data migration (`ReviseArabicSeedNames`) for
  databases that are already migrated. Never edit `BilingualReferenceData`. Record the reviewer's
  notes. If no
  reviewer is available, record V13 as **not run**.
- [X] T086 [P] Run a final text audit (spec SC-004). Grep the frontend templates for remaining
  literal user-facing strings outside `en.ts`/`ar.ts`, and confirm that the seeded patient names,
  dosages, and notes are unchanged against the baseline seed.
- [X] T087 [P] Confirm the AutoMapper key is not in git with
  `git grep -n "LicenseKey" -- ':!*.md'`. The only hit should be the configuration read in
  `LocalizedProjections.cs`. Confirm no key appears in `frontend/`.
- [X] T088 Fill in the `## Results` section of
  `specs/005-bilingual-localization-themes/quickstart.md` for V1–V17. Record each as passed,
  failed, or not run, with evidence (test names and screenshot paths).
- [X] T089 [P] Update `SPEC.md` with feature 005: the Arabic default, the bilingual reference
  data, the themes, and the amended 001 FR-023 and FR-024, 003 FR-007, and 004 FR-007 (spec
  conflicts C5, C8–C10).
- [X] T090 [P] Update `PROMPTS.md` with the prompts used for feature 005.
- [X] T091 Update `README.md`:
  - The `Accept-Language` behavior; clients without a header now get Arabic (spec C7).
  - AutoMapper Community License key setup with user secrets, and the behavior with no key.
  - A re-seed note for authored Arabic names.
  - The language and theme controls.
  
  Re-verify the README steps from a clean clone.
- [X] T092 Run the final full suite (`dotnet test`, `npm run test:ci`, `npm run build`) and compare
  the counts with the T002 baseline. No test may be removed or weakened, and every changed
  assertion must be listed in the `quickstart.md` Results section (spec SC-010).

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: T001, then T002, then T003, then T004. T003 is allowed only after T001
  records DG-1 condition 3 as verified and met. If it stays open, follow "If the DG-1 gate stays
  open" below.
- **Foundational (Phase 2)**: Depends on Setup and blocks every story. The order inside it:
  - Tests T005–T013 come first; they may run in parallel where marked.
  - Domain: T014, then T015. T016–T018 can run in parallel with them.
  - Catalog: T019, then T020, then T021 and T022 in parallel, then T023.
  - API wiring: T024, then T025, T026, T027, T028.
  - Schema: T029 and T030, then T031, then T032, then T033.
  - Projections: T034, then T035, then T036, then T037, T038, T039, then T040. They depend on
    T013, the Domain tasks, the catalog, and the schema.
  - Frontend core: T041, T042, T042a, then T043–T047. It is independent of the backend tasks and can run in parallel
    with T014–T040.
- **US1 (Phase 3)**: Depends on Phase 2. This is the MVP.
- **US2 (Phase 4)**: Depends on Phase 2. It can run in parallel with US1, but T067 edits the same
  header files as US1's T055 and US3's T075, so finish US1 first if one person does both.
- **US3 (Phase 5)**: Depends on Phase 2 and on T066 (the `preference-switch` component from US2).
  T074 finishes the boot script started in T055.
- **US4 (Phase 6)**: Depends on Phase 2 (projections). It is backend-only except T081, and can run
  in parallel with US2 and US3.
- **Polish (Phase 7)**: After every story wanted for delivery.

### If the DG-1 gate stays open

This applies when T001 leaves condition 3 open, or when T013/T040 cannot prove condition 2. It
lasts until the gate is met, or until the author decides on an alternative (spec DG-1).

| Status | Tasks | Why |
|--------|-------|-----|
| **Held** | T003, T013, T034–T040 | They need the AutoMapper package or its types. |
| **Held** | T078–T080, T082 (US4 backend) | Localized search and the one-name assertions are built on the projections. |
| **Held** | T083–T086, T088–T092 (Polish) | They verify or document the finished feature. |
| **May proceed** | T002, T004 | Baseline, and key or log hygiene. Neither adds the package. |
| **May proceed** | T005–T012, T014–T033, T041–T047, T042a | Codes-only Domain, catalog, culture resolution, schema and migration, and the frontend core. |
| **May proceed** | T048, T050–T060 (US1) | Language resolution, Arabic error texts, and all of the Arabic UI. T049 (parity) may run, but it doesn't count as US1 evidence while names are unlocalized. |
| **May proceed** | T062–T069 (US2), T070–T077 (US3), T081, T087 | Frontend only. |

While the gate is open:
- The **Phase 2 checkpoint is only partly met**: API medication names and warning descriptions
  are still English in both languages.
- **US1 cannot be signed off** (T061), because its acceptance scenarios 1–3 need Arabic medication
  names.
- Record what is held in the `quickstart.md` Results section as **not run (DG-1 open)**.

Without the projection tasks, `Medication.Name` and `DrugInteraction.Description` no longer exist
after T017–T018. So the existing read paths (`DtoMapper.ToDetailDto`, the list mapping, and
evaluation message composition) switch to the entities' `NameEn` and `DescriptionEn`, which hold
today's values. API output in English then stays byte-identical. The queries accept the
`Language` parameter added by T027–T028 and use it for catalog text only, and the build and all
existing tests must pass. This is a temporary state, not a fallback design, and it does not meet
FR-022. Choosing a fallback
(Mapperly or hand-written `Select`) remains the author's decision.

### User story dependencies

| Story | Depends on | Shares files with |
|-------|------------|-------------------|
| US1 (P1) | Phase 2 | US2 and US3 (`app.component.*`, `index.html`) |
| US2 (P1) | Phase 2 | US3 (`preference-switch` component) |
| US3 (P2) | Phase 2, T066 | US1 (boot script) |
| US4 (P2) | Phase 2 | US1 (`LocalizationTests.cs`) |

---

## Parallel Examples

### Phase 2

```text
Backend tests together:  T005, T006, T007, T008, T009, T010
Domain together:         T016, T017, T018 (after T014 starts)
Catalog texts together:  T021 (EnglishText), T022 (ArabicText)
Schema config together:  T029, T030
Frontend core alongside the backend: T041, T043, T045, T046
```

### User Story 1

```text
Tests:   T048, T049, T050, T051, T052, T053
Styles:  T057 (logical properties), T058 (directional icons), T060 (Arabic typography)
```

### User Story 2

```text
Tests: T062, T063, T064, T065
```

### User Story 3

```text
Tests: T070 (ThemeService spec), T071 (contrast script)
```

### User Story 4

```text
Tests: T078 (LocalizedSearchTests), T079 (one-name-per-language assertions)
```

---

## Implementation Strategy

### MVP first (User Story 1)

1. Phase 1, then Phase 2. Stop at the T040 checkpoint if the projection SQL test fails, and
   report the alternatives.
2. Phase 3 (US1). **Stop and validate**: quickstart V1, V2, V4, V5, and V8. The application now
   opens in Arabic, and English is still reachable through the API.

### Incremental delivery

1. US1: Arabic default (MVP).
2. US2: a runtime language switch with persistence. Together with US1, this meets the bilingual
   requirement.
3. US4: localized search and mixed-language guarantees. It is mostly backend and can run
   alongside US2.
4. US3: Dark theme and the theme control.
5. Polish: the full quickstart, the Arabic review, performance, and docs.

### Gate reminder

DG-1 conditions 1, 3, and 4 are handled in Phase 1. Condition 2 is proven by T013 in Phase 2
before any projection code is written. If it fails, user-story backend work stops and the
alternatives (Mapperly, hand-written `Select`) are reported to the author. The requirement does
not change without the author's decision.

## Implementation notes

- **T034–T039, as built**: see research R3 "As implemented". The profile maps
  `Medication → LocalizedNameDto` and `DrugInteraction → LocalizedTextDto` only. `LocalizedReferenceData`
  runs those lookups and `LocalizedDtoBuilder` merges them into the unchanged DTO shapes. The
  decision success response is built from the committed entities, with no re-read after commit.
- **License key wiring (T035)**: Application has no configuration package, so the API passes
  `builder.Configuration["AutoMapper:LicenseKey"]` to `AddApplication(...)`.
  `LocalizedProjections` sets `cfg.LicenseKey` only when a key is present.
- **Search (T080)** was implemented with T038, because removing `Medication.Name` required it.
  The SQL assertion is in `ProjectionSqlTests`.
- **Test infrastructure (T040)**: with the new test classes, running every class at once on a
  20-thread machine timed out `CREATE DATABASE` and the 1 s paging budget on the local SQL Server.
  The baseline's first run already showed 9 such failures. `xunit.runner.json` now runs 4
  classes at a time, and fixtures allow 180 s per command. Three consecutive full runs passed
  187/187.

## Notes

- [P] means different files with no dependency on an incomplete task.
- Commit after each task or logical group. Never commit secrets.
- Domain test changes replace message-string assertions with code, kind, or entity assertions.
  Never weaken an outcome assertion.
