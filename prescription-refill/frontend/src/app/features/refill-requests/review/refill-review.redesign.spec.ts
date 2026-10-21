import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';
import { provideRouter } from '@angular/router';
import { FakeRefillEvents, provideFakeRefillEvents } from '../../../core/realtime/testing/fake-refill-events';
import { RefillRequestDetail } from '../data/models';
import { RefillRequestsApi } from '../data/refill-requests.api';
import { blockedEvaluation, detail, evaluation } from '../testing/fixtures';
import { DecisionFormComponent } from './decision-form.component';
import { RefillReviewPage } from './refill-review.page';
import { provideI18nTesting } from '../../../core/i18n/testing';

/** True when `a` comes before `b` in document order. */
const precedes = (a: Element | null, b: Element | null) =>
  !!a && !!b && !!(a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING);

describe('RefillReviewPage (redesign: safety-first layout and feedback)', () => {
  let api: jasmine.SpyObj<RefillRequestsApi>;
  let fixture: ComponentFixture<RefillReviewPage>;
  let el: HTMLElement;

  function render(response: ReturnType<RefillRequestsApi['get']>) {
    api.get.and.returnValue(response);
    fixture = TestBed.createComponent(RefillReviewPage);
    fixture.componentRef.setInput('id', 5);
    fixture.detectChanges();
    el = fixture.nativeElement;
  }

  beforeEach(() => {
    api = jasmine.createSpyObj<RefillRequestsApi>('RefillRequestsApi', ['list', 'get', 'approve', 'deny']);
    TestBed.configureTestingModule({
      imports: [RefillReviewPage],
      providers: [...provideI18nTesting(), provideRouter([]), { provide: RefillRequestsApi, useValue: api }, provideFakeRefillEvents(new FakeRefillEvents())],
    });
  });

  it('puts the safety checks and decision before the patient details in reading order', () => {
    render(of(detail()));

    expect(precedes(el.querySelector('app-evaluation-panel'), el.querySelector('app-patient-panel'))).toBeTrue();
    expect(precedes(el.querySelector('app-decision-form'), el.querySelector('app-patient-panel'))).toBeTrue();
  });

  it('shows blockers before warnings, each with its own label', () => {
    const both = blockedEvaluation();
    both.warnings = detail().evaluation!.warnings;
    render(of(detail({ evaluation: both })));

    const danger = el.querySelector('.alert-danger');
    const warning = el.querySelector('.alert-warning');
    expect(danger?.textContent).toContain('Approval blocked');
    expect(danger?.textContent).toContain('Too early');
    expect(warning?.textContent).toContain('do not block approval');
    expect(warning?.textContent).toContain('Drug interaction');
    expect(precedes(danger, warning)).toBeTrue();
  });

  it('shows a compact all-clear when nothing applies', () => {
    render(of(detail({ evaluation: evaluation() })));

    expect(el.querySelector('.alert-danger')).toBeNull();
    expect(el.querySelector('.alert-warning')).toBeNull();
    expect(el.querySelector('.alert-success')?.textContent).toContain('No blocking rules apply');
  });

  // 004 US1: each safety state is named in text, not by color alone, and nothing reads as "safe".
  it('labels blockers as preventing approval', () => {
    render(of(detail({ evaluation: blockedEvaluation() })));

    const danger = el.querySelector('.alert-danger');
    expect(danger?.textContent).toContain('Approval blocked');
    expect(danger?.textContent).toContain('Prevents approval');
  });

  it('asks for clinical review on warnings without hiding that they do not block', () => {
    render(of(detail()));

    const warning = el.querySelector('.alert-warning');
    expect(warning?.textContent).toContain('clinical review required');
    expect(warning?.textContent).toContain('do not block approval');
  });

  it('words the no-issues state neutrally', () => {
    render(of(detail({ evaluation: evaluation() })));

    const clear = el.querySelector('.alert-success');
    expect(clear?.textContent).toContain('No blocking rules apply');
    expect(clear?.textContent).toContain('Review the details before deciding');
    expect(el.querySelector('section.safety')?.classList).toContain('is-clear');
  });

  it('labels the safety state in the panel header as text', () => {
    const headerBadge = () => el.querySelector('section.safety .panel-header .badge')?.textContent?.trim();

    render(of(detail({ evaluation: blockedEvaluation() })));
    expect(headerBadge()).toBe('Approval blocked');

    render(of(detail()));
    expect(headerBadge()).toBe('Warnings');

    render(of(detail({ evaluation: evaluation() })));
    expect(headerBadge()).toBe('No issues found');
  });

  it('renders a skeleton with a loading status until the request arrives', () => {
    const pending = new Subject<RefillRequestDetail>();
    render(pending);

    expect(el.querySelector('[role="status"]')?.textContent).toContain('Loading request');
    expect(el.querySelector('.skeleton')).not.toBeNull();

    pending.next(detail());
    fixture.detectChanges();

    expect(el.querySelector('.skeleton')).toBeNull();
  });

  it('shows allergies as separate items', () => {
    const base = detail();
    render(of(detail({ patient: { ...base.patient, allergies: ['Avelorin', 'Brimexa'] } })));

    const chips = Array.from(el.querySelectorAll('.chip-allergy')).map((c) => c.textContent!.trim());
    expect(chips).toEqual(['Avelorin', 'Brimexa']);
  });

  it('shows progress only on the pressed decision button', () => {
    render(of(detail()));
    api.deny.and.returnValue(new Subject<RefillRequestDetail>());

    (el.querySelector('button.btn-danger') as HTMLButtonElement).click();
    fixture.detectChanges();

    const approve = el.querySelector('button.btn-primary') as HTMLButtonElement;
    const deny = el.querySelector('button.btn-danger') as HTMLButtonElement;
    expect(deny.textContent).toContain('Denying…');
    expect(approve.textContent).not.toContain('Approving…');
    expect(approve.disabled).toBeTrue();
    expect(deny.disabled).toBeTrue();
  });
});

describe('DecisionFormComponent (redesign: counter)', () => {
  it('shows the note length against the limit without blocking submission', () => {
    const fixture = TestBed.createComponent(DecisionFormComponent);
    fixture.componentRef.setInput('note', 'x'.repeat(1001));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    let approved = false;
    fixture.componentInstance.approve.subscribe(() => (approved = true));

    expect(el.querySelector('.counter')?.textContent).toContain('1001 / 1000');
    expect(el.querySelector('.counter')?.classList).toContain('is-over');

    (el.querySelector('button.btn-primary') as HTMLButtonElement).click();
    expect(approved).toBeTrue();
  });
});
