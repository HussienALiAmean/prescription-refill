import { Injectable, InjectionToken, OnDestroy, inject, signal } from '@angular/core';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

/** Minimal hint from the server (FR-029): which request changed and how. Never state. */
export interface RequestUpdatedEvent {
  requestId: number;
  change: 'created' | 'decided';
}

/** The subset of `@microsoft/signalr`'s HubConnection this service uses (replaceable in tests). */
export interface HubConnectionLike {
  on(event: string, handler: (payload: unknown) => void): void;
  onreconnecting(callback: () => void): void;
  onreconnected(callback: () => void): void;
  onclose(callback: () => void): void;
  start(): Promise<void>;
}

export const HUB_PATH = '/hubs/refill-requests';

/** Delay before retrying a connection that failed to start or closed (not data polling). */
export const RECONNECT_RETRY_MS = 10_000;

export const HUB_CONNECTION_FACTORY = new InjectionToken<() => HubConnectionLike>('HUB_CONNECTION_FACTORY', {
  providedIn: 'root',
  factory: () => () =>
    new HubConnectionBuilder().withUrl(HUB_PATH).withAutomaticReconnect().configureLogging(LogLevel.Warning).build(),
});

/**
 * The only code that talks to SignalR (constitution Principle VII). It exposes hints and
 * connection state; pages react by reloading from the API, because delivery is not guaranteed
 * (FR-030, FR-031).
 */
@Injectable({ providedIn: 'root' })
export class RefillEventsService implements OnDestroy {
  private readonly updates = new Subject<RequestUpdatedEvent>();
  private readonly reconnects = new Subject<void>();
  private readonly connection = inject(HUB_CONNECTION_FACTORY)();
  private retryTimer: ReturnType<typeof setTimeout> | undefined;
  private needsReload = false;
  private destroyed = false;

  readonly requestUpdated$: Observable<RequestUpdatedEvent> = this.updates.asObservable();
  /** Emits when a connection is (re)established after a gap, so pages reconcile with the API. */
  readonly reconnected$: Observable<void> = this.reconnects.asObservable();
  readonly connectionState = signal<ConnectionState>('connecting');

  constructor() {
    this.connection.on('RequestUpdated', (payload) => {
      if (isRequestUpdated(payload)) {
        this.updates.next({ requestId: payload.requestId, change: payload.change });
      }
    });
    this.connection.onreconnecting(() => this.connectionState.set('reconnecting'));
    this.connection.onreconnected(() => {
      this.connectionState.set('connected');
      this.reconnects.next();
    });
    this.connection.onclose(() => {
      this.connectionState.set('disconnected');
      this.needsReload = true;
      this.scheduleRetry();
    });

    void this.start();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    clearTimeout(this.retryTimer);
  }

  private async start(): Promise<void> {
    try {
      await this.connection.start();
      this.connectionState.set('connected');
      if (this.needsReload) {
        this.needsReload = false;
        this.reconnects.next();
      }
    } catch {
      this.connectionState.set('disconnected');
      this.needsReload = true;
      this.scheduleRetry();
    }
  }

  private scheduleRetry(): void {
    if (this.destroyed) {
      return;
    }

    clearTimeout(this.retryTimer);
    this.retryTimer = setTimeout(() => void this.start(), RECONNECT_RETRY_MS);
  }
}

function isRequestUpdated(payload: unknown): payload is RequestUpdatedEvent {
  const value = payload as Partial<RequestUpdatedEvent> | null;
  return (
    typeof value?.requestId === 'number' && (value.change === 'created' || value.change === 'decided')
  );
}
