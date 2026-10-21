# Research: Bilingual Localization and Theme Preferences

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-21

Each entry gives the Decision, the Rationale, and the Alternatives considered. "Spec FR-xxx" means
this feature's spec; "001/003/004 FR-xxx" refers to the earlier features.

---

## R1. AutoMapper version, license, and dependency gate DG-1

**Decision**: The candidate is **AutoMapper 16.2.0**, registered in `Directory.Packages.props` and
referenced only by `PrescriptionRefill.Application`. All four conditions are met: condition 3 by
T001 (2026-10-21), condition 1 by T003, and condition 2 by T013/T040 (2026-10-22). The status of
each condition:

| DG-1 condition | Status at planning | Evidence / remaining action |
|----------------|--------------------|-----------------------------|
| 1. Version: net10.0, no open advisory, dependencies resolve | **Met (T003, 2026-10-21)**: `dotnet restore` succeeded, and `dotnet list package --vulnerable --include-transitive` reported "no vulnerable packages" for all 6 projects. Resolved versions: AutoMapper 16.2.0; Logging.Abstractions 10.0.13 (pinned, not downgraded); Options 10.0.13; IdentityModel.JsonWebTokens, Tokens, Logging, and Abstractions 8.14.0 (new transitive). `dotnet build` succeeded. | 16.2.0 targets net10.0 and is past the GHSA-rvv3-g6hj-g44x fix (16.1.1). It needs Microsoft.Extensions.Logging.Abstractions ≥ 10.0.0 (10.0.13 is pinned), Microsoft.Extensions.Options ≥ 10.0.0, and Microsoft.IdentityModel.JsonWebTokens ≥ 8.14.0. Transitive pinning is on, so `dotnet restore` plus `dotnet list package --vulnerable --include-transitive` confirms it at task time. |
| 2. EF Core translation | **Met (T013/T040, 2026-10-22)** | `tests/PrescriptionRefill.IntegrationTests/Persistence/ProjectionSqlTests.cs` runs on SQL Server. For the medication-name projection, the interaction-description projection, and the search predicate, in each language, `ToQueryString()` contains only that language's column and no `CASE`. The queries execute, and no `Microsoft.EntityFrameworkCore.Query` warning is logged. 6 of 6 passed. |
| 3. License confirmed in writing by the author | **Met (verified 2026-10-21, T001)** | The author chose the Community License and confirmed eligibility (spec Clarifications 2026-10-21). Verification evidence is below. No paid license is assumed. |
| 4. License key handling | **Designed** | The key is read from configuration key `AutoMapper:LicenseKey`: user secrets in development, environment variable elsewhere. It is never committed and never sent to the browser. With no key, AutoMapper works and logs warnings under `LuckyPennySoftware.AutoMapper.License`. Test hosts filter that category so warnings don't affect log assertions. |

**T001 verification evidence (retrieved 2026-10-21)**:

1. **Package metadata**: the nuspec, from
   `https://api.nuget.org/v3-flatcontainer/automapper/16.2.0/automapper.nuspec`, plus
   `https://www.nuget.org/packages/AutoMapper/16.2.0`.
   - **License**: `<license type="file">LICENSE.md</license>` with
     `requireLicenseAcceptance=true`.
   - **Assets**: net10.0, among others.
   - **net10.0 dependencies**: Microsoft.Extensions.Logging.Abstractions ≥ 10.0.0,
     Microsoft.Extensions.Options ≥ 10.0.0, Microsoft.IdentityModel.JsonWebTokens ≥ 8.14.0.
   - **Advisories**: none are flagged on 16.2.0. 16.0.0 and 16.1.0 are flagged as high severity.
