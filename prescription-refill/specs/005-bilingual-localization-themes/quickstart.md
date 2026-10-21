# Quickstart Validation: Bilingual Localization and Theme Preferences

Contracts: [contracts/http-api.md](contracts/http-api.md) and
[contracts/ui-contract.md](contracts/ui-contract.md). Data: [data-model.md](data-model.md).
Decisions: [research.md](research.md).

## Gate before any AutoMapper task (DG-1)

1. DG-1 condition 3 is **met**. The Community License was chosen, with eligibility confirmed
   (spec Clarifications 2026-10-21), and verified from the sources on 2026-10-21 (`research.md` R1,
   T001). Before a production release, confirm that a key is configured through user secrets or
   the environment. Tests run without one (license section 4.4, non-production use).
2. After adding the package, both of these must list no AutoMapper or transitive downgrade or
   vulnerability:

   ```bash
   dotnet restore
   dotnet list package --vulnerable --include-transitive
   ```
3. V3 below passes before any other projection is written.

## Baseline (T002, 2026-10-21, before any 005 code)

| Suite | Result |
|-------|--------|
| `dotnet test`, Domain | 47 passed, 0 failed |
| `dotnet test`, Integration (SQL Server `.`) | 103 passed, 0 failed. The first run in the session had 9 failures inside `SqlServerDatabaseFixture.InitializeAsync` (database creation while SQL Server was starting cold). Two later runs passed 103/103 with no code change. |
| `npm run test:ci` | 117 passed (Edge 155) |
| `npm run build` | Succeeded, initial total 424.01 kB |

**V16 "before" timing**: medians of 20 calls after 3 warm-up calls. Throwaway seeded database
`RefillI18n_Check` (dropped afterwards), API on port 5180, measured with curl `time_total`. The
script is in the session scratchpad.

| `Accept-Language` | `GET /api/refill-requests` | `GET /api/refill-requests/6` |
|-------------------|----------------------------|------------------------------|
| `en` | 7.68 ms | 6.30 ms |
| `ar` (ignored before 005) | 7.13 ms | 6.62 ms |

## Prerequisites

As in the [README](../../README.md). Use a **throwaway database and a free port**, never the
developer's running instance (same procedure as the 004 quickstart):

```bash
powershell -NoProfile -Command "Get-NetTCPConnection -State Listen -LocalPort 5180 -ErrorAction SilentlyContinue"   # must print nothing
export ConnectionStrings__RefillDb="Server=.;Database=RefillI18n_Check;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src/PrescriptionRefill.Api -- --seed
dotnet run --no-build --project src/PrescriptionRefill.Api --urls http://localhost:5180
```

Drop `RefillI18n_Check` afterwards. If no SQL Server is available, report V2–V7 and V16 as **not
run** (Principle V).

## Automated

```bash
dotnet test                                   # Domain + SQL Server integration tests
cd frontend && npm run test:ci && npm run build
```

## API checks

| # | Check | Expected |
|---|-------|----------|
| V1 | `GET /api/refill-requests` with `Accept-Language` absent, `ar`, `ar-SA`, `en`, `en-GB`, `fr`, `x;;`, and `fr, en;q=0.8` | `Content-Language` is ar, ar, ar, en, en, ar, ar, en. Medication names match the language. Codes, ids, order, and flags are identical. |
| V2 | Detail for seed scenarios 1–6 in `ar` and in `en` | Identical JSON after removing `message`, `title`, `detail`, `errors`, `medicationName`, `allergies`, and `description` (spec SC-003). `fullName`, `dosage`, and `decisionNote` are identical, untranslated. |
| V3 | SQL of each projection (`ToQueryString()` test on SQL Server) | `ar`: contains `NameAr`/`DescriptionAr` and **not** `NameEn`/`DescriptionEn`. `en`: the reverse. No client-evaluation warning is logged (DG-1 condition 2, spec FR-022). |
| V4 | Approve a blocked request (scenario 6) with `ar`, then with `en` | 422 `approval_blocked` both times. The Arabic body matches the contract example. Nothing changed in the database. |
| V5 | Every error code in `ar` (invalid note, inactive prescription, bad page, unknown id, decided request, pending exists, forced concurrency conflict, malformed JSON) | Same `code` and status as in English, with the Arabic texts from the catalog. |
| V6 | `search=` an Arabic seed name with `ar`. The same Arabic name with `en`. An English name with `ar`. | The first matches. The other two return no match for that medication (spec FR-025). Paging totals are consistent. |
| V7 | Apply migrations to a database created before this feature (from the `InitialCreate` state with seed data) | Names and descriptions kept as English. Seeded Arabic names filled. Both unique indexes exist. `Down` restores the old schema. |

