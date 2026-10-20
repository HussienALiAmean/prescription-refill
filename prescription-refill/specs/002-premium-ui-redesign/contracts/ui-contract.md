# UI Contract: Premium Clinical UI Redesign

**Feature**: [spec.md](../spec.md) | **Date**: 2026-10-20

The HTTP and live-update contract is
[001 contracts/http-api.md](../../001-refill-request-review/contracts/http-api.md) and does not
change. This file lists the DOM hooks, text, and semantics that existing tests and assistive
technology rely on. The redesign MUST keep every item below.

## Selectors and structure relied on by existing tests

| Hook | Where | Relied on by |
|------|-------|--------------|
| `[role="note"]` containing "Fictional demo data — not clinical guidance", "No sign-in", "not for real patient data" | app shell | `app.component.spec.ts` |
| `li.queue-item` per row, first `a` in row with `href="/requests/{id}"` | queue | `refill-queue.page.spec.ts` |
| Row text includes patient name, medication, "Approval blocked", "Warnings", "Decided", status label | queue | `refill-queue.page.spec.ts` |
| First `time[datetime]` on the Pending view is the list's `evaluatedAt` | queue | `refill-queue.page.spec.ts` |
| `h1` text = "Pending refill requests" / "Approved requests" / "Denied requests" | queue | `refill-queue.page.spec.ts` |
| `.empty-state` containing "No pending requests" / "No denied requests" | queue | `refill-queue.page.spec.ts` |
| Text "Loading" while loading (queue and review) | both | queue + review specs |
| `[role="alert"]` containing "could not be loaded"; `app-error-state button` retries | both | queue + review specs |
| ~~Native `select` with `label[for]` "Show"~~ → `fieldset` with `legend` "Show" and radio inputs valued Pending/Approved/Denied (intentional change, research R7; spec rewritten with equivalent assertions) | queue | `status-filter.component.spec.ts` |
| Rows rendered as `li.queue-item` on the current page only; `nav[aria-label="Pagination"]` with "Showing x–y of N", "Page p of P", Previous/Next buttons, labeled page-size select | queue | new pagination specs |
| `button.btn-primary` = Approve, `button.btn-danger` = Deny | review | decision-form + review specs |
| A `button` whose text includes "Refresh" | review | `refill-review.page.spec.ts` |
| `textarea` with `label[for]` "Decision note"; `.field-error`; `aria-describedby` includes `decision-note-error` on error | review | decision-form + review specs |
| Text "You can still deny", "may be out of date", "read-only", "decided elsewhere", "already been decided", "changed while your decision was being saved", "Request approved", "not found" | review | `refill-review.page.spec.ts` |
| `time[datetime="{evaluation.evaluatedAt}"]` | review | `refill-review.page.spec.ts` |
| `.alert` with `role="alert"` for danger, `role="status"` otherwise; `.badge`; `.empty-state` | shared | `shared-ui.spec.ts` |
| Connection indicator renders no text while connected; `[role="status"]` with "Live updates unavailable" and "Refresh"; "Reconnecting" | shell | `connection-indicator.component.spec.ts` |

## Semantics the redesign keeps or adds

- Landmarks: `header`, `main`; one `h1` per page; `h2` per panel.
- Live regions: review notice region `aria-live="polite"`; danger alerts `role="alert"`.
- New icons are `aria-hidden="true"`; meaning is always also in text.
- New search field: `<label for>` "Search patient or medication", `type="search"`; result
  count announced via `role="status"`.
- Focus: `:focus-visible` uses `outline` (visible in forced-colors mode).

## Shared component API (after redesign)

| Component | Inputs / slots | Change |
|-----------|----------------|--------|
| `app-alert` | `kind`, `heading`, content | + kind icon; styles only |
| `app-status-badge` | `label`, `tone` (+ `info`, `accent`) | + optional leading dot/icon |
| `app-empty-state` | `message`, + `heading?`, `icon?`, projected action | additive |
| `app-error-state` | unchanged | restyle |
| `app-icon` (new) | `name`, `size?` | new |
| `app-connection-indicator` | unchanged | pill styling |
