import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { acceptLanguageInterceptor } from '../../../core/i18n/accept-language.interceptor';
import { I18nService } from '../../../core/i18n/i18n.service';
import { provideI18nTesting } from '../../../core/i18n/testing';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';
import { RefillRequestDetail } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { arabicDetail, blockedEvaluation, decided, detail, evaluation } from '../testing/fixtures';
import { RefillReviewPage } from './refill-review.page';

/** Arabic rendering of every review state (spec 005 US1, FR-013–FR-018, quickstart V8). */
describe('RefillReviewPage in Arabic', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;
  let fixture: ComponentFixture<RefillReviewPage>;
  let el: HTMLElement;

  function render(request: RefillRequestDetail) {
    api.get.and.returnValue(of(request));
    fixture = TestBed.createComponent(RefillReviewPage);
    fixture.componentRef.setInput('id', '5');
    fixture.detectChanges();
    el = fixture.nativeElement;
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    TestBed.configureTestingModule({
      imports: [RefillReviewPage],
      providers: [
        ...provideI18nTesting('ar'),
        provideRouter([]),
        { provide: RefillRequestsApi, useValue: api },
        provideFakeRefillEvents(new FakeRefillEvents()),
      ],
    });
  });

  it('shows a blocked request in Arabic with the server message and the disabled-approve reason', () => {
    render(detail({ evaluation: blockedEvaluation() }));

    const text = el.textContent!;
    expect(text).toContain('فحوصات السلامة');
    expect(text).toContain('الموافقة محظورة');
    expect(text).toContain('مبكر جدًا');
    expect(text).toContain('مؤهل اعتبارًا من');
    expect(text).toContain('الموافقة غير متاحة لأن قاعدة مانعة تنطبق');
    expect(el.querySelector<HTMLButtonElement>('button.btn-primary')!.disabled).toBeTrue();
    expect(el.querySelector<HTMLButtonElement>('button.btn-danger')!.disabled).toBeFalse();
  });

  it('labels warnings as non-blocking in Arabic', () => {
    render(detail());

    expect(el.textContent).toContain('تحذيرات — تتطلب مراجعة سريرية، ولا تمنع الموافقة.');
    expect(el.textContent).toContain('تداخل دوائي');
  });

  it('says "no issues found" and never "safe"', () => {
    render(detail({ evaluation: evaluation() }));

    expect(el.textContent).toContain('لم يُعثر على مشكلات');
    expect(el.textContent).not.toContain('آمن');
  });

  it('shows a decided request read-only in Arabic', () => {
    render(decided('Denied'));

    expect(el.textContent).toContain('مرفوض');
    expect(el.textContent).toContain('للقراءة فقط');
    expect(el.querySelector('textarea')).toBeNull();
  });

  it('keeps recorded text untranslated and isolated in its own direction (spec 005 FR-004, FR-015)', () => {
    render(decided('Approved'));

    expect(el.querySelector('.patient-name')?.getAttribute('dir')).toBe('auto');
    expect(el.querySelector('.patient-name')?.textContent?.trim()).toBe('Sam Patel');
    expect(el.querySelector('.rx-dosage')?.getAttribute('dir')).toBe('auto');
    expect(el.querySelector('.rx-dosage')?.textContent?.trim()).toBe('10 mg by mouth once daily');
    const note = el.querySelector('blockquote.note')!;
    expect(note.getAttribute('dir')).toBe('auto');
    expect(note.textContent?.trim()).toBe('Stable on current dose.');
    expect(el.querySelector('h1 bdi')?.textContent).toBe('Fenquira');
  });

  it('shows the localized names and texts exactly as the API sent them (spec 005 FR-021, FR-023)', () => {
    render(arabicDetail());

    expect(el.querySelector('h1 bdi')?.textContent).toBe('فينكيرا');
    expect(el.querySelector('.rx-name bdi')?.textContent).toBe('فينكيرا');
    expect(el.querySelector('.rx-row-name')?.textContent?.trim()).toBe('غليمورا');
    expect(el.textContent).toContain('يوجد تداخل تجريبي مُصطنع بين فينكيرا والدواء النشط غليمورا.');
    expect(el.textContent).toContain('تداخل تجريبي مُصطنع (خيالي، وليس إرشادًا سريريًا).');
    expect(el.textContent).not.toContain('Fenquira');
    expect(el.querySelector('.patient-name')?.textContent?.trim()).toBe('Sam Patel');
  });

  it('gives the note field its own direction', () => {
    render(detail());

    expect(el.querySelector('textarea')?.getAttribute('dir')).toBe('auto');
  });
});