2. **License terms**:
   - The packaged `LICENSE.md` (github.com/LuckyPennySoftware/AutoMapper, `LICENSE.md`) grants
     RPL-1.5. As the alternative to its reciprocal obligations, it points to the License
     Agreement at `https://luckypennysoftware.com/license`.
   - That agreement is a PDF, "Software License Agreement, License document version 2.0", 18
     pages, and shows no date. Its section 4.2(g) defines the Community License:
     - Gross annual revenue under US $5,000,000 for each year it is held (4.2.g.3.1), aggregated
       with any controlling entity. Alternatively, a nonprofit with an annual budget under
       US $5,000,000.
     - Never more than US $10,000,000 of outside capital, such as private equity or venture
       capital (4.2.g.3.2).
     - Not available to government or quasi-government agencies (4.2.g.1).
     - Individual developers may use it for any legal purpose, including commercial use. They
       may not use it on behalf of an entity that would not itself qualify (4.2.g.2, 4.2.g.8).
     - Universities are excluded for institutional software. Individual educational use is
       allowed (4.2.g.3.3).
     - Non-transferable (4.2.g.5). Perpetual while the holder stays eligible, with no renewals
       (4.2.g.9).
     - Ineligibility must be reported within 60 days, followed by an upgrade (4.2.g.10).
     - Lucky Penny may request proof of eligibility (4.2.g.4) and has the final say
       (4.2.g.13).
     - Provided "as is", with no warranty (4.2.g.12).
   - Section 4.4 also allows development, testing, staging, and evaluation use at no cost and
     without a license, which covers this repository's tests.
3. **Key configuration**: from `https://docs.automapper.io/en/latest/License-configuration.html`.
   - The key is set as `cfg.LicenseKey` inside the configuration expression: in
     `services.AddAutoMapper(cfg => ...)`, or in `new MapperConfiguration(cfg => ..., loggerFactory)`
     when DI isn't used.
   - With no key set, AutoMapper falls back to the `AUTOMAPPER_LICENSE_KEY` and then
     `LUCKYPENNY_LICENSE_KEY` environment variables. With neither, it logs a warning, and "a
     missing/invalid license key does not affect runtime behavior in any way".
   - This confirms the R1 design. `LocalizedProjections` copies configuration
     `AutoMapper:LicenseKey` into `cfg.LicenseKey` for both configurations and passes the host's
     `ILoggerFactory`.

**Rationale**: 14.0.0 (MIT) has a high-severity advisory with no 14.x patch, so it fails condition 1.
15.1.3 is also eligible, but 16.2.0 is the newest line with no advisory and explicit net10.0
assets. Restricting AutoMapper to Application keeps Domain package-free (Principle I) and keeps
mapping next to the DTOs.

**If the gate fails**: work stops at the projection tasks and alternatives are reported (spec
DG-1). These include Mapperly (MIT, source-generated, supports `IQueryable` projection) and
hand-written `Select` projections. The requirement is not changed without the author's decision.

**Alternatives considered**: AutoMapper 14.0.0 (rejected: advisory). AutoMapper 15.1.3 (acceptable
fallback if 16.x shows an incompatibility). `services.AddAutoMapper(...)` with a single
configuration (rejected: see R2).

## R2. How the localized column is selected inside SQL

**Decision**: Build **two `MapperConfiguration` instances at startup**, one per language, from
the same profile code. The profile takes the language as a constructor argument and passes
language-specific member expressions to `MapFrom`, for example
`m => m.NameAr` versus `m => m.NameEn`. A singleton `LocalizedProjections` returns the
`IConfigurationProvider` for a `Language`. Queries call
`ProjectTo<TDto>(projections.For(language))`.

**Rationale**: Each language's expression tree contains only its own column, so the generated SQL
selects `[m].[NameAr]` **or** `[m].[NameEn]`, never both. This meets spec FR-022 and DG-1
condition 2 literally. Query plans stay cacheable because the SQL text differs only by language.
Both configurations are validated at startup with `AssertConfigurationIsValid()`.

**Alternatives considered**:
- *AutoMapper parameterized projection* (`ProjectTo<T>(cfg, new { language })` with
  `language == "ar" ? s.NameAr : s.NameEn`). Rejected: depending on how the parameter is
  substituted, SQL Server receives `CASE WHEN @language = N'ar' THEN [NameAr] ELSE [NameEn] END`,
  which references both columns. That is ambiguous against FR-022.
- *Computed, non-mapped C# property on the entity* (`LocalizedName`). Rejected: not translatable
  by EF Core, so it forces client evaluation.
- *SQL view per language*. Rejected: adds schema objects and duplicates mappings for no gain.

## R3. Where projection applies, and how it coexists with rule evaluation

**Decision**: Two kinds of read, with clear roles.
1. **Rule reads (unchanged)**: `RefillDecisionContextLoader` and `GetRefillRequests.EvaluatePendingAsync`
   keep loading the entities the Domain rules need (Principle II). Their names are never used for
   responses.
