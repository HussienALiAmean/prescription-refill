import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DecisionFormComponent } from './decision-form.component';

describe('DecisionFormComponent', () => {
  let fixture: ComponentFixture<DecisionFormComponent>;
  let el: HTMLElement;

  const approveButton = () => el.querySelector('button.btn-primary') as HTMLButtonElement;
  const denyButton = () => el.querySelector('button.btn-danger') as HTMLButtonElement;

  function render(inputs: { canApprove?: boolean; submitting?: boolean; errors?: string[] } = {}) {
    fixture = TestBed.createComponent(DecisionFormComponent);
    fixture.componentRef.setInput('canApprove', inputs.canApprove ?? true);
    fixture.componentRef.setInput('submitting', inputs.submitting ?? false);
    fixture.componentRef.setInput('errors', inputs.errors ?? []);
    fixture.detectChanges();
    el = fixture.nativeElement;
  }

  it('disables Approve but keeps Deny available when approval is blocked', () => {
    render({ canApprove: false });

    expect(approveButton().disabled).toBeTrue();
    expect(denyButton().disabled).toBeFalse();
    expect(el.textContent).toContain('You can still deny');
  });

  it('disables both while a decision is being submitted', () => {
    render({ submitting: true });

    expect(approveButton().disabled).toBeTrue();
    expect(denyButton().disabled).toBeTrue();
  });

  it('does not apply its own note rule: an empty note can still be submitted to the server', () => {
    render();
    let denied = false;
    fixture.componentInstance.deny.subscribe(() => (denied = true));

    denyButton().click();

    expect(denied).toBeTrue();
  });

  it('emits approve and keeps the typed note', () => {
    render();
    let approved = false;
    fixture.componentInstance.approve.subscribe(() => (approved = true));
    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;

    textarea.value = '  typed note ';
    textarea.dispatchEvent(new Event('input'));
    approveButton().click();

    expect(approved).toBeTrue();
    expect(fixture.componentInstance.note()).toBe('  typed note ');
  });

  it('shows server field errors inline and marks the field invalid', () => {
    render({ errors: ['A decision note is required.'] });

    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    expect(textarea.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector('.field-error')?.textContent).toContain('A decision note is required.');
    expect(textarea.getAttribute('aria-describedby')).toContain('decision-note-error');
  });

  it('labels the note field', () => {
    render();

    const textarea = el.querySelector('textarea') as HTMLTextAreaElement;
    expect(el.querySelector(`label[for="${textarea.id}"]`)?.textContent).toContain('Decision note');
  });
});
