import { Component, inject } from '@angular/core';

import { RouterLink } from '@angular/router';
import { CartService } from './cart.service';
import { MoneyPipe } from '../currency/money.pipe';
import { CurrencyService } from '../currency/currency.service';

/** Slide-in mini cart: count, line items with qty steppers + remove, subtotal. */
@Component({
  selector: 'app-cart-drawer',
  imports: [MoneyPipe, RouterLink],
  templateUrl: './cart-drawer.html',
  styleUrl: './cart-drawer.scss',
})
export class CartDrawer {
  protected readonly cart = inject(CartService);
  protected readonly money = inject(CurrencyService);
}