2. **Display reads (new, `ProjectTo`)**: every name or description that appears in a response
   comes from a projection in the resolved language:
   - List items: `RefillRequest → RefillRequestSummaryDto` (evaluation flags are ignored by the
     map and merged by id afterwards). Paged pending: the projection runs over the page's ids.
     Unpaged and decided lists: the ordered projection is the list.
   - Detail: `RefillRequest → RefillRequestDetailDto` without `activePrescriptions` and
     `evaluation`. The patient's allergies are projected as localized names ordered by the
     localized name (spec FR-023).
   - Active prescriptions: the **Domain** still selects which prescriptions count
     (`ActiveMedications.For`, 001 FR-022). Their ids are then projected
     (`Prescription → ActivePrescriptionDto`) and returned in the Domain's order, so the rule
     definition is not duplicated in SQL.
   - Evaluation text: the Domain `Warning` refers to the `Medication` and `DrugInteraction` it is
     about, not a name (R6). Application projects `Medication → LocalizedNameDto(Id, Name)` and
     `DrugInteraction → LocalizedTextDto(Id, Description)` for the referenced ids, then composes
     the localized messages (R5).
   - Decision success response: after commit and notification, the detail is re-read through the
     same projection. The transaction, its contents, and the notification order are unchanged
     (spec FR-001). Today the response is built from the tracked entities. The re-read returns the
     committed values, which are identical.
   - **Merge rule**: values that a rule reads, or that can change after creation (refill counts,
     last filled, days supply, active flag, status, and the evaluation including `eligibleAt`),
     are taken from the rule read. The display
     read contributes only localized text and values fixed at creation. So the displayed facts
     and the evaluation always come from the same read, even when a concurrent approval commits
     between the two queries. Approval still re-evaluates every rule against current persisted
     data inside its unit of work (constitution Principle III); none of this changes it.

**As implemented (2026-10-22)**: the display read is narrower than the list above. It is
`LocalizedReferenceData`: two `ProjectTo` lookups, `Medication → LocalizedNameDto` and
`DrugInteraction → LocalizedTextDto`, over the ids that one response needs, with one round trip
each. List items, the detail, active prescriptions, allergies, warnings, and the decision
responses take every name and description from it. Every other field, including all safety
values, comes from the rule read.

This meets FR-022: every response name is selected in SQL for one language. The merge rule holds
by construction, because no safety value can come from the display read. The decision success
response is built from the entities that the command committed, as before 005, plus the
localized lookup. No detail re-read happens after commit, so the F8 caveat (a later change
showing in the response) does not arise. Full-DTO projections of `RefillRequest` were not added:
they would duplicate the rule read's fields and reintroduce the two-snapshot risk that the merge
rule exists to prevent.

**Rationale**: `ProjectTo` cannot carry Domain evaluation, which needs entity graphs. Splitting
display from rules keeps every rule in Domain and moves every localized value into SQL. Rule
reads load whole medication rows, including both name columns, because EF Core materializes
entities. FR-022 governs how response names are *chosen*, and none comes from those rows. This
interpretation is recorded in the plan's constitution check.

**Alternatives considered**:
- Choose the name in memory from loaded entities. Rejected: violates FR-022.
- Split medication names off with table splitting so rule reads skip them. Rejected:
  disproportionate, and it is a translation table in all but name.
- Re-implement "active prescription" in SQL for the projection. Rejected: duplicates a Domain
  rule (Principle I).

**Cost**: one extra read per list page or detail, plus at most one small lookup read for warning
names. Measured against today in quickstart V16 (spec SC-009).

## R4. Request language resolution

**Decision**: Use ASP.NET Core request localization (in the shared framework, so no package) with:
- `SupportedCultures` = `SupportedUICultures` = `ar`, `en`; `DefaultRequestCulture` = `ar`.
- Only the `AcceptLanguageHeaderRequestCultureProvider`. The query-string and cookie providers
  are removed so there is one deterministic input (spec FR-010).
- `FallBackToParentCultures` and `FallBackToParentUICultures` = true, so `ar-SA` → `ar` and
  `en-GB` → `en`. A missing, malformed, or unsupported value (`fr`) falls to the default, `ar`.
  Quality values are respected and the first supported entry wins.
- `ApplyCurrentCultureToResponseHeaders` = true, so responses carry `Content-Language: ar|en`.
- `app.UseRequestLocalization()` is the **first** middleware, before the exception handler and
  status pages, so framework-produced problems are localized too.
- The API maps the resolved UI culture to an Application `Language` enum (`Arabic`, `English`)
  once per request (`RequestLanguage.From(HttpContext)`) and passes it **explicitly** to
  Application queries and commands. Application never reads ambient culture, which keeps it
  deterministic and testable.