/** A language switch on the review screen (spec 005 FR-012, US2 scenario 3, edge cases). */
describe('RefillReviewPage language switch', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;
  let fixture: ComponentFixture<RefillReviewPage>;
  let el: HTMLElement;

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    TestBed.configureTestingModule({
      imports: [RefillReviewPage],
      providers: [
        ...provideI18nTesting('ar'),
        provideRouter([]),
        { provide: RefillRequestsApi, useValue: api },
        provideFakeRefillEvents(new FakeRefillEvents()),
      ],
    });
    api.get.and.returnValue(of(detail()));
    fixture = TestBed.createComponent(RefillReviewPage);
    fixture.componentRef.setInput('id', '5');
    fixture.detectChanges();
    el = fixture.nativeElement;
  });

  function typeNote(text: string) {
    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = text;
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  it('reloads the detail in the new language and keeps the typed note', () => {
    typeNote('ملاحظة قيد الكتابة');
    expect(api.get).toHaveBeenCalledTimes(1);

    TestBed.inject(I18nService).setLanguage('en');
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledTimes(2);
    expect((el.querySelector('textarea') as HTMLTextAreaElement).value).toBe('ملاحظة قيد الكتابة');
    expect(el.textContent).toContain('Safety checks');
  });

  it('lets a decision in flight finish once, then shows its outcome in the new language', () => {
    const response = new Subject<RefillRequestDetail>();
    api.deny.and.returnValue(response);
    typeNote('Denied.');
    (el.querySelector('button.btn-danger') as HTMLButtonElement).click();

    TestBed.inject(I18nService).setLanguage('en');
    response.next(decided('Denied'));
    response.complete();
    fixture.detectChanges();

    expect(api.deny).toHaveBeenCalledTimes(1);
    expect(el.textContent).toContain('Request denied.');
  });

  it('drops server validation text from the old language', () => {
    api.deny.and.returnValue(new Subject<RefillRequestDetail>());
    const errors = fixture.componentInstance['noteErrors'];
    errors.set(['ملاحظة القرار مطلوبة.']);

    TestBed.inject(I18nService).setLanguage('en');

    expect(errors()).toEqual([]);
  });
});

/** Live updates reload in the active language (spec 005 edge case, FR-003). */
describe('RefillReviewPage live update language', () => {
  it('sends the active language on a reload triggered by a live update', () => {
    const events = new FakeRefillEvents();
    TestBed.configureTestingModule({
      imports: [RefillReviewPage],
      providers: [
        ...provideI18nTesting('ar'),
        provideRouter([]),
        provideHttpClient(withInterceptors([acceptLanguageInterceptor])),
        provideHttpClientTesting(),
        provideFakeRefillEvents(events),
      ],
    });
    const backend = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(RefillReviewPage);
    fixture.componentRef.setInput('id', '5');
    fixture.detectChanges();

    const first = backend.expectOne('/api/refill-requests/5');
    expect(first.request.headers.get('Accept-Language')).toBe('ar');
    first.flush(detail());

    TestBed.inject(I18nService).setLanguage('en');
    backend.expectOne('/api/refill-requests/5').flush(detail());

    events.updates.next({ requestId: 5, change: 'decided' });
    const fromLiveUpdate = backend.expectOne('/api/refill-requests/5');
    expect(fromLiveUpdate.request.headers.get('Accept-Language')).toBe('en');
    fromLiveUpdate.flush(decided('Approved'));
    backend.verify();
  });
});