## Browser checks (Edge, DevTools-protocol script outside the repo, as in 002/004)

| # | Check | Expected |
|---|-------|----------|
| V8 | Fresh profile (no storage), browser language English: open the queue and a review | `lang="ar"`, `dir="rtl"`, Light. All text is Arabic. No first-paint flash of LTR or Dark (screenshot at first paint). |
| V9 | Four combinations (ar/Light, ar/Dark, en/Light, en/Dark) × queue, each review state, and a decided request × 360 px, 1280 px, and 200% zoom | No horizontal overflow. Blockers, warnings, note, and buttons visible. Directional icons mirrored only in RTL. Ambient light centered under the pointer in RTL. Mixed text (Latin patient name, dosage, `UTC+03:00`) in correct order. |
| V10 | Switch language on queue page 2 with a search, and on a review with a typed note. Then reload. | Place, search, page size, and note kept. Data reloads in the new language. The choice survives the reload. The theme is untouched. |
| V11 | Switch theme in each language, reload, corrupt both storage keys, and block storage. Also save `en` + Dark, reload with a screenshot at first paint (DevTools `Page.captureScreenshot` on the first `Page.frameStartedLoading` paint, or a screencast frame), and confirm the first frame is already `lang="en"`, `dir="ltr"`, `data-theme="dark"`. | Theme restored and the language untouched. The first painted frame after reload shows no Arabic RTL or Light flash (spec FR-028). Corrupt or blocked storage falls back to ar/Light with no error. |
| V12 | Contrast audit (axe or equivalent) of the queue and review in all four combinations, plus the token-pair calculation script | 0 AA failures (spec SC-006). Results recorded per combination. |
| V13 | Arabic wording review by an Arabic speaker against the glossary in the UI contract | Reviewer's notes recorded. Fixes applied to the dictionary and the server catalog together. |
| V14 | Keyboard only through both header controls in both directions. Screen reader spot check (Narrator). | One tab stop each, arrow keys follow the visual direction, visible focus. The change is announced in the new language. |
| V15 | Forced colors and reduced motion in Dark. Print preview in Dark. | Safety sections, badges, and focus distinguishable. Static composition. Print is legible on light. |
| V17 | In the queue (page 2, with a search) and in a review, switch language 10 times and theme 10 times. Measure each from the click to the new `html[lang]`, `html[dir]` or `data-theme` plus the first translated heading being painted (`performance.now()` around a `MutationObserver` and `requestAnimationFrame` in the DevTools script). | Every switch is visible within 1 s. A language switch's data reload may finish later; the interface text must not wait for it (spec SC-008). |

## Performance

| # | Check | Expected |
|---|-------|----------|
| V16 | Median of 20 list and detail calls at seeded size, before and after, each language. Plus the existing `PagedListPerformanceTests` (1,000 Pending). | ≤ 10% slower than before (spec SC-009). 1,000-request page still under 1 s (003 SC-005). |

## Results

Record each V-check as passed, failed, or not run, with evidence (test names, screenshots under
`screenshots/`). Unexecuted checks are listed as not run, never as passed.

### Results (2026-10-22, all from actual runs)

Environment: local SQL Server, .NET SDK 11.0.100-rc.2 targeting `net10.0`, Angular 19, headless Edge 155.
The browser checks ran against a throwaway database `RefillI18n_Check` (seeded, then dropped), with the
API on port 5180 and the dev server on port 4300 behind a scratchpad proxy. They used a DevTools-protocol
script kept outside the repository.