**Rationale**: The constraint asks for ASP.NET Core request culture resolution. `Accept-Language`
is the standard, CORS-safelisted header and the frontend sets it explicitly on every API call
(R10), so the browser's own language never decides (spec FR-009). Non-browser clients that send
nothing get Arabic (spec C7).

**Formatting safety**: Request parsing already uses `CultureInfo.InvariantCulture`. JSON
serialization of numbers and instants is culture-invariant. The text catalog (R5) formats numbers
with the invariant culture, so Western digits appear in both languages (spec FR-016) and no Hijri
or other calendar can appear.

**Alternatives considered**: a custom header (`X-Language`; rejected as non-standard), a query
parameter (rejected: leaks into URLs and caching), a culture cookie (rejected: implicit state, and
SignalR and tests would need it), and keeping all three default providers (rejected:
non-deterministic precedence).

## R5. Server-side text catalog

**Decision**: A typed catalog in Application: interface `IAppText`, with one method or property
per message (parameters typed, for example `TooEarly(int daysSupply)` and
`AllergyConflict(string medicationName)`). It has two implementations, `EnglishText` and
`ArabicText`, and `AppText.For(Language)` selects one. The API uses the same catalog for
ProblemDetails `title`, `detail`, and `errors` texts. That includes framework-produced 400 and 404
problems (in `CustomizeProblemDetails`, by `code`) and the unexpected-error handler. Every
message's English text is today's text, verbatim. The Arabic texts are in
[contracts/http-api.md](contracts/http-api.md).

**Rationale**: The compiler guarantees that both languages define every message (spec FR-017,
FR-018) and that parameters match. It adds no package: `IStringLocalizer` would add
`Microsoft.Extensions.Localization` to Application, against "reuse installed packages". It also
keeps codes and text separate (spec FR-002).

**Alternatives considered**: `.resx` with `IStringLocalizer` in the API only (rejected: evaluation
DTOs are built in Application, and resx keys fail at runtime, not compile time), and localizing in
the browser by code (rejected: the request asks for server culture resolution, and API clients
would get no localized text).

## R6. Removing message text from Domain without touching rules

**Decision**: Domain results carry **codes and facts, not text**:
- `BlockingReason(Code, EligibleAt, InvalidData)`, where `InvalidData` is a nullable
  `InvalidPrescriptionData` with values
  `DaysSupplyNotPositive`, `LastFilledMissing`, or `LastFilledAfterEvaluation`. TooEarly's message
  needs only the prescription's days supply, which Application already has.
- `Warning(Code, Medication Medication, DrugInteraction? Interaction)`, which references the
  entities the warning is about. Application reads their ids for the localized lookup (R3).
- `DecisionResult.InvalidNote(NoteError Error)` with `Required` and `TooLong`, and
  `CreateRequestResult.PrescriptionInactive()` with no text.

Every condition, comparison, and order in `RefillEvaluator` and `RefillRequest` is untouched.
Domain tests change only where they asserted message strings; they now assert the code, kind, or
referenced entity, which is equal or stronger.

**Rationale**: Text is presentation (Principle I). Keeping English strings in Domain would be dead
code once the catalog exists.

**Alternatives considered**: Keep Domain strings and translate them by matching text (rejected:
fragile), and keep Domain strings as "English fallback" (rejected: dead code, and two sources for
English).

## R7. Schema and data migration

**Decision**: One migration, `BilingualReferenceData`:
- `Medications`: rename `Name` → `NameEn`. Add `NameAr nvarchar(100) NOT NULL`. Replace the unique
  index on `Name` with unique indexes `IX_Medications_NameEn` and `IX_Medications_NameAr`.
- `DrugInteractions`: rename `Description` → `DescriptionEn`. Add
  `DescriptionAr nvarchar(300) NOT NULL`.
- Data step, inside the migration: set `NameAr` for the 11 seeded medication names and
  `DescriptionAr` for the seeded description, using the data-model "Seed names" values **written
  as literal values in the migration file**. The migration never references the seeder's
  `SeedNames` list, so it stays fixed once committed. Any other existing row (not produced by the
  seeder) gets `NameAr = NameEn` and `DescriptionAr = DescriptionEn`, so the migration never
  fails.
