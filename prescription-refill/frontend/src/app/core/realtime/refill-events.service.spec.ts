import { TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { HUB_CONNECTION_FACTORY, HubConnectionLike, RECONNECT_RETRY_MS, RefillEventsService, RequestUpdatedEvent } from './refill-events.service';
import { provideI18nTesting } from '../i18n/testing';

class FakeConnection implements HubConnectionLike {
  readonly handlers = new Map<string, (payload: unknown) => void>();
  reconnecting?: () => void;
  reconnected?: () => void;
  closed?: () => void;
  start = jasmine.createSpy('start').and.returnValue(Promise.resolve());

  on(event: string, handler: (payload: unknown) => void): void {
    this.handlers.set(event, handler);
  }
  onreconnecting(callback: () => void): void {
    this.reconnecting = callback;
  }
  onreconnected(callback: () => void): void {
    this.reconnected = callback;
  }
  onclose(callback: () => void): void {
    this.closed = callback;
  }
}

describe('RefillEventsService', () => {
  let connection: FakeConnection;

  function create(): RefillEventsService {
    TestBed.configureTestingModule({ providers: [...provideI18nTesting(), { provide: HUB_CONNECTION_FACTORY, useValue: () => connection }] });
    return TestBed.inject(RefillEventsService);
  }

  beforeEach(() => (connection = new FakeConnection()));

  it('connects on creation and reports connected', fakeAsync(() => {
    const service = create();
    expect(service.connectionState()).toBe('connecting');

    flushMicrotasks();

    expect(connection.start).toHaveBeenCalledTimes(1);
    expect(service.connectionState()).toBe('connected');
  }));

  it('forwards RequestUpdated hints and ignores malformed payloads', fakeAsync(() => {
    const service = create();
    const received: RequestUpdatedEvent[] = [];
    service.requestUpdated$.subscribe((event) => received.push(event));
    flushMicrotasks();

    connection.handlers.get('RequestUpdated')!({ requestId: 6, change: 'decided' });
    connection.handlers.get('RequestUpdated')!({ requestId: 'x' });

    expect(received).toEqual([{ requestId: 6, change: 'decided' }]);
  }));

  it('reports reconnecting, then connected, and signals that a reload is needed', fakeAsync(() => {
    const service = create();
    let reconnects = 0;
    service.reconnected$.subscribe(() => reconnects++);
    flushMicrotasks();

    connection.reconnecting!();
    expect(service.connectionState()).toBe('reconnecting');

    connection.reconnected!();
    expect(service.connectionState()).toBe('connected');
    expect(reconnects).toBe(1);
  }));

  it('marks itself disconnected when it cannot start and retries later, then signals a reload', fakeAsync(() => {
    connection.start.and.returnValues(Promise.reject(new Error('offline')), Promise.resolve());
    const service = create();
    let reconnects = 0;
    service.reconnected$.subscribe(() => reconnects++);

    flushMicrotasks();
    expect(service.connectionState()).toBe('disconnected');

    tick(RECONNECT_RETRY_MS);
    flushMicrotasks();

    expect(connection.start).toHaveBeenCalledTimes(2);
    expect(service.connectionState()).toBe('connected');
    expect(reconnects).toBe(1);
  }));

  it('marks itself disconnected when the connection closes and retries', fakeAsync(() => {
    const service = create();
    flushMicrotasks();

    connection.closed!();
    expect(service.connectionState()).toBe('disconnected');

    tick(RECONNECT_RETRY_MS);
    flushMicrotasks();
    expect(connection.start).toHaveBeenCalledTimes(2);
    expect(service.connectionState()).toBe('connected');
  }));
});
