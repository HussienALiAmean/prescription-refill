# Research: Prescription Refill Request Review

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-20

Each entry: Decision / Rationale / Alternatives considered. Version numbers appear only where
they were verified on the development machine on 2026-10-20 (R0).

## R0 Verified development environment

Checks run on 2026-10-20 (commands in [quickstart.md](quickstart.md#verify-the-environment)):

| Component | Verified | How |
|-----------|----------|-----|
| .NET SDKs | 10.0.100-rc.1.25451.107, 11.0.100-rc.2.26504.105 (default; both previews) | `dotnet --list-sdks` |
| .NET runtimes | Microsoft.NETCore.App / AspNetCore.App 10.0.13 (also 8.0.32, 11.0.0-rc.2) | `dotnet --list-runtimes` |
| net10.0 build | Scratch `dotnet new web -f net10.0` built with the default SDK, ran on 10.0.13, returned HTTP 200 | scratch folder outside the repo |
| EF Core SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` 10.0.13 restored and built | same scratch project |
| dotnet-ef | global tool 10.0.2 | `dotnet tool list -g` |
| SQL Server | default instance `MSSQLSERVER` running; 16.0.1000.6 (SQL Server 2022 RTM), Enterprise Edition; Windows authentication only; current user can create databases | `sqlcmd -S . -E` read-only query |
| LocalDB | `MSSQLLocalDB` 17.0.4025.3, stopped (not used) | `sqllocaldb info` |
| Docker | client 29.1.3; engine not running | `docker version` |
| Node / npm | 22.18.0 / 10.9.3 | `node --version` |
| Angular | global CLI 19.0.0; scratch `ng new` resolved @angular/core 19.2.25, CLI and build-angular 19.2.27, TypeScript 5.6.3, Karma 6.4.4, Jasmine 5.4.0; `ng build` succeeded; `ng test` ran 3/3 default tests in headless Edge | scratch folder outside the repo |
| SignalR JS client | `@microsoft/signalr` 10.0.11 installed in the scratch app (not built against a hub) | `npm ls` |
| Browsers | Microsoft Edge 155.0.4283.45; Chrome not installed | file check |

The scratch checks show the toolchain works; they are not application tests.

## R1 Runtime and SDK

- **Decision**: Target `net10.0`. Build with the installed default SDK (11.0.100-rc.2), which was
  verified to build `net10.0` and run on the installed 10.0.13 runtime. No `global.json` and no
  SDK installation or upgrade.
- **Rationale**: Uses what is installed and verified; `net10.0` is an LTS target that any
  .NET 10 or later SDK on a reviewer's machine can build.
- **Constraint**: The installed SDKs are previews. If a non-preview toolchain is wanted, the
  smallest change is installing the .NET 10 SDK side by side; this is the author's decision and
  is not done automatically.
- **Alternatives**: pin the 10.0 RC1 SDK with `global.json` (older preview, no benefit);
  `net11.0` (preview runtime).

## R2 API style

- **Decision**: ASP.NET Core minimal APIs grouped per feature (`MapGroup`), `TypedResults`,
  built-in ProblemDetails, and one SignalR hub mapped in the same host.
- **Rationale**: Six endpoints and one hub; least ceremony, thin handlers.
- **Alternatives**: MVC controllers (more boilerplate); MediatR (rejected by Principle I).

## R3 Clock and time handling

- **Decision**: `System.TimeProvider` injected everywhere "now" is needed; `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`) in tests. Instants are `DateTimeOffset` in UTC;
  the 80% calculation uses integer ticks (data-model "Evaluation").
- **Rationale**: Built into .NET; deterministic tests; no floating-point boundary errors.
- **Alternatives**: custom `IClock` (duplicates a framework type); `DateTime` with `Kind`
  (ambiguous).

## R4 Validation and error results

- **Decision**: Request bodies are checked for shape at the API boundary. The note rule
  (FR-010) lives only in Domain. Application returns a small result type (success, blocked with
  evaluation, already decided, not found, validation); Infrastructure translates
  `DbUpdateConcurrencyException` and filtered-index violations (SQL errors 2601/2627) into
  Application exceptions mapped once in the API to the contract's error table.
- **Rationale**: One definition per rule (Principle I); expected outcomes are not exceptions.
- **Alternatives**: FluentValidation (duplicates the Domain note rule).

## R5 Persistence, transactions, and concurrency

- **Decision**: EF Core + SQL Server provider; shadow `rowversion` on `Prescriptions` and
  `RefillRequests`; one `SaveChanges` per command, which EF Core wraps in a single transaction;
  filtered unique index for one Pending request per prescription; no `EnableRetryOnFailure`.
- **Rationale**: Decided by the author (constitution v3.0.1; spec FR-003, FR-013, FR-016).
  `SaveChanges` rolls back its transaction before `DbUpdateConcurrencyException` reaches the
  caller, so the conflict response is sent after rollback.
- **Alternatives**: lock hints or serializable isolation (heavier, provider-specific SQL);
  `ExecuteUpdate` decrement (moves the refill rule out of Domain).

## R6 Database initialization and seed

- **Decision**: Committed `InitialCreate` migration. `--seed` applies pending migrations and runs
  an idempotent seeder; normal startup never migrates. Seed dates come from `TimeProvider`.
- **Rationale**: One command for reviewers; dates stay meaningful for the 80% rule.
- **Alternatives**: `HasData` (freezes dates); auto-migrate on every startup.

## R7 Integration test database

- **Decision**: Run persistence, concurrency, and API integration tests against the SQL Server
  instance named by environment variable `REFILL_TEST_SQLSERVER` (a server-level connection
  string without a database name). Default when unset: the verified local default instance with
  Windows authentication, `Server=.;Trusted_Connection=True;TrustServerCertificate=True`. Each
  test class creates a uniquely named database `RefillTests_<guid>` by applying the migrations
  and drops it afterwards. If the server cannot be reached, the suite fails with a clear
  "SQL Server not available" message and is reported as not run.
- **Rationale**: Reuses the running SQL Server 2022 instance; no secrets; real rowversion,
  filtered index, and transaction semantics as the constitution requires; Docker is not
  needed because its engine is not running.
- **Alternatives**: Testcontainers (requires a running Docker engine; remains possible by
  pointing `REFILL_TEST_SQLSERVER` at a container); LocalDB (installed but stopped; usable the
  same way); EF Core InMemory or SQLite (forbidden).

## R8 Test frameworks

- **Decision**: xUnit for backend tests; `Microsoft.AspNetCore.Mvc.Testing` for API tests;
  `Microsoft.AspNetCore.SignalR.Client` for one in-process hub test. Frontend: the Angular 19
  default, Karma + Jasmine, run headless in Edge by setting `CHROME_BIN` to the Edge executable
  (verified with the scratch app).
- **Rationale**: Defaults of the installed toolchain; no extra runner to configure.
- **Alternatives**: Jest/Vitest (not the Angular 19 default); Playwright end-to-end (outside the
  timebox).

## R9 Frontend framework version and structure

- **Decision**: Angular 19.2.x, scaffolded with the installed global CLI 19.0.0 (which resolved
  19.2.25/19.2.27 in the scratch check); the exact versions are pinned by `package-lock.json`
  at scaffold time. Standalone components, signals for view state, `HttpClient` in feature API
  services, Angular Router, dev-server proxy to the API including WebSockets (`ws: true`).
- **Constraint**: Angular 19 is past its support window as of 2026-10; upgrading is a separate
  decision for the author and is not done automatically.
- **Rationale**: Installed and verified; no upgrade inside the timebox.
- **Alternatives**: newer Angular via `npx` (not installed/verified; rejected by the
  environment rule).

## R10 Styling and UI components (A9)

- **Decision**: No component library. Plain SCSS with shared variables (spacing, type scale,
  colors, focus ring) and native HTML controls with ARIA where needed.
- **Rationale**: Two screens; native elements are accessible by default.
- **Alternatives**: Angular Material (heavier); Tailwind (extra build config).

## R11 Seed medications and interaction pairs (A5)

- **Decision**: Invented medication names and four synthetic interaction pairs (data-model
  "Seed Scenarios"); descriptions read "Synthetic demo interaction (fictional, not clinical
  guidance)"; the app shell shows a persistent "Fictional demo data — not clinical guidance"
  notice.
- **Rationale**: Demonstrates deterministic matching without presenting real-drug combinations
  as guidance.
- **Alternatives**: real drug pairs with a disclaimer (rejected by the author).

## R12 Time display (A8)

- **Decision**: One shared formatter renders every instant as `d MMM yyyy, HH:mm (UTC±hh:mm)`
  in the browser's zone, with the ISO UTC value in a `title` attribute and in details. Dates of
  birth are formatted from `yyyy-MM-dd` without time-zone conversion.
- **Rationale**: Rules use UTC; clinicians read local time; the offset removes ambiguity.
- **Alternatives**: UTC only; relative times (ambiguous at the 80% boundary).

## R13 Live updates

- **Decision**: One ASP.NET Core SignalR hub (`/hubs/refill-requests`, in-process, part of the
  shared framework) broadcasting `RequestUpdated { requestId, change }` to all clients. The hub
  has no client-callable methods. Application calls an `IRequestNotifier` port after
  `SaveChanges` succeeds; the API implements it with `IHubContext`. Publishing failures are
  caught and logged with identifiers only. The Angular client uses `@microsoft/signalr` with
  automatic reconnect, reloads on `onreconnected` and on screen activation, and shows a
  "live updates unavailable" indicator when disconnected.
- **Rationale**: Meets FR-029–FR-031 with one in-process component; the port keeps SignalR out
  of Application and makes "publish only after commit" testable with a recording fake.
- **Note on "no polling"**: the application never polls the API. SignalR may fall back to its
  long-polling transport if WebSockets are unavailable; that is the library's delivery
  transport, not data polling.
- **Alternatives**: Server-Sent Events by hand (more code, no reconnect helper); per-request
  groups (needs subscribe calls; no benefit at demo scale); Redis backplane (distributed
  infrastructure, not needed for one process).

## R14 Caching

- **Decision**: No `IMemoryCache` and no distributed cache. Request status, remaining refills,
  and eligibility are read from SQL Server for every evaluation and decision (FR-028).
- **Rationale**: These values are mutable and safety-relevant; caching would need invalidation
  and could serve stale eligibility. Demo scale has no measured performance need.
- **Revisit only if**: a measured need appears for non-authoritative read data (for example the
  medication list), with an explicit invalidation strategy.

## R15 Commit-then-notify without an outbox

- **Decision**: Notify after commit, best effort, no transactional outbox.
- **Rationale**: Notifications are hints (FR-030). If the process fails between commit and
  publish, or delivery fails, the decision stands and clients recover by reconnecting,
  returning to the screen, or Refresh (FR-031). An outbox would add a table, a dispatcher, and
  retry logic for no correctness gain here.
- **Alternatives**: outbox pattern (justified only if notifications became authoritative or
  needed guaranteed delivery).

## R16 Dependency advisories found during verification

- **Finding**: Restoring EF Core SqlServer 10.0.13 in the scratch project produced NuGet warnings
  NU1902/NU1903 for transitive package `System.Security.Cryptography.Pkcs` 9.0.11 (moderate and
  high severity advisories GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx). The parent package was not
  traced.
- **Decision**: During implementation, trace the parent and add the smallest fix (typically a
  direct reference to a patched version), then confirm the warnings are gone. Not yet done.
- **Related**: the global `dotnet-ef` tool is 10.0.2 while EF Core is 10.0.13; compatibility was
  not verified. If it causes problems, the smallest change is a project-local tool manifest
  pinned to the matching version; not installed automatically.
- **Not reviewed**: `npm audit` output for the scratch Angular app.
