import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from './cart.service';
import { SettingsService } from '../settings/settings.service';

/** Slide-in mini cart: count, line items with qty steppers + remove, subtotal. */
@Component({
  selector: 'app-cart-drawer',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './cart-drawer.html',
  styleUrl: './cart-drawer.scss',
})
export class CartDrawer {
  protected readonly cart = inject(CartService);
  protected readonly settings = inject(SettingsService);
}