| # | Result | Evidence |
|---|--------|----------|
| V1 | **Passed** | `LocalizationTests.Each_request_resolves_to_exactly_one_supported_language` (11 header cases, including `*`, empty, and a q-list), `Errors_carry_Content_Language_too`, `List_ids_codes_order_and_flags_are_identical_in_both_languages` |
| V2 | **Passed** | `LocalizationParityTests.Every_seeded_request_is_identical_in_Arabic_and_English_apart_from_text`: all 8 seeded requests; `fullName`, `dosage`, and `decisionNote` identical; Arabic names equal the seed map |
| V3 | **Passed** | `ProjectionSqlTests` (6/6): names, descriptions, and search, each in `ar` and `en`. The SQL names only that language's column, contains no `CASE`, and runs with no EF query warning |
| V4 | **Passed** | `LocalizationTests.Blocked_approval_is_worded_in_Arabic_with_the_same_codes_and_no_change`: Arabic title, detail, reason, and warning texts; JSON equal to English once text is removed; request still Pending |
| V5 | **Passed** | `LocalizationTests`: invalid note (blank, whitespace, too long), inactive and missing prescription, bad status, page, page size, and search, both not-found cases, already decided, pending exists, forced concurrency conflict (nothing saved), malformed JSON, and unknown route |
| V6 | **Passed** | `LocalizedSearchTests` (5/5): only the active-language name matches; patient-name matching unchanged in both languages; totals follow the matches; Arabic search text never logged |
| V7 | **Passed** | `BilingualMigrationTests` (2/2): from `InitialCreate` with data, English kept, seeded Arabic filled from the migration's own literals, unseeded rows copied, both unique indexes exist; `Down` restores `Name`/`Description` and `IX_Medications_Name` |
| V8 | **Passed** | Fresh profile, browser language `en-US`: `lang="ar"`, `dir="rtl"`, Light, Arabic `h1` and title. The attributes at parse end, before the app starts, were already `ar`/`rtl`/`light` (`screenshots/v8-fresh-ar-light-1280.png`) |
| V9 | **Passed** | 60/60: 4 combinations × queue, blocked, warning, no-issues, and decided × 360 px, 1280 px, and 200% zoom. Horizontal overflow 0 everywhere; safety section, note, and both buttons inside the viewport; directional icons mirrored only in Arabic (`screenshots/v9-*`). Mixed-direction instants checked zoomed: the date stays right to left and "18:21 (UTC+03:00)" stays a left-to-right unit |
| V10 | **Passed** | Search kept across a switch; English survives a reload with the theme untouched and no LTR flash; the typed review note survives a switch to Arabic, and the data reloaded in Arabic (`screenshots/v10-review-switched-ar.png`). Covered by specs, not the browser: page 2 and page size (`refill-queue.i18n.spec.ts`), because the seed has fewer than 11 Pending requests |
| V11 | **Passed** | A theme change leaves the language and its key alone. A reload with `en` + Dark is `en`/`ltr`/`dark` and `color-scheme: dark` at parse end (`screenshots/v11-reload-en-dark-1280.png`). Corrupt values fall back to `ar`/Light. With storage blocked, the defaults load with 0 page errors and the theme switch still applies |
| V12 | **Passed in part** | Token-pair calculation `scripts/contrast-check.mjs`: 70/70 pairs meet AA (35 per theme). A first run flagged Light `color-danger-border` (1.74:1), a decorative 004 border; the meaningful alert edges use the state colors, which pass. **Not run**: an axe (or equivalent) audit of the rendered pages, because no audit tool is installed and none was added |
| V13 | **Not run** | No Arabic-speaking reviewer was available. The Arabic texts follow the glossary but are unreviewed |
| V14 | **Passed in part** | Specs cover each control as a native radio group with one tab stop, a legend name, the checked state, `lang` per option, and a polite announcement in the new language (`preference-switch.component.spec.ts`, `app.component.spec.ts`). **Not run**: a keyboard-only pass in a real browser and a Narrator spot check |
| V15 | **Passed** | Forced colors + reduced motion in Dark: the ambient layer is hidden and the safety section keeps a solid border (`screenshots/v15-forced-colors-dark.png`). Print media in Dark resolves to the Light token values (`#0f172a` text on `#ffffff`) (`screenshots/v15-print-dark.png`) |
| V16 | **Passed** | Medians of 20 calls (seeded size). After: list 5.1–5.4 ms and detail 4.5–4.6 ms in both languages, against a baseline of 7.1–7.7 ms and 6.3–6.6 ms, so within +10%. `PagedListPerformanceTests` (1,000 Pending) passed in every full run |
| V17 | **Passed** | 10 language switches on the queue took 6–16 ms each until the new `lang` and translated `h1` were painted. 10 theme switches took 1–6 ms each. Every switch was well under 1 s |

**Clean copy (T091)**: a fresh copy without build output, `node_modules`, or `.git`, with a new throwaway
database, following the README:
- `--seed` seeded once, then skipped.
- With no `Accept-Language` header, the API answered `Content-Language: ar` with Arabic names; with
  `Accept-Language: en`, English names.
- `POST` returned 201; the duplicate returned 409 `pending_request_exists` with an Arabic title.
- `npm ci` and `npm run test:ci` (158/158) passed, and `npm run build` succeeded.
- `npm start` with a browser session was not part of this pass.

**Test suites**: `dotnet test` passed 47/47 Domain and 187/187 integration tests in three consecutive runs.
`npm run test:ci` passed 159/159, and `npm run build` succeeded.

**Changed existing tests** (spec SC-010):
- **Domain**: assertions on message strings now assert codes, `InvalidData` kinds, `NoteError`, or the
  referenced entity. No outcome assertion changed.
- **Integration**:
  - The test client sends `Accept-Language: en` by default.
  - `SchemaTests` inserts both description columns.
  - `SeederTests` checks the bilingual seed.
- **Frontend**: every existing spec gained `provideI18nTesting()` (English). No assertion changed.
