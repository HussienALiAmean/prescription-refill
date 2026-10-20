# Quickstart & Validation: Prescription Refill Request Review

Run and verify the feature end to end. Setup commands live in the root
[README.md](../../README.md); this guide lists what to check. Seed scenarios:
[data-model.md](data-model.md#seed-scenarios-fr-019). API and live updates:
[contracts/http-api.md](contracts/http-api.md).

> Planned guide. The environment checks below were run on 2026-10-20 (research R0); the run and
> validation steps have not been run because no application code exists yet.

## Prerequisites (verified versions on the development machine)

| Component | Version verified | Note |
|-----------|------------------|------|
| .NET SDK | 11.0.100-rc.2 (default) building `net10.0` | preview SDK; any .NET 10-capable SDK works |
| .NET runtime | 10.0.13 | |
| SQL Server | 2022 (16.0.1000.6), local default instance, Windows authentication | Docker or LocalDB work via connection string |
| Node / npm | 22.18.0 / 10.9.3 | |
| Angular | CLI 19.0.0 global; project resolves 19.2.x | past support window; not upgraded |
| Browser for frontend tests | Microsoft Edge 155 | set `CHROME_BIN` (below) |

## Verify the environment

Run from any folder; all commands are read-only:

```powershell
dotnet --list-sdks
dotnet --list-runtimes
dotnet tool list -g                     # dotnet-ef
sqlcmd -S . -E -C -Q "SELECT @@VERSION" # local SQL Server, Windows authentication
node --version; npm --version
ng version                              # global Angular CLI
```

If `sqlcmd -S .` fails, point the connection strings at another SQL Server (README).

## Run

1. Set `ConnectionStrings:RefillDb` if the default local instance is not used (README).
2. `dotnet run --project src/PrescriptionRefill.Api -- --seed` (applies migrations and seeds).
3. `dotnet run --project src/PrescriptionRefill.Api`
4. `cd frontend; npm ci; npm start`, then open the dev-server URL shown.

## Manual validation

| # | Action | Expected |
|---|--------|----------|
| V1 | Open the queue | Six Pending requests, oldest first; list shows when it was evaluated |
| V2 | Open scenario 1, approve with a note | Approved; remaining refills 3 → 2; last filled unchanged |
| V3 | Open scenario 2 | TOO_EARLY with eligible date; Approve disabled; Deny available |
| V4 | Approve scenario 2 through the API anyway | `422 approval_blocked` with the current evaluation |
| V5 | Open scenario 3; deny with a note | NO_REFILLS_REMAINING shown; Denied; refills still 0 |
| V6 | Open scenarios 4 and 5 | Allergy / interaction warning shown before deciding; approval succeeds |
| V7 | Open scenario 1 (patient also has inactive Corvantil) | No interaction warning; Corvantil not listed as active |
| V8 | `POST /api/refill-requests` for the inactive Corvantil prescription | `400 validation_failed`, `errors.prescriptionId` |
| V9 | Look at a review screen | Evaluation time shown with "may be out of date" notice; Refresh updates the time |
| V10 | Submit an empty or whitespace note | Inline error; nothing saved |
| V11 | Open a decided request | Read-only; no decision controls; API decision → `409 request_already_decided` |
| V12 | Open the same Pending request in tabs A and B; approve in A | B reloads by itself (live update) and shows Decided, read-only, "decided elsewhere" |
| V13 | Repeat V12 with B disconnected from the network, then approve in B after reconnecting | B reloads on reconnect; if it still submits, it gets `409` and shows no success |
| V14 | Stop the API while the queue is open | Error state on next action; "live updates unavailable" indicator; no blank screen |
| V15 | Check timestamps and a date of birth | Local time with UTC offset; UTC on hover; date of birth unshifted |
| V16 | Look at any screen | "Fictional demo data — not clinical guidance" notice visible |
| V17 | Narrow to 360px; tab through the review screen | No horizontal scroll; every control reachable and labeled |

## Automated

```powershell
# Backend: unit + SQL Server integration (uses the local default instance unless set)
$env:REFILL_TEST_SQLSERVER = "Server=.;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test

# Frontend: Karma + Jasmine in headless Edge (Chrome is not installed)
$env:CHROME_BIN = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
cd frontend; npm test -- --watch=false --browsers=ChromeHeadless
```

Report results from actual runs only. If SQL Server is unavailable, report the integration
suite as not run.
