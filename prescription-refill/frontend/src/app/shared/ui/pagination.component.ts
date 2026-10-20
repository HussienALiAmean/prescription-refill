import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IconComponent } from './icon.component';

let nextId = 0;

/**
 * Presentational pagination for a list whose total is known exactly (research R13). The owner
 * keeps `page` within 1..pageCount and slices its own items; this component only reports intent.
 */
@Component({
  selector: 'app-pagination',
  imports: [IconComponent],
  template: `
    @if (total() > 0) {
      <nav class="pagination" aria-label="Pagination">
        <p class="range tabular" aria-live="polite">
          Showing <strong>{{ from() }}–{{ to() }}</strong> of <strong>{{ total() }}</strong>
        </p>

        <div class="size">
          <label [for]="sizeId">Rows per page</label>
          <select [id]="sizeId" [value]="pageSize()" (change)="onSize($event)">
            @for (size of pageSizes(); track size) {
              <option [value]="size" [selected]="size === pageSize()">{{ size }}</option>
            }
          </select>
        </div>

        <div class="steps">
          <button
            type="button"
            class="btn btn-sm"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
          >
            <app-icon name="chevron-left" [size]="16" />
            Previous
          </button>
          <span class="position tabular">Page {{ page() }} of {{ pageCount() }}</span>
          <button
            type="button"
            class="btn btn-sm"
            [disabled]="page() >= pageCount()"
            (click)="pageChange.emit(page() + 1)"
          >
            Next
            <app-icon name="chevron-right" [size]="16" />
          </button>
        </div>
      </nav>
    }
  `,
  styles: `
    @use 'variables' as *;
    .pagination {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: $space-3 $space-5;
      padding: $space-3 0 0;
      font-size: $font-size-sm;
      color: $color-text-muted;
    }
    .range {
      margin: 0;
      strong {
        color: $color-text;
        font-weight: $font-weight-semibold;
      }
    }
    .size {
      display: flex;
      align-items: center;
      gap: $space-2;
      label {
        margin: 0;
        font-weight: $font-weight-medium;
        color: $color-text-muted;
        white-space: nowrap;
      }
      select {
        width: auto;
        min-height: 2rem;
        padding-top: $space-1;
        padding-bottom: $space-1;
        @media (max-width: ($breakpoint-md - 1px)) {
          min-height: $target-min;
        }
      }
    }
    .steps {
      display: flex;
      align-items: center;
      gap: $space-2;
    }
    .position {
      min-width: 6.5rem;
      text-align: center;
      color: $color-text;
      font-weight: $font-weight-medium;
      white-space: nowrap;
    }
    @media (max-width: #{$breakpoint-sm - 1px}) {
      .pagination {
        justify-content: center;
      }
      .range {
        width: 100%;
        text-align: center;
      }
    }
    @media (min-width: $breakpoint-md) {
      .size {
        margin-left: auto;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationComponent {
  readonly page = input(1);
  readonly pageSize = input(10);
  readonly total = input(0);
  readonly pageSizes = input<readonly number[]>([10, 25, 50]);
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly sizeId = `page-size-${nextId++}`;
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  protected readonly from = computed(() => (this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  protected readonly to = computed(() => Math.min(this.total(), this.page() * this.pageSize()));

  protected onSize(event: Event): void {
    this.pageSizeChange.emit(Number((event.target as HTMLSelectElement).value));
  }
}
