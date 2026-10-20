# Quickstart Validation: Creative Clinical UI

Contract: [contracts/ui-contract.md](contracts/ui-contract.md). Tokens and runtime state:
[data-model.md](data-model.md). Decisions: [research.md](research.md).

## Prerequisites

As in the [README](../../README.md). Run the API **on a free port against a throwaway database**,
never the developer's running instance or the `PrescriptionRefill` database. First check the port:

```bash
powershell -NoProfile -Command "Get-NetTCPConnection -State Listen -LocalPort 5180 -ErrorAction SilentlyContinue"   # must print nothing
export ConnectionStrings__RefillDb="Server=.;Database=RefillUi_Check;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src/PrescriptionRefill.Api -- --seed
dotnet run --no-build --project src/PrescriptionRefill.Api --urls http://localhost:5180
```

Serve the frontend with its proxy pointed at 5180 (a temporary proxy file outside the repo), then
confirm `GET /api/refill-requests` through the proxy returns the seeded 6 Pending requests before
any visual check. Drop `RefillUi_Check` afterwards.

## Automated

```bash
cd frontend
npm run test:ci      # all existing specs + new directive/review specs pass
npm run build        # production build OK, within budget
```

## Visual checks (real browser, DevTools-protocol script outside the repo; research R10)

| # | Check | Expected |
|---|-------|----------|
| V1 | Queue at 360 / 768 / 1280 px, Pending, Approved, Denied, no-match search, empty, error (API stopped), loading | Distinct states; no horizontal overflow; rows readable |
| V2 | Review for seed scenarios 1–6 and a decided request at 360 / 1280 px | Safety state named by text + icon; blockers before warnings; nothing sticky below 1024 px |
| V3 | Grayscale screenshots of V2 | Each safety state identifiable without color (SC-003) |
| V4 | 200% zoom on a 1280 px window, review scenario 6 | Blocker, warning, note, buttons visible; no horizontal overflow |
| V5 | Move the pointer across the canvas, then over queue rows; then scroll the review page by 600 px and move the pointer again | `--pointer-x/y` equal the dispatched viewport coordinates before and after scrolling; the row's `--glow-x/y` equal the pointer position minus the row's bounding-rect origin; light visibly centered under the pointer in screenshots |
| V6 | Pointer still for 5 s | Custom properties unchanged; no running animations (`document.getAnimations()` empty) |
| V7 | Emulate `prefers-reduced-motion: reduce`, then `hover: none` / touch | No `data-ambient` attribute; light fixed; layout identical |
| V8 | Keyboard only: Tab through queue and review | Visible focus on every control; row focus outline + bar |
| V9 | Forced colors (high contrast) | Decorative light hidden; focus, badges, safety sections distinguishable |
| V10 | (a) Click, scroll, select text over glowing rows and the canvas. (b) Performance, ambient mode on (`data-ambient="on"` confirmed): type 200 characters into the review note field while moving the pointer every 10 characters, then scroll the Pending queue top to bottom while moving the pointer over rows; record `longtask` entries with `PerformanceObserver`. (c) Repeat (b) with ambient mode off (emulated `prefers-reduced-motion: reduce`) as the comparison run | (a) All behave normally. (b) Every long task > 50 ms listed with start time, duration, and `attribution`; the run counts as passing only if no long task is attributable to pointer-effect handling, i.e. none in (b) beyond those also present in (c) at the same step. If `PerformanceObserver.supportedEntryTypes` lacks `longtask`, record "not measured: longtask unsupported" (a limitation, **not** a pass) |
| V11 | Rows visible at 1280 × 800 before vs. after | After ≥ before (SC-007) |
| V12 | Target sizes at 360 px and 767 px (the contract's rule applies below 768 px): queue (search input, status segments, Clear search, row links, pagination buttons and size select, Refresh) and review (back link, evaluation Refresh, note field, Approve, Deny, retry) | Every interactive element's bounding box ≥ 44 × 44 px; failures listed by selector (spec FR-028) |

For V3, record per review scenario: state label visible (yes/no), icon visible (yes/no), pass/fail
(spec SC-003).

## Clean-copy verification (constitution Delivery Constraints)

Follows the README "Database setup" and "Run" sections in a fresh copy. The README's run flow uses
fixed ports (API 5080 from `launchSettings.json`, `ng serve` 4200, `proxy.conf.json` → 5080), so:

1. Check ports: `Get-NetTCPConnection -State Listen -LocalPort 5080,4200`. If both are free, use the
   README commands unchanged. If either is in use (for example a developer's running instance), do
   **not** stop it; use the port-override variant below and record it as a deviation.
2. Copy the repository excluding `bin`, `obj`, `node_modules`, `dist`, `.angular`, `.vs`, and
   `TestResults` into a scratch folder.
3. Throwaway database only:
   `ConnectionStrings__RefillDb="Server=.;Database=RefillUi_Clean_<timestamp>;Trusted_Connection=True;TrustServerCertificate=True"`
   (the README documents this variable). Never the `PrescriptionRefill` database.
4. `dotnet run --project src/PrescriptionRefill.Api -- --seed` (README step 2). Expected:
   "Demo data seeded."
5. `dotnet run --project src/PrescriptionRefill.Api` (README "Run", unchanged), or with the
   override `dotnet run --project src/PrescriptionRefill.Api --urls http://localhost:5180`.
6. `cd frontend`, then `npm ci`, `npm run test:ci`, `npm start` (README "Run" and "Tests"), or with
   the override `npm start -- --port <free port> --proxy-config <temp proxy file outside the repo
   targeting 5180>`.
7. Before any request that writes, confirm the responding API is the clean one:
   `GET /api/refill-requests` returns 6 Pending, and `?status=Approved` and `?status=Denied` return
   1 each. Then, through the dev proxy, the app shell returns 200, `/api/refill-requests` 200, and
   `/hubs/refill-requests/negotiate` 200.
8. Record prerequisites (SDK, Node, SQL Server versions), every command, results, and deviations in
   "Results"; stop the servers you started; drop only the throwaway database.

Record which checks were run and their results in `SPEC.md` "Verification"; list any not run.

## Baseline (T001, 2026-10-20, before any 004 code change)

| Command | Result |
|---------|--------|
| `npm run test:ci` | 98/98 |
| `npm run build` | OK, initial 415.99 kB |
| Before screenshots (T003) | 16 captured in `screenshots/before/` (queue × 3 statuses, review × 5 states, at 360 and 1280 px); horizontal overflow 0 on all |
| Rows per 1280 × 800 view (T003, SC-007) | first row top 403 px, row pitch 80 px → 4 rows fit (seed has 6 Pending) |

## Results (T033–T035, 2026-10-20; all from actual runs)

### Automated (repository)

| Command | Result |
|---------|--------|
| `npm run test:ci` | 117/117 (baseline 98; +19 new: 11 directive, 2 shell, 4 review wording, 2 queue) |
| `npm run build` | OK, initial 424.01 kB (baseline 415.99 kB) |
| `dotnet build PrescriptionRefill.slnx` | 0 warnings, 0 errors |
| `dotnet test PrescriptionRefill.slnx` | Domain 47/47, integration 103/103 (backend unchanged; the 003 fixture timeouts did not recur in this run) |

### Clean-copy verification

Prerequisites: .NET SDK 11.0.100-rc.2, Node 22.18.0, npm 10.9.3, SQL Server 16.0.1000.6 (2022). Ports
5080 and 4200 were free, so the README commands ran **unchanged** (no port deviation). Copy excluded
`bin`, `obj`, `node_modules`, `dist`, `.angular`, `.vs`, `TestResults` (5.7 MB). Throwaway database
`RefillUi_Clean_1792527394`.

| README step | Result |
|-------------|--------|
| `dotnet run --project src/PrescriptionRefill.Api -- --seed` | "Demo data seeded." |
| `dotnet run --project src/PrescriptionRefill.Api` | listening on 5080; fresh seed counts 6 Pending / 1 Approved / 1 Denied confirmed before any check |
| `npm ci` | 976 packages |
| `npm run test:ci` | 117/117 |
| `npm start` | app shell 200; `/api/refill-requests` 200 and `/hubs/refill-requests/negotiate` 200 through the dev proxy |

**Deviation during the run:** someone using a browser on `localhost:4200` (now served by this clean
environment) approved request 3 (scenario 1) with a hand-typed note at 20:21:04 UTC. The checks never
approve or deny. The check script then resolved scenario ids live from the API, and recreated
scenario 1 once with the documented create endpoint (prescription 1 → request 9). The approved request
3 served as the "decided" scenario.

### Visual checks (headless Edge 155 over the DevTools protocol)

| # | Result |
|---|--------|
| V1 | Queue Pending/Approved/Denied and no-match at 360/768/1280: overflow 0 everywhere; no-match shows "No requests match “zzzz”."; loading skeleton captured with only `/api/*` held; error state (API stopped) at 360/1280: danger alert, "Try again", "Live updates unavailable — use Refresh.", overflow 0 |
| V2 | 7 review states × 360/1280: overflow 0; safety/decision before patient details in all 14; no sticky/fixed element at 360; at 1280 only the header and the decision column (allowed ≥ 1024 px) |
| V3 | Grayscale, per scenario (label seen / icon seen / pass): clear "No issues found" / yes / pass; blocked "Approval blocked" / yes / pass; no-refills "Approval blocked" / yes / pass; warning "Warnings" / yes / pass; interaction "Warnings" / yes / pass; blocked-warning "Approval blocked" + warning alert / yes / pass |
| V4 | 200% zoom (1280 window): overflow 0; blocker, warning, note, Approve, Deny all visible |
| V5 | `data-ambient="on"`; canvas: `--pointer-x/y` = 300/180 = dispatched viewport coordinates; row: pointer 313/466 matches, `--glow-x/y` 240/28 = pointer − row origin; glow opacity 1 on hover; after scrolling 600 px: 640/300 = dispatched coordinates |
| V6 | Pointer still 5 s: variables unchanged (500px/300px); running animations 0 |
| V7 | Reduced motion: no `data-ambient`, no variables written, 0 animations; touch emulation (`hover: none` true): no `data-ambient` |
| V8 | Tab order brand → Refresh → status radios → search → 6 row links → page-size select; every stop has a 2 px outline (canvas accent on the canvas, teal on the sheet; rows outlined as a whole) |
| V9 | Forced colors: `.ambient-layer` `display: none`; screenshots of queue and review captured |
| V10 | (a) wheel scroll over a glowing row scrolled 236 px; hit-test at the row returns the row link; click navigated to the request; glow `pointer-events: none`. (b) Ambient on: typing 200 characters (pointer moving every 10) → 0 long tasks; queue scroll with pointer moving → 0 long tasks. (c) Ambient off (reduced motion): 0 and 0. `longtask` supported; none attributable to the pointer effect |
| V11 | Rows per 1280 × 800: 7 fit (first row 382 px, pitch 56 px) vs. 4 before (403 px, 80 px) |
| V12 | Targets < 44 × 44 px: none at 360 or 767 px on the queue (15 controls) or review (6 controls) |
| SC-002 | Contrast audit at 1280: 0 failures across 215 text elements (queue, approved list, blocked+warning, clear, decided review) |

Notice contrast correction: the notice sits on `$canvas-surface` (#13223B), so its text is 10.7:1 and
icon 10.8:1 (T032 quoted 12.1:1 against the bare canvas); both pass AA.

### Fixes from inspection (T035)

| Found in screenshots | Fix | Re-check |
|----------------------|-----|----------|
| Queue row badge "No issues found" was bright green while the review and summary were neutral (FR-015) | `refill-queue.page.html`: badge tone `success` → `neutral` | 117/117; queue re-shot at 1280/360, badge class `badge-neutral` |
| Check-script artifacts (not app defects): touch-emulation parameter, assertions before data rendered, network throttling hiding the skeleton | Script fixed (wait for rendered content; hold only `/api/*`) and all checks re-run | Results above are from the final run |

Not run: a separate manual (human) browser session; true empty status (the seed has no empty status;
covered by component specs).
