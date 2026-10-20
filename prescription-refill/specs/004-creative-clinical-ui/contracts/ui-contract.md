# UI Contract: Creative Clinical UI

## Preserved (unchanged hooks and texts)

Every row of the 002 contract table
([002 contracts/ui-contract.md](../../002-premium-ui-redesign/contracts/ui-contract.md#selectors-and-structure-relied-on-by-existing-tests))
stays valid, plus the hooks added by 003:

- `li.queue-item` rows with a first `a[href="/requests/{id}"]`; `.patient` element with the patient
  name; `.summary` with "approval blocked", "with warnings", "no issues found";
  `app-status-filter .count`; `.empty-state` texts including "No requests match “…”" and
  "Clear search"; `nav[aria-label="Pagination"]` with "Showing x–y of N", "Page p of P",
  Previous/Next, `app-pagination select`; `input[type="search"]`; a Refresh `button` with
  `.spinner` while reloading.
- Review: `.alert-danger` containing "Approval blocked" and the reason label; `.alert-warning`
  containing "do not block approval" and the warning label; `.alert-success` containing
  "No blocking rules apply" (all-clear); `.counter`; `app-evaluation-panel`, `app-decision-form`,
  and `app-patient-panel` in that DOM order; `button.btn-primary` (Approve), `button.btn-danger`
  (Deny).
- Shell: `[role="note"]` notice texts; connection indicator texts and roles.

## Added

| Item | Contract |
|------|----------|
| `appAmbientLight` directive | Standalone attribute directive, no inputs or outputs. Applied once to the app shell host. Behavior: research R4. Never adds listeners to individual elements. |
| `data-glow` attribute | Opt-in marker for elements that show the local pointer glow. Used on `li.queue-item` only. Purely presentational; no behavior attached. |
| `data-ambient="on"` | Set by the directive on its host when the ambient mode is active. CSS-only meaning. |
| CSS custom properties | `--pointer-x`, `--pointer-y` (shell): viewport pixels from `clientX`/`clientY`, consumed only by the `position: fixed` canvas light. `--glow-x`, `--glow-y` (glowing element): pixels relative to that element's top-left. Written by the directive only. |
| Wording | Warning heading: "Warnings — clinical review required. They do not block approval." No-issues text: "No blocking rules apply. No allergy or interaction warnings. Review the details before deciding." |

## Layering rules

1. Decorative layers (the `.ambient-layer` canvas light and the `[data-glow]::after` row glow) have
   `pointer-events: none`, sit behind or beneath content text, and are hidden in
   `forced-colors: active` and print.
2. Clinical information is only on opaque surfaces (`$color-surface` cards on the `$color-bg` sheet).
3. No fixed or sticky element on viewports narrower than 1024 px (the decorative `.ambient-layer`
   is fixed but non-interactive, behind content, and hidden in forced colors and print).
4. Interactive controls are at least `$target-min` (44 px) in both dimensions below 768 px.
