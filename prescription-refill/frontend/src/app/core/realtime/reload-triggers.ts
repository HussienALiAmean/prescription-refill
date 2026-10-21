import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DOCUMENT } from '@angular/common';
import { fromEvent, filter, merge, map } from 'rxjs';
import { I18nService } from '../i18n/i18n.service';
import { RefillEventsService, RequestUpdatedEvent } from './refill-events.service';

/**
 * Reconciles a page with the API whenever its data may be stale (FR-030): a matching live hint,
 * a re-established connection, the tab becoming visible again, or a language switch, which needs
 * the server's text in the new language (spec 005 FR-012, amending 001 FR-023). The page reloads
 * from the API; hint payloads are never used as state. Must be called in an injection context.
 */
export function reloadOnChanges(reload: () => void, matches: (event: RequestUpdatedEvent) => boolean = () => true): void {
  const events = inject(RefillEventsService);
  const document = inject(DOCUMENT);
  const i18n = inject(I18nService);

  merge(
    events.requestUpdated$.pipe(filter(matches)),
    events.reconnected$,
    fromEvent(document, 'visibilitychange').pipe(filter(() => document.visibilityState === 'visible')),
    i18n.languageChanged$,
  )
    .pipe(
      map(() => undefined),
      takeUntilDestroyed(inject(DestroyRef)),
    )
    .subscribe(() => reload());
}
