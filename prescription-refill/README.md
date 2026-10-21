# Prescription Refill Requests

Two-day full-stack take-home: a clinician reviews fictional refill requests and approves or
denies them against deterministic, server-side safety rules. Backend: ASP.NET Core minimal APIs,
EF Core, SignalR on Microsoft SQL Server. Frontend: Angular 19.

- Requirements (source of truth): [specs/001-refill-request-review/spec.md](specs/001-refill-request-review/spec.md)
- Submission summary and verification log: [SPEC.md](SPEC.md)
- AI prompts and outcomes: [PROMPTS.md](PROMPTS.md)
- Engineering constitution: [.specify/memory/constitution.md](.specify/memory/constitution.md)

## Limitations

- **No authentication or authorization.** Anyone who can reach the app can create, approve, and
  deny requests and receive live notifications. Reviewer identity is not recorded. The live
  update hub is unauthenticated and not production-secure. Do not deploy this demo or use it
  with real patient data.
- **Fictional data.** Patients, medications, and drug interactions are invented for the demo and
  are not clinical guidance.
- **Dispensing is not modeled.** Approval uses one refill but does not change the last filled
  date, so more than one request can be approved within the same supply period while refills
  remain.
- **Requests are created through the API only**; the UI is the clinician review workflow.
- **Displayed evaluations can be out of date.** Each shows when it was evaluated; the server
  re-checks every rule when an approval is processed.
- **Live updates are best effort.** If a notification is missed, screens catch up when they
  reconnect, when you return to them, or when you press Refresh.

## Prerequisites

