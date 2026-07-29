import { Component, inject, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../cart/cart.service';
import { OrderService } from './order.service';
import { PlaceOrderRequest } from './order.models';
import { MoneyPipe } from '../currency/money.pipe';
import { CurrencyService } from '../currency/currency.service';

interface CheckoutForm {
  contactEmail: string;
  contactPhone: string;
  shipFirstName: string;
  shipLastName: string;
  shipAddress: string;
  shipCity: string;
  shipZip: string;
  shipCountry: string;
}

const emptyForm = (): CheckoutForm => ({
  contactEmail: '', contactPhone: '', shipFirstName: '', shipLastName: '',
  shipAddress: '', shipCity: '', shipZip: '', shipCountry: '',
});

/** Checkout: contact + shipping form and an order summary. Places an order (no payment). */
@Component({
  selector: 'app-checkout-page',
  imports: [MoneyPipe, FormsModule, RouterLink],
  templateUrl: './checkout-page.html',
  styleUrl: './checkout-page.scss',
})
export class CheckoutPage {
  protected readonly cart = inject(CartService);
  protected readonly money = inject(CurrencyService);
  private readonly orders = inject(OrderService);
  private readonly router = inject(Router);

  protected form: CheckoutForm = emptyForm();
  protected readonly placing = signal(false);
  protected readonly error = signal('');

  protected submit(): void {
    if (this.cart.isEmpty() || this.placing()) return;

    const request: PlaceOrderRequest = {
      contactEmail: this.form.contactEmail.trim(),
      contactPhone: this.form.contactPhone.trim(),
      shipFirstName: this.form.shipFirstName.trim(),
      shipLastName: this.form.shipLastName.trim(),
      shipAddress: this.form.shipAddress.trim(),
      shipCity: this.form.shipCity.trim(),
      shipZip: this.form.shipZip.trim(),
      shipCountry: this.form.shipCountry.trim(),
      // unitPrice goes out in the base currency, unconverted — the server compares it to
      // Product.Price, which is also base. The display currency never enters this payload.
      items: this.cart.lines().map(l => ({ productId: l.productId, quantity: l.qty, unitPrice: l.price })),
    };

    this.placing.set(true);
    this.error.set('');
    this.orders.place(request).subscribe({
      next: order => {
        this.cart.clear();
        this.router.navigate(['/checkout/confirmation', order.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.placing.set(false);
        this.error.set(this.describeError(err));
      },
    });
  }

  /** Surfaces the server's clear re-validation message (price/stock changed), or validation errors. */
  private describeError(err: HttpErrorResponse): string {
    const body = err.error;
    if (typeof body?.error === 'string') return body.error;
    if (body?.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, string[]>).flat();
      if (messages.length) return messages.join(' ');
    }
    return 'Could not place the order. Please try again.';
  }
}
