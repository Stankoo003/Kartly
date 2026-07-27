import { Component, inject } from '@angular/core';

import { RouterLink } from '@angular/router';
import { CartService } from './cart.service';
import { MoneyPipe } from '../currency/money.pipe';
import { CurrencyService } from '../currency/currency.service';

/** Full cart page (/cart): line management + a summary sidebar. Checkout is a later phase. */
@Component({
  selector: 'app-cart-page',
  imports: [MoneyPipe, RouterLink],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.scss',
})
export class CartPage {
  protected readonly cart = inject(CartService);
  protected readonly money = inject(CurrencyService);
}
