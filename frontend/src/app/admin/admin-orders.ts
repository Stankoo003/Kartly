import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { OrderService } from '../orders/order.service';
import { Order, OrderStatus, OrderSummary } from '../orders/order.models';
import { UiButton } from '../ui/ui-button';
import { UiDialog } from '../ui/ui-dialog';
import { UiPager } from '../ui/ui-pager';
import { MoneyPipe } from '../currency/money.pipe';
import { CurrencyService } from '../currency/currency.service';

const STATUSES: readonly OrderStatus[] = ['Pending', 'Confirmed', 'Shipped', 'Cancelled'];

/** Admin orders: paginated list, detail dialog, and lifecycle advance/cancel. */
@Component({
  selector: 'app-admin-orders',
  imports: [MoneyPipe, DatePipe, UiButton, UiDialog, UiPager],
  templateUrl: './admin-orders.html',
  styleUrl: './admin-orders.scss',
})
export class AdminOrders {
  private readonly api = inject(OrderService);
  protected readonly money = inject(CurrencyService);

  protected readonly statuses = STATUSES;

  protected readonly orders = signal<OrderSummary[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly statusFilter = signal<OrderStatus | ''>('');

  protected readonly loading = signal(false);
  protected readonly error = signal('');

  protected readonly detail = signal<Order | null>(null);
  protected readonly dialogOpen = signal(false);
  protected readonly saving = signal(false);
  protected readonly detailError = signal('');

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  /** The next forward status (Pending→Confirmed→Shipped), or null when terminal. */
  protected readonly advanceTarget = computed<OrderStatus | null>(() => {
    switch (this.detail()?.status) {
      case 'Pending': return 'Confirmed';
      case 'Confirmed': return 'Shipped';
      default: return null;
    }
  });
  protected readonly canCancel = computed(() => {
    const s = this.detail()?.status;
    return s === 'Pending' || s === 'Confirmed';
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.list({ status: this.statusFilter(), page: this.page(), pageSize: this.pageSize }).subscribe({
      next: r => {
        this.orders.set(r.items);
        this.total.set(r.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load orders. Is the API running and are you signed in as Admin?');
        this.loading.set(false);
      },
    });
  }

  /** `''` is the All tab. Typed now that the value comes from the tabs rather than a select. */
  protected setStatusFilter(value: OrderStatus | ''): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.page.set(page);
    this.load();
  }

  protected openDetail(o: OrderSummary): void {
    this.detailError.set('');
    this.detail.set(null);
    this.dialogOpen.set(true);
    this.api.getAdmin(o.id).subscribe({
      next: order => this.detail.set(order),
      error: () => this.detailError.set('Could not load the order.'),
    });
  }

  protected closeDialog(): void {
    if (this.saving()) return;
    this.dialogOpen.set(false);
  }

  protected advance(): void {
    const target = this.advanceTarget();
    if (target) this.changeStatus(target);
  }

  protected cancel(): void {
    this.changeStatus('Cancelled');
  }

  private changeStatus(status: OrderStatus): void {
    const order = this.detail();
    if (!order) return;
    this.saving.set(true);
    this.detailError.set('');
    this.api.setStatus(order.id, status).subscribe({
      next: updated => {
        this.detail.set(updated);
        this.saving.set(false);
        this.load(); // refresh the list's status column
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.detailError.set(this.describeError(err));
      },
    });
  }

  protected shortId(id: string): string {
    return id.slice(0, 8);
  }

  private describeError(err: HttpErrorResponse): string {
    const body = err.error;
    if (typeof body?.error === 'string') return body.error;
    if (typeof body?.title === 'string') return body.title;
    return 'Something went wrong. Please try again.';
  }
}