Verified on the development machine (details:
[research.md R0](specs/001-refill-request-review/research.md#r0-verified-development-environment)):

| Tool | Verified version | Notes |
|------|------------------|-------|
| .NET SDK | 11.0.100-rc.2 building `net10.0`, runtime 10.0.13 | any SDK that can build `net10.0` |
| EF Core CLI | `dotnet-ef` 10.0.2 (global) | `dotnet tool install --global dotnet-ef`; it warns that it is older than EF Core 10.0.13 but works |
| SQL Server | 2022 default instance, Windows authentication | Docker or LocalDB also work (below) |
| Node.js / npm | 22.18.0 / 10.9.3 | the project-local Angular CLI is used via `npm` |
| Browser for frontend tests | Edge 155 | Chrome is used if installed; otherwise Edge is found automatically |

**Windows path length:** clone into a short path (for example `D:\src\prescription-refill`).
From very deep folders, the SQL Server client's native DLL under `bin\...\runtimes\` can exceed
the 260-character Windows path limit and tests fail with `Unable to load DLL
'Microsoft.Data.SqlClient.SNI.dll' ... The filename or extension is too long`. Enabling Windows
long paths also avoids it.

## Database setup

### 1. SQL Server connection

The API reads `ConnectionStrings:RefillDb`. In the Development environment it defaults to the
local default instance with Windows authentication (no secret, in
`src/PrescriptionRefill.Api/appsettings.Development.json`):

```text
Server=.;Database=PrescriptionRefill;Trusted_Connection=True;TrustServerCertificate=True
```

To use another server, override it with user secrets (never commit credentials) or the
environment variable `ConnectionStrings__RefillDb`:

```bash
# Docker alternative (example password for local development only)
docker run -d --name refill-sql -p 1433:1433 \
  -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Local_Dev_Only_1!" \
  mcr.microsoft.com/mssql/server:2022-latest

dotnet user-secrets --project src/PrescriptionRefill.Api set "ConnectionStrings:RefillDb" \
  "Server=localhost,1433;Database=PrescriptionRefill;User Id=sa;Password=<your password>;TrustServerCertificate=True"
```

LocalDB alternative: `Server=(localdb)\MSSQLLocalDB;Database=PrescriptionRefill;Trusted_Connection=True`

### 2. Create the schema and seed fictional data

```bash
dotnet run --project src/PrescriptionRefill.Api -- --seed
```

`--seed` applies the EF Core migrations, adds the demo data if the database is empty, and exits.
Normal startup never migrates or seeds. Seed dates are relative to the current clock, so the
scenarios (eligible, too early, no refills, allergy, interaction, decided history) stay valid.

To apply migrations only:

```bash
dotnet ef database update --project src/PrescriptionRefill.Infrastructure --startup-project src/PrescriptionRefill.Api
```

To start over, drop the database and seed again:

```bash
dotnet ef database drop --force --project src/PrescriptionRefill.Infrastructure --startup-project src/PrescriptionRefill.Api
```

**Arabic names in an existing database (feature 005).** The `BilingualReferenceData` migration keeps
existing medication names and interaction descriptions as the English values. It fills the Arabic
values for the seeded rows, and copies the English value into any other row. To get every authored
Arabic name, either reseed a local demo database (drop it as above, then run `--seed` again; the
seeder skips a database that already has data), or, for a database that must keep its data, apply a
new data migration that ships the corrected values. The committed migration is never edited.

### 3. AutoMapper license key (optional for development)

Localized names are read with AutoMapper 16.2.0 under the free Lucky Penny Community License
(eligibility and terms are recorded in
[research R1](specs/005-bilingual-localization-themes/research.md)). Register for a key at Lucky
Penny Software and keep it out of the repository, in user secrets or an environment variable:

```bash
dotnet user-secrets set "AutoMapper:LicenseKey" "<your key>" --project src/PrescriptionRefill.Api
# or: export AutoMapper__LicenseKey="<your key>"
```

Without a key everything works: AutoMapper logs a license warning under
`LuckyPennySoftware.AutoMapper.License`. Development, test, and evaluation use is allowed without a
license (agreement section 4.4). Tests run without a key and filter that warning.

## Run

```bash
dotnet run --project src/PrescriptionRefill.Api      # API on http://localhost:5080
cd frontend
npm ci
npm start                                             # app on http://localhost:4200
```

The Angular dev server proxies `/api` and the live-update hub `/hubs` (WebSockets) to the API.
Open two browser tabs to see a decision in one refresh the other.

The app opens in **Arabic, right-to-left, Light** on a first visit, whatever the browser's language.
The header has a language control (العربية / English) and a theme control (Light / Dark). Both
choices are kept in this browser, independently of each other, and survive a reload.

Create a request (API only):

```bash
curl -X POST http://localhost:5080/api/refill-requests -H "content-type: application/json" -d '{"prescriptionId": 6}'
```

**Response language.** Every endpoint reads `Accept-Language`: `ar` (the default) or `en`, including
regional variants such as `ar-SA` or `en-GB`; anything else gets Arabic. Responses carry
`Content-Language`. A client that sends no header gets Arabic messages and medication names; send
`Accept-Language: en` for the English output of earlier features. Codes, statuses, and field names
are the same in both languages.

```bash
curl -H "Accept-Language: en" http://localhost:5080/api/refill-requests
```

## Tests

```bash
dotnet test                       # Domain unit tests + SQL Server integration tests
cd frontend
npm run test:ci                   # Karma + Jasmine, headless
```

- Integration tests run against SQL Server, never EF Core InMemory or SQLite. They read the
  server-level connection string from `REFILL_TEST_SQLSERVER` (default
  `Server=.;Trusted_Connection=True;TrustServerCertificate=True`), create a temporary
  `RefillTests_<guid>` database per test class by applying the migrations, and drop it
  afterwards. Without a reachable SQL Server they fail with "SQL Server not available"; report
  them as not run.
- Test classes run four at a time (`tests/PrescriptionRefill.IntegrationTests/xunit.runner.json`), so
  parallel database creation does not time out on a local SQL Server. Test clients send
  `Accept-Language: en` unless a test states another language.
- Frontend tests use Chrome if installed, otherwise Microsoft Edge; set `CHROME_BIN` to use a
  specific Chromium-based browser.

## Project layout

```text
src/PrescriptionRefill.Domain/          entities, refill rules (RefillEvaluator), decisions
src/PrescriptionRefill.Application/     use cases, DTOs, notifier port
src/PrescriptionRefill.Infrastructure/  EF Core DbContext, configurations, migrations, seeder
src/PrescriptionRefill.Api/             minimal API endpoints, ProblemDetails, SignalR hub
tests/                                  Domain unit tests; SQL Server integration tests
frontend/                               Angular app (features/refill-requests, shared, core)
specs/001-refill-request-review/        spec, plan, data model, API contract, tasks
specs/002-premium-ui-redesign/          UI redesign spec, plan, tasks, before/after screenshots
specs/003-backend-pagination/           server-side paging and search: spec, plan, contract, tasks
specs/004-creative-clinical-ui/         Creative Clinical UI spec, plan, contract, tasks, screenshots
specs/005-bilingual-localization-themes/ Arabic/English and Light/Dark: spec, plan, contracts, tasks, screenshots
```

## How it works

- **Rules** live only in the Domain: approval is blocked when no refills remain or when less
  than 80% of the supply period has elapsed since the last fill (exact UTC duration; equality is
  allowed). Allergy and synthetic interaction matches are warnings and never block.
- **Evaluation**: the server evaluates every rule when a request is opened, refreshed, or
  approved. Approve is disabled when the latest evaluation blocks it, but only the server's check
  at approval time counts (`422 approval_blocked` returns the current evaluation).
- **Concurrency**: an approval updates the request and its prescription in one transaction,
  protected by SQL Server `rowversion` on both rows. A conflict rolls everything back and
  returns `409 concurrency_conflict`; nothing is retried automatically. A filtered unique index
  allows only one Pending request per prescription.
- **Live updates**: after a change commits, SignalR sends a hint (`RequestUpdated`, request ID
  and change only) and other screens reload from the API.
- **No caching** of request status, refill counts, or eligibility.
- **Language and theme** are presentation only. The server resolves the request language from
  `Accept-Language` and words messages from a typed catalog keyed by the unchanged codes; each
  medication name and interaction description is selected in SQL for that language. The browser
  keeps the language and theme choices; they never change a rule or a decision.

API reference and error codes: [contracts/http-api.md](specs/001-refill-request-review/contracts/http-api.md).
