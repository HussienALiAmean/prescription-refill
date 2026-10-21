# Implementation Plan: Bilingual Localization and Theme Preferences

**Branch**: `005-bilingual-localization-themes` (planned name; work currently on `master`) | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-bilingual-localization-themes/spec.md`

## Summary

Add Arabic (default, RTL) and English (LTR) to the whole application, and independent Light and
Dark themes, without changing any rule, decision, transaction, concurrency, or live-update
behavior.

**Backend**:
- Each request's language is resolved by ASP.NET Core request localization from
  `Accept-Language`. Only `ar` and `en` are supported, and anything else resolves to `ar` (R4).
- The resolved `Language` is passed explicitly into Application.
- Human-readable text moves out of Domain, which now returns codes and facts only (R6). Text comes
  from a typed `IAppText` catalog with English (today's text, verbatim) and Arabic
  implementations (R5).
- Medications gain `NameAr`/`NameEn` and interactions gain `DescriptionAr`/`DescriptionEn`
  through a data-preserving migration (R7).
- Every name or description in a response is selected in SQL by AutoMapper `ProjectTo`, using one
  mapper configuration per language, so only that language's column appears in the query (R2,
  R3).
- Rule evaluation keeps loading entities as today. Search matches the active language's name
  only (R8).

**Frontend**:
- A typed in-house dictionary and signal-based `I18nService` switch language without a reload
  (R9).
- An interceptor sends `Accept-Language`, and a language change reloads the current data while
  keeping place and the typed note (R10).
- Preferences live in `localStorage` under two independent keys and are applied before first
  paint by an inline boot script (R11).
- Theme tokens become CSS custom properties, with a contrast-verified Dark set (R12).
- Layout moves to logical properties, with bidi isolation for recorded text (R13) and localized,
  Western-digit dates (R14).

**Gate**: AutoMapper (16.2.0 candidate) is added only after DG-1 condition 3 is met. The author's
decision (Community License, eligibility confirmed) is recorded in spec Clarifications
2026-10-21. The 16.2.0 license metadata, the current Community License terms, and the key
configuration were verified from the sources by task T001 on 2026-10-21 (research R1), so
condition 3 is met. Other work is independent of it
(see Implementation Order).

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), TypeScript 5.6 on Angular 19, SCSS

**Primary Dependencies**:
- Existing: ASP.NET Core 10 (request localization is in the shared framework), EF Core 10.0.13 with
  the SQL Server provider, `@angular/*` 19, `@microsoft/signalr` 10.
- New: **AutoMapper 16.2.0, gated by DG-1** (research R1).
- No new frontend package.

**Storage**: Microsoft SQL Server. One new migration, `BilingualReferenceData`. Browser
`localStorage` holds two preference keys.

**Testing**:
- xUnit: Domain tests, and integration tests against SQL Server with migrations applied
  (Principle V).
- Karma + Jasmine: `npm run test:ci`.
- Edge DevTools-protocol scripts for visual, RTL, theme, and contrast checks (quickstart V8–V15).

**Target Platform**: The existing Web API plus evergreen desktop and mobile browsers.

**Project Type**: Web application (`src/` backend, `frontend/` Angular).

**Performance Goals**: List and detail ≤ 10% slower than today at seeded size, and a 1,000-request
page still under 1 s (spec SC-009, 003 SC-005). A language or theme switch is visible within 1 s
(spec SC-008).

**Constraints**:
- No rule, code, status, transaction, or SignalR change.
- English output byte-identical when `Accept-Language: en` is sent.
- WCAG 2.1 AA in both themes.
- No horizontal overflow at 360 px or 200% zoom in either direction.
- No web fonts or external requests.
- No translation tables, and no runtime machine translation.

**Scale/Scope**:
- Backend: about 25 files (7 Domain, 10 Application, 4 Infrastructure plus a migration, 4 API).
- Frontend: about 35 files (all templates with text, roughly 150 dictionary entries, about 20
  SCSS files).
- About 15 new test files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|-----------|-------|--------|
| I. Pragmatic Clean Architecture | Domain stays package-free and loses only text (codes and facts remain). Rule logic is unchanged and stays in one place. Culture and HTTP stay in the API, and Application receives a plain `Language` value. **New abstractions, each with a current consumer**: `IAppText` (evaluation DTOs and ProblemDetails), `LocalizedProjections` (every list and detail query), and the AutoMapper dependency (mandated by the request, consumed by those projections). The "active prescription" definition stays in Domain: projection receives Domain-selected ids (R3). | PASS |
| II. Deterministic Server-Side Rules | `RefillEvaluator` conditions and order are untouched. The evaluation is identical in both languages (quickstart V2 parity test). The frontend still only displays the server evaluation. Language never feeds a rule. | PASS |
| III. Workflow Integrity | Transitions, note rules, and the distinct error codes are unchanged. Only `title`, `detail`, and `errors` text varies, and codes are identical (contract). | PASS |
| IV. Persistence & Concurrency | One committed migration with explicit configuration per entity (lengths, required, unique per language). The approval transaction and `rowversion` tokens are unchanged. The success response is re-read after commit, outside the transaction, so the commit contents and notification order are unchanged (R3). | PASS |
| V. Test Evidence | Migration, projection SQL, search, and localization tests run on SQL Server with migrations applied. Existing tests keep their assertions by selecting English explicitly. Domain message assertions become code and kind assertions (equal or stronger). Unrun suites are reported as not run. | PASS |
| VI. Security & Data Handling | DTOs only, and no persistence fields exposed. Seed Arabic names are synthetic. No new logging of names or search text. The AutoMapper license key comes from configuration or user secrets, and is never committed or sent to the browser (R1). The no-sign-in and fictional-data notices are translated, not removed. | PASS |
| VII. Frontend Quality | Feature folders kept. HTTP stays in `RefillRequestsApi`; the interceptor only adds a header. Loading, empty, and error states, warnings, blocking reasons, and success all exist in both languages. Keyboard and labels work in both directions. 360/1280 px in all four combinations. Colors, spacing, and type come from shared tokens. | PASS |
| Delivery / Workflow | `SPEC.md`, `README.md` (language header behavior, license-key setup, re-seed note), and `PROMPTS.md` are updated. README steps are re-verified from a clean copy. The Arabic wording review is recorded (V13). | PASS |

**Gate status (not a principle violation)**: DG-1 condition 3 is **met**. The author's decision
(Community License, spec Clarifications 2026-10-21) was verified from the sources by T001 on
2026-10-21, and the evidence is in research R1. Condition 1 is confirmed by restore in T003, and
condition 2 by T013.

**Post-design re-check (after Phase 1)**: PASS. Three points were checked against the design.
1. **FR-022 and rule reads.** Rule reads still materialize medication rows with both name
   columns. Response names are never taken from them; they come only from per-language
   projections. This interpretation is recorded in R3 and tested by V3.
2. **Default language for API clients.** Changing the default response language for clients that
   send no header (spec C7) is documented, not hidden. Tests pin English explicitly.
3. **Dark theme and 004 FR-007.** The Dark theme keeps clinical content on opaque surfaces with
   computed AA ratios (R12), as amended by spec FR-030.

## Project Structure

### Documentation (this feature)

```text
specs/005-bilingual-localization-themes/
├── plan.md              # This file
├── research.md          # Phase 0: R1–R16 (R1 = DG-1 status)
├── data-model.md        # Phase 1: schema, migration, Domain result shapes, browser state, tokens
├── quickstart.md        # Phase 1: DG-1 gate, API checks V1–V7, browser checks V8–V15, perf V16
├── contracts/
│   ├── http-api.md      # Phase 1: Accept-Language, localized fields, EN/AR message catalog
│   └── ui-contract.md   # Phase 1: document attributes, header controls, direction rules, glossary
├── checklists/
│   └── requirements.md  # Spec quality checklist
├── scripts/
│   └── contrast-check.mjs  # NEW: WCAG pair check over _themes.scss marker blocks (tasks T071)
└── tasks.md             # Phase 2 (/speckit-tasks; not created here)
```

### Source Code (repository root)

```text
Directory.Packages.props                               # + AutoMapper 16.2.0 (only after DG-1)
src/
├── PrescriptionRefill.Domain/
│   ├── Medications/Medication.cs                      # NameAr, NameEn
│   ├── Medications/DrugInteraction.cs                 # DescriptionAr, DescriptionEn
│   ├── Evaluation/RefillEvaluation.cs                 # BlockingReason/Warning without text (R6)
│   ├── Evaluation/RefillEvaluator.cs                  # same rules; emits codes + facts
│   └── RefillRequests/{RefillRequest,DecisionResult,CreateRequestResult}.cs  # NoteError, no text
├── PrescriptionRefill.Application/
│   ├── PrescriptionRefill.Application.csproj          # + AutoMapper reference (after DG-1)
│   ├── Localization/{Language,IAppText,EnglishText,ArabicText,AppText}.cs   # NEW (R5)
│   ├── Mapping/{LocalizedProjections,RefillRequestProfile}.cs              # NEW (R2)
│   ├── DependencyInjection.cs                         # builds both configurations; license key from config
│   └── RefillRequests/
│       ├── Dtos/DtoMapper.cs                          # evaluation text composition only
│       ├── Queries/{GetRefillRequests,GetRefillRequestDetail}.cs   # Language param, ProjectTo, localized search
│       └── Commands/{DecisionCommand,CreateRefillRequest,DecisionOutcome}.cs  # Language param; error kinds
├── PrescriptionRefill.Infrastructure/
│   ├── Persistence/Configurations/{Medication,DrugInteraction}Configuration.cs
│   ├── Persistence/Migrations/<timestamp>_BilingualReferenceData.cs        # NEW (R7)
│   └── Seeding/{DemoDataSeeder,SeedNames}.cs          # NEW SeedNames: seeder-only name map; migration uses literals
└── PrescriptionRefill.Api/
    ├── Program.cs                                     # UseRequestLocalization first; ProblemDetails text by code
    ├── Localization/RequestLanguage.cs                # NEW: culture → Language
    ├── Endpoints/RefillRequestEndpoints.cs            # pass Language; localized validation text
    └── Errors/{ProblemResults,UnexpectedErrorHandler}.cs   # texts from IAppText

tests/
├── PrescriptionRefill.Domain.Tests/...                # message asserts → code/kind/entity asserts
└── PrescriptionRefill.IntegrationTests/
    ├── Infrastructure/{RefillApiFactory,TestData}.cs  # default Accept-Language: en; bilingual builders; license log filter
    ├── Api/LocalizationTests.cs                       # NEW: resolution, Content-Language, AR error texts, parity
    ├── Api/LocalizedSearchTests.cs                    # NEW
    ├── Persistence/ProjectionSqlTests.cs              # NEW: DG-1 condition 2
    ├── Persistence/BilingualMigrationTests.cs         # NEW
    └── Localization/AppTextCatalogTests.cs            # NEW: every member non-empty in both languages

frontend/src/
├── index.html                                         # lang="ar" dir="rtl" data-theme="light"; boot script
├── styles/_themes.scss                                # NEW: light + dark custom properties (R12)
├── styles/_variables.scss, styles.scss                # vars → custom properties; :lang(ar) typography; print
└── app/
    ├── app.config.ts                                  # withInterceptors([acceptLanguageInterceptor])
    ├── app.component.{ts,html,scss}                   # header controls, translated notice
    ├── core/i18n/{language,messages,en,ar,i18n.service,accept-language.interceptor,testing}.ts  # NEW (R9, R10)
    ├── core/preferences/preferences.store.ts          # NEW (R11)
    ├── core/theme/theme.service.ts                    # NEW (R11, R12)
    ├── core/realtime/reload-triggers.ts               # + languageChanged$ trigger
    ├── shared/ui/preference-switch.component.ts       # NEW: 2-option radio group (language, theme)
    ├── shared/ui/icon.component.ts                    # directional mirroring; sun, moon
    ├── shared/time/format-time.ts                     # language argument (R14)
    └── features/refill-requests/**                    # templates read i18n.t(); bidi isolation; logical properties
```

Every new `.ts` file in `core/` and `shared/` gets a `.spec.ts` (research R15).

**Structure Decision**: Keep the four backend projects and the frontend feature layout. New code
goes into `Application/Localization`, `Application/Mapping`, `Api/Localization`,
`frontend/src/app/core/{i18n,preferences,theme}`, and one shared UI component. No new project.

## Implementation Order

1. **Baseline**: run `dotnet test`, `npm run test:ci`, and `npm run build`. Take timings for V16 and
   "before" screenshots.
2. **Domain text removal (R6)**: tests first. Assert codes, kinds, and entities, and confirm no
   rule test changes its outcome.
3. **Catalog and culture (R4, R5)**: `Language`, `IAppText` and its two implementations, request
   localization, ProblemDetails and validation text. The English-pinned test client and the
   `LocalizationTests` resolution cases.
4. **Schema (R7)**: entity constructors, configurations, migration, seeder, test data builders,
   `BilingualMigrationTests`.
5. **DG-1 checkpoint**: if research R1 records condition 3 as verified and met (task T001), add
   AutoMapper. Write
   `ProjectionSqlTests` (V3) **first**, then the projections for list, detail, active
   prescriptions, warning lookups, and localized search (R2, R3, R8). If it is not recorded,
   stop this step and report. Steps 6–8 can proceed.
6. **Frontend core (R9–R11)**, which may run in parallel with steps 2–5 because it shares no files
   with the backend and needs only the contracts: dictionaries (English verbatim), `I18nService`, interceptor,
   `PreferencesStore`, `ThemeService`, boot script, reload trigger, header controls. Add
   `provideI18nTesting('en')` to existing specs.
7. **Frontend presentation (R12–R14)**: theme tokens and Dark set, logical properties,
   directional icons, bidi isolation, Arabic typography, localized dates, Arabic dictionary.
8. **Verification**: quickstart V1–V16, Arabic wording review (V13), and screenshots under
   `screenshots/`.
9. **Docs**: `SPEC.md`, `README.md`, `PROMPTS.md`. Clean-copy README check. Record only
   executed results.

## Complexity Tracking

No constitution violations. These additions were weighed and kept:

| Addition | Why needed | Simpler alternative rejected because |
|----------|------------|--------------------------------------|
| AutoMapper (new package, license-gated) | Mandated by the request (spec Constraints, DG-1). Selects the localized column in SQL. | Hand-written `Select` projections would meet FR-022 with no dependency, but the author kept `ProjectTo` as a requirement (spec Clarifications). It remains the fallback if DG-1 fails. |
| Two mapper configurations (one per language) | Guarantees only one language's column in SQL (FR-022, DG-1 condition 2). | A parameterized projection may emit a `CASE` over both columns (R2). |
| Separate display read alongside the rule read | `ProjectTo` cannot carry Domain evaluation. Rules must stay in Domain. | Choosing names from loaded entities violates FR-022. Re-implementing rules in SQL violates Principle I (R3). |