- **Later wording corrections** (for example from the V13 review) change `SeedNames.cs` for new
  databases. Databases that are already migrated are updated by a **new** data migration, never
  by editing `BilingualReferenceData`. The documented alternative for a local demo database is a
  reseed: drop the database, then run `dotnet run --project src/PrescriptionRefill.Api -- --seed`
  (the seeder skips a database that already has data). The README states both. The columns are added nullable, filled, then altered to NOT NULL
  before the Arabic unique index is created.
- `Down` reverses the change (drops the Arabic columns, renames back).

The Domain constructors become `Medication(string nameAr, string nameEn)` and
`DrugInteraction(Medication, Medication, string descriptionAr, string descriptionEn)`, each with
the same trim and length rules as today, applied per language.

**Rationale**: A rename preserves data and foreign keys. Constitution Principle IV requires
committed migrations, and Principle V requires tests to apply them.

**Alternatives considered**: drop and recreate the columns (rejected: data loss), and a separate
`MedicationTranslations` table (forbidden by spec FR-020).

## R8. Search on the localized name

**Decision**: `Matching` takes the `Language` and selects the **member expression outside the
query**: for Arabic `r.Prescription.Medication.NameAr`, for English `...NameEn`. Patient name
matching is unchanged. The existing lower-casing is kept (it is a no-op for Arabic script).
Nothing is logged.

**Rationale**: Spec FR-025 says active-language only. An inline conditional would put a `CASE`
over both columns in SQL. The search term is still a parameter.

## R9. Frontend translation mechanism

**Decision**: An in-house, typed, runtime mechanism with **no new package**:
- `core/i18n/messages.ts` defines `interface Messages`, a nested, typed tree of every UI string
  and of parameterized strings as functions. `en.ts` and `ar.ts` each export
  `const en: Messages` and `const ar: Messages`, so the compiler rejects a missing key in either
  language.
- `I18nService` (root) holds `language = signal<Language>()` and `t = computed(() => dictionary)`,
  and exposes `setLanguage()`. Components read `i18n.t().queue.title`, which works with OnPush and
  signals.
- English values are today's strings, verbatim (spec FR-017).

**Rationale**: Spec FR-012 requires switching without a page reload. `@angular/localize` compiles
one bundle per locale and needs a reload or a second deployment. It is also not installed.
`ngx-translate` and `transloco` would be new packages. A typed dictionary is small, with roughly
150 strings.

**Alternatives considered**: `@angular/localize` (rejected: build-time and reload), ngx-translate
(rejected: new package and string keys with no compile-time check), and a JSON dictionary loaded
over HTTP (rejected: an extra request and a flash of untranslated text).

## R10. Language on API calls, reloads, and in-flight responses

**Decision**:
- A functional `HttpInterceptorFn` (`core/i18n/accept-language.interceptor.ts`) sets
  `Accept-Language: ar` or `en` on requests to `/api/`. It is registered with
  `withInterceptors`. HTTP calls stay in `RefillRequestsApi` (Principle VII).
- `reloadOnChanges` (`core/realtime/reload-triggers.ts`) gains a fourth trigger:
  `I18nService.languageChanged$`, which emits on change only, not the initial value. This amends
  001 FR-023, as spec FR-012 requires. The queue and review pages already route reloads through
  their `switchMap` pipelines, so only the latest response is shown (003 FR-016). The queue keeps
  status, search, page, and size; the review keeps the typed note, as it already does on reload
  (001 FR-032).
- A decision in flight is **not** cancelled by a language switch. Decision requests are not part
  of the reload pipeline, and their outcome text comes from the response already received. After
  it resolves, the page reloads in the new language (spec edge case).
- SignalR is unchanged. Hints carry no text (spec FR-003).

## R11. Preference storage and applying it before first paint

**Decision**:
- `localStorage` keys `prx.language` (`ar` | `en`) and `prx.theme` (`light` | `dark`), written
  independently by `PreferencesStore` (`core/preferences/`). Every read and write is wrapped in
  `try/catch`. An invalid value or a storage error yields the default (Arabic, Light; spec FR-009,
  FR-027).
- A small inline script in `index.html` (about 15 lines, before `<app-root>`) reads both keys with
  the same validation and sets `<html lang dir data-theme>` and
  `<meta name="color-scheme" content="light|dark">` before CSS paints (spec FR-028). The static
  markup defaults to `lang="ar" dir="rtl" data-theme="light"`, so a disabled script still yields
  the defaults. After bootstrap, `I18nService` and `ThemeService` read the same store and own those
  attributes, plus `document.title`.
