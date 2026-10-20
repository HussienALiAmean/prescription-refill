import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DOCUMENT } from '@angular/common';
import { fromEvent, filter, merge, map } from 'rxjs';
import { RefillEventsService, RequestUpdatedEvent } from './refill-events.service';

/**
 * Reconciles a page with the API whenever its data may be stale (FR-030): a matching live hint,
 * a re-established connection, or the tab becoming visible again. The page reloads from the
 * API; hint payloads are never used as state. Must be called in an injection context.
 */
export function reloadOnChanges(reload: () => void, matches: (event: RequestUpdatedEvent) => boolean = () => true): void {
  const events = inject(RefillEventsService);
  const document = inject(DOCUMENT);

  merge(
    events.requestUpdated$.pipe(filter(matches)),
    events.reconnected$,
    fromEvent(document, 'visibilitychange').pipe(filter(() => document.visibilityState === 'visible')),
  )
    .pipe(
      map(() => undefined),
      takeUntilDestroyed(inject(DestroyRef)),
    )
    .subscribe(() => reload());
}
