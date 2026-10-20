import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { ConnectionState, RefillEventsService, RequestUpdatedEvent } from '../refill-events.service';

/** Test double: lets specs push hints and reconnects without a real SignalR connection. */
export class FakeRefillEvents {
  readonly updates = new Subject<RequestUpdatedEvent>();
  readonly reconnects = new Subject<void>();
  readonly requestUpdated$ = this.updates.asObservable();
  readonly reconnected$ = this.reconnects.asObservable();
  readonly connectionState = signal<ConnectionState>('connected');
}

export const provideFakeRefillEvents = (fake = new FakeRefillEvents()) => ({
  provide: RefillEventsService,
  useValue: fake,
});