- No cross-tab synchronization (the spec edge case keeps each tab as-is until reload). Multi-tab
  behavior follows browser-local storage semantics: both tabs share one `localStorage` per
  origin, a value written by one tab is read by another only on its next load, and no `storage`
  event listener is added. The last write wins.

**Rationale**: No server-side identity exists (001 FR-020), so preferences live in the browser.
The inline script is the only way to avoid a flash of the wrong theme or direction without
server rendering.

**Alternatives considered**: cookies (rejected: sent to the API and of no use there), and
`sessionStorage` (rejected: does not persist across visits).

## R12. Theme tokens and the Dark palette

**Decision**:
- The color roles in `_variables.scss` become CSS custom properties defined in
  `styles/_themes.scss` under `:root[data-theme='light']` and `:root[data-theme='dark']`. The
  SCSS variables keep their names but point at the properties
  (`$color-text: var(--color-text)`), so component SCSS needs no rename.
- The 12 SCSS color-function uses (`rgba($…)`, `mix(…)`) cannot work on `var()`. Each becomes a
  named token with an explicit value per theme (for example `--focus-ring`, `--shadow-color`,
  `--canvas-border`).
- In Dark, the sheet and cards stay **opaque** (spec FR-030). Glow opacity is reduced and the
  ambient layer stays behind opaque surfaces (004 layering rules).
- `@media print` forces the Light token set (spec FR-033). In forced colors, the 004 rules apply
  unchanged in both themes.

**Dark tokens** (contrast computed per WCAG 2.1; values verified again at implementation):

| Role | Value | Contrast |
|------|-------|----------|
| `--color-bg` (sheet) | `#0b1220` | — |
| `--color-surface` | `#121b2d` | — |
| `--color-surface-subtle` | `#172338` | — |
| `--color-surface-sunken` | `#0e1728` | — |
| `--color-text` | `#e6edf6` | 14.6:1 on surface, 15.9:1 on bg |
| `--color-text-muted` | `#aebbcc` | 8.8:1 on surface, 8.1:1 on subtle |
| `--color-text-subtle` | `#94a3b8` | 6.7:1 on surface, 6.1:1 on subtle |
| `--color-border-strong` (input borders) | `#6b7f99` | 4.2:1 on surface (≥ 3:1) |
| `--color-accent` (text and links) | `#2dd4bf` | 9.2:1 on surface, 8.5:1 on subtle |
| primary button: bg `--color-accent`, text `--color-on-accent` `#042f2e` | — | 7.8:1 |
| primary hover bg `#5eead4` | — | 9.8:1 with `--color-on-accent` |
| `--color-accent-2` | `#a5b4fc` | 8.6:1 on surface |
| danger text / bg | `#fca5a5` / `#2b1416` | 9.1:1 |
| danger border | `#f87171` | 6.2:1 on surface |
| danger button: bg `#f87171`, text `#2b0a0a` | — | 6.6:1 |
| warning text / bg | `#fcd34d` / `#2a2109` | 11.0:1 |
| warning border | `#f59e0b` | 8.0:1 on surface |
| success text / bg | `#86efac` / `#0f2a1c` | 10.9:1 |
| success border | `#22c55e` | 7.6:1 on surface |
| info text / bg | `#93c5fd` / `#10213d` | 8.9:1 |
| info border | `#60a5fa` | 6.8:1 on surface |
| focus outline | `#7dd3fc` | 10.3:1 on surface, 11.2:1 on bg, 10.8:1 on canvas |
| canvas (shell) | `#060d1a`, canvas text unchanged | separation from sheet by `--shadow-sheet` and a 1px sheet border |

The Light token set is the 004 set, unchanged. Its ratios are already documented in
`_variables.scss` comments.

**Rationale**: Runtime theme switching without a reload (spec acceptance scenario 3.2) requires
CSS custom properties. Keeping the SCSS names limits churn across about 20 component styles.

**Alternatives considered**: two compiled stylesheets swapped at runtime (rejected: doubles CSS
and causes flashes), `prefers-color-scheme` (rejected: spec default is Light, a deterministic
choice), and `color-scheme: light dark` with `light-dark()` (rejected: weaker support in the
checked browsers and it is still OS-driven).

## R13. Right-to-left layout and bidirectional text

