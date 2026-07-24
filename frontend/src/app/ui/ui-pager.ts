import { Component, computed, input, output } from '@angular/core';

/**
 * Table footer used by the admin lists: "Showing x of y" on the left, numbered
 * page buttons with prev/next on the right.
 *
 * Usage:
 *   <ui-pager [page]="page()" [pageSize]="pageSize" [total]="total()" (pageChange)="goToPage($event)" />
 */
@Component({
  selector: 'ui-pager',
  template: `
    <p class="showing text-muted">Showing {{ shown() }} of {{ total() }}</p>

    <nav class="pages">
      <button type="button" class="page-btn" [disabled]="page() <= 1" (click)="go(page() - 1)" aria-label="Previous page">‹</button>
      @for (p of pages(); track p) {
        <button type="button" class="page-btn" [class.current]="p === page()" (click)="go(p)">{{ p }}</button>
      }
      <button type="button" class="page-btn" [disabled]="page() >= totalPages()" (click)="go(page() + 1)" aria-label="Next page">›</button>
    </nav>
  `,
  styles: [`
    :host {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-4);
      flex-wrap: wrap;
      margin-top: var(--space-4);
    }
    .showing { margin: 0; font-size: 13px; }
    .pages { display: flex; align-items: center; gap: var(--space-1); }
    .page-btn {
      min-width: 32px;
      height: 32px;
      padding: 0 8px;
      border: 1px solid var(--color-divider);
      border-radius: var(--radius-sm);
      background: var(--color-surface);
      color: var(--color-text);
      font: inherit;
      font-size: 13px;
      cursor: pointer;
    }
    .page-btn:hover:not(:disabled):not(.current) { border-color: var(--color-accent); }
    .page-btn:disabled { opacity: 0.4; cursor: not-allowed; }
    .page-btn.current {
      background: var(--color-accent);
      border-color: var(--color-accent);
      color: #fff;
      font-weight: 600;
    }
  `],
})
export class UiPager {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly total = input.required<number>();
  readonly pageChange = output<number>();

  /** How many rows the current page actually shows (the last page is usually short). */
  protected readonly shown = computed(() =>
    Math.max(0, Math.min(this.pageSize(), this.total() - (this.page() - 1) * this.pageSize())),
  );

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize())),
  );

  /** A sliding window of at most 5 page numbers centred on the current page. */
  protected readonly pages = computed(() => {
    const last = this.totalPages();
    const start = Math.max(1, Math.min(this.page() - 2, last - 4));
    const end = Math.min(last, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  protected go(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.pageChange.emit(page);
  }
}
