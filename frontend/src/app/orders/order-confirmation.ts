import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { OrderService } from './order.service';
import { Order } from './order.models';
import { MoneyPipe } from '../currency/money.pipe';
import { CurrencyService } from '../currency/currency.service';

type Status = 'loading' | 'loaded' | 'not-found';

/** Read-only order confirmation (/checkout/confirmation/:id). */
@Component({
  selector: 'app-order-confirmation',
  imports: [MoneyPipe, RouterLink],
  templateUrl: './order-confirmation.html',
  styleUrl: './order-confirmation.scss',
})
export class OrderConfirmation {
  private readonly orders = inject(OrderService);
  protected readonly money = inject(CurrencyService);

  private readonly id = toSignal(
    inject(ActivatedRoute).paramMap.pipe(map(p => p.get('id') ?? '')),
    { initialValue: '' },
  );

  protected readonly status = signal<Status>('loading');
  protected readonly order = signal<Order | null>(null);

  constructor() {
    const id = this.id();
    if (!id) { this.status.set('not-found'); return; }
    this.orders.get(id).subscribe({
      next: o => { this.order.set(o); this.status.set('loaded'); },
      error: () => this.status.set('not-found'),
    });
  }
}