**Decision**:
- Replace the 15 physical-direction declarations found (`margin-left`, `padding-left`, `left:`,
  `border-left*`, `text-align: right`, `border-right-color`, `padding-right`) with logical
  properties (`margin-inline-start`, `padding-inline-start`, `inset-inline-start`,
  `border-inline-start*`, `text-align: end`). Flex and grid order follows `dir` automatically.
- The icon component gains a `directional` input. Directional icons (back arrow, chevrons,
  Previous/Next) mirror with `:dir(rtl)` / `[dir=rtl]` `transform: scaleX(-1)`. Others (warning,
  check, info, pill, sun, moon) never mirror (spec FR-014).
- Recorded and mixed-direction text (patient name, dosage, note, medication name, eligible time) is
  isolated: `dir="auto"` on its element, or `<bdi>` inline. Times and UTC offsets are wrapped in an
  LTR isolate so `UTC+03:00` keeps its order (spec FR-015).
- Arabic typography: `:lang(ar)` sets `letter-spacing: 0`, because letter-spacing breaks Arabic
  joining (9 uses today). It also puts Arabic-capable system fonts first:
  `'Segoe UI', 'Noto Sans Arabic', 'Geeza Pro', Tahoma, …`. No web fonts (002/004 decision).
  Line height stays at 1.5 or more for Arabic diacritics.
- Ambient light: pointer coordinates are physical (`clientX`), so the light follows the pointer
  in RTL without change. Verified in quickstart V9.

**Alternatives considered**: a separate RTL stylesheet (rejected: drifts), and the `rtlcss` build
step (rejected: new package, and logical properties are native).

## R14. Dates, times, and numbers in the interface

**Decision**: Keep the hand-written formatter in `shared/time/format-time.ts`, which is
deterministic and does not depend on ICU. Add a language argument:
- English: unchanged (`20 Oct 2026, 14:05 (UTC+03:00)`).
- Arabic: `20 أكتوبر 2026، 14:05 (UTC+03:00)`, using month names يناير، فبراير، مارس، أبريل، مايو،
  يونيو، يوليو، أغسطس، سبتمبر، أكتوبر، نوفمبر، ديسمبر, the Arabic comma, Western digits, and the
  time and offset in an LTR isolate.
- `formatDateOnly` gets the same treatment. Counts and refill numbers render as plain Western
  digits.

**Rationale**: Spec FR-016. `Intl.DateTimeFormat('ar-u-nu-latn')` output varies by browser in
month names and separators and would make tests brittle.

## R15. Test strategy

**Decision**:
- **Backend**: `RefillApiFactory.CreateClient` sets a default `Accept-Language: en` header, so the
  existing English assertions keep their meaning. They select English explicitly (spec SC-010). New
  tests cover:
  - resolution rules: none, `ar`, `ar-SA`, `en-GB`, `fr`, malformed, and a quality list, plus the
    `Content-Language` header;
  - parity: the same seeded scenario in both languages yields identical JSON once human-readable
    fields are removed (spec SC-003);
  - an Arabic text example for every error code;
  - projection SQL: `ToQueryString()` contains only the language's column (DG-1 condition 2);
  - search: a medication's other-language name finds nothing;
  - the migration on SQL Server, including the seed name mapping.

  A catalog test checks that every `IAppText` member returns non-empty text in both languages.
- **Frontend**: `provideI18nTesting('en')` sets English for existing specs, with no assertion
  weakened. New specs cover:
  - `I18nService` and `ThemeService` defaults, persistence, invalid storage, and independence;
  - the interceptor header;
  - the language-change reload keeping status, search, page, and note;
  - `dir`/`lang`/`data-theme` attributes;
  - an Arabic rendering of each safety state;
  - format-time in both languages;
  - a dictionary-parity test that no `ar` value is empty or identical to `en`, except an explicit
    allow-list of tokens such as `UTC`.
- **Browser**: Edge with real screenshots, as in 002/004. It covers the four combinations at
  360 px, 1280 px, and 200% zoom, plus an axe-style contrast audit (quickstart V8–V11).
- Unavailable SQL Server or browser runs are reported as **not run** (Principle V).

## R16. Performance

**Decision**: Measure with the existing `PagedListPerformanceTests` (1,000 Pending requests) and a
seeded-size timing before and after (quickstart V16). The budget is ≤ 10% slower at seeded size
(spec SC-009) and still within 003 SC-005 (1 s at 1,000). If the budget fails, the first lever is
to merge the page projection into the existing page-id query. That changes no behavior.
