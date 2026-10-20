import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination.component';

describe('PaginationComponent', () => {
  let fixture: ComponentFixture<PaginationComponent>;
  let el: HTMLElement;

  function render(page: number, total: number, pageSize = 10) {
    fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('total', total);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.detectChanges();
    el = fixture.nativeElement;
  }

  const button = (label: string) =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes(label)) as HTMLButtonElement;
  const text = () => el.textContent?.replace(/\s+/g, ' ') ?? '';

  it('renders nothing when there are no records', () => {
    render(1, 0);

    expect(el.querySelector('nav')).toBeNull();
  });

  it('shows a single page for one record with both steps disabled', () => {
    render(1, 1);

    expect(el.querySelector('nav')?.getAttribute('aria-label')).toBe('Pagination');
    expect(text()).toContain('Showing 1–1 of 1');
    expect(text()).toContain('Page 1 of 1');
    expect(button('Previous').disabled).toBeTrue();
    expect(button('Next').disabled).toBeTrue();
  });

  it('treats exactly one full page as a single page', () => {
    render(1, 10);

    expect(text()).toContain('Showing 1–10 of 10');
    expect(text()).toContain('Page 1 of 1');
    expect(button('Next').disabled).toBeTrue();
  });

  it('shows the partial last page with correct range and boundaries', () => {
    render(2, 11);

    expect(text()).toContain('Showing 11–11 of 11');
    expect(text()).toContain('Page 2 of 2');
    expect(button('Previous').disabled).toBeFalse();
    expect(button('Next').disabled).toBeTrue();
  });

  it('enables both steps on a middle page and emits the target page', () => {
    render(2, 26);
    const pages: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => pages.push(p));

    expect(text()).toContain('Showing 11–20 of 26');
    expect(text()).toContain('Page 2 of 3');

    button('Previous').click();
    button('Next').click();

    expect(pages).toEqual([1, 3]);
  });

  it('offers 10, 25, and 50 rows per page through a labeled select and emits changes', () => {
    render(1, 26);
    const select = el.querySelector('select') as HTMLSelectElement;
    let size: number | undefined;
    fixture.componentInstance.pageSizeChange.subscribe((s) => (size = s));

    expect(el.querySelector(`label[for="${select.id}"]`)?.textContent).toContain('Rows per page');
    expect(Array.from(select.options).map((o) => o.value)).toEqual(['10', '25', '50']);

    select.value = '25';
    select.dispatchEvent(new Event('change'));

    expect(size).toBe(25);
  });
});
