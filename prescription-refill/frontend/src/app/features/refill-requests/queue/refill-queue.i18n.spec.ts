import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { provideI18nTesting } from '../../../core/i18n/testing';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';
import { RefillRequestList } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { list, pagedList } from '../testing/fixtures';
import { RefillQueuePage } from './refill-queue.page';

/** The queue in Arabic and across a language switch (spec 005 US1, US2, FR-012). */
describe('RefillQueuePage language', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;

  function render() {
    const fixture = TestBed.createComponent(RefillQueuePage);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    TestBed.configureTestingModule({
      imports: [RefillQueuePage],
      providers: [
        ...provideI18nTesting('ar'),
        provideRouter([]),
        { provide: RefillRequestsApi, useValue: api },
        provideFakeRefillEvents(new FakeRefillEvents()),
      ],
    });
  });

  it('renders the queue in Arabic with translated status labels and isolated names', () => {
    api.list.and.returnValue(of(pagedList({ statusTotal: 12, matchingCount: 2 })));

    const { el } = render();

    expect(el.querySelector('h1')?.textContent).toContain('طلبات إعادة الصرف قيد الانتظار');
    expect(el.querySelector('#queue-results-heading')?.textContent?.trim()).toBe('12 طلبًا');
    expect(el.textContent).toContain('الموافقة محظورة');
    expect(el.querySelector('.queue-item .patient')?.getAttribute('dir')).toBe('auto');
    expect(el.querySelector('.queue-item .medication bdi')?.textContent).toBe('Avelorin');
    expect(el.querySelector('input[type="search"]')?.getAttribute('placeholder')).toBe('ابحث باسم المريض أو الدواء');
  });

  it('reloads once on a language switch and keeps status, search, page, and page size', fakeAsync(() => {
    api.list.and.returnValue(of(pagedList({ statusTotal: 60, matchingCount: 60 })));
    const { fixture, el } = render();

    const input = el.querySelector('input[type="search"]') as HTMLInputElement;
    input.value = 'Taylor';
    input.dispatchEvent(new Event('input'));
    tick(300);
    fixture.detectChanges();
    (Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('التالي')) as HTMLButtonElement).click();
    fixture.detectChanges();
    api.list.calls.reset();

    TestBed.inject(I18nService).setLanguage('en');
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalledTimes(1);
    expect(api.list).toHaveBeenCalledWith('Pending', { page: 2, pageSize: 10, search: 'Taylor' });
    expect(el.querySelector('h1')?.textContent).toContain('Pending refill requests');
    expect((el.querySelector('input[type="search"]') as HTMLInputElement).value).toBe('Taylor');
  }));

  it('shows only the latest response when a switch happens during a load (003 FR-016)', () => {
    const arabic = new Subject<RefillRequestList>();
    const english = new Subject<RefillRequestList>();
    api.list.and.returnValues(arabic, english);
    const { fixture, el } = render();

    TestBed.inject(I18nService).setLanguage('en');
    english.next(list({ items: [{ ...list().items[0], medicationName: 'Avelorin' }] }));
    arabic.next(list({ items: [{ ...list().items[0], medicationName: 'أفيلورين' }] }));
    fixture.detectChanges();

    expect(el.querySelector('.queue-item .medication')?.textContent).toContain('Avelorin');
    expect(el.textContent).not.toContain('أفيلورين');
  });
});
