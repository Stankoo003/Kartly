import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from './cart.service';
import { SettingsService } from '../settings/settings.service';

/** Full cart page (/cart): line management + a summary sidebar. Checkout is a later phase. */
@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.scss',
})
export class CartPage {
  protected readonly cart = inject(CartService);
  protected readonly settings = inject(SettingsService);
}
