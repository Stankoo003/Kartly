import { Component, computed, inject, input } from '@angular/core';

import { RouterLink } from '@angular/router';
import { CartService } from '../cart/cart.service';
import { Product } from './product.models';
import { MoneyPipe } from '../currency/money.pipe';
import { CurrencyService } from '../currency/currency.service';

/**
 * Presentational product card used on the home page and the catalog listing.
 * Non-clickable this phase; the "Add to cart" button is inert (cart is a later phase).
 */
@Component({
  selector: 'app-product-card',
  imports: [MoneyPipe, RouterLink],
  template: `
    <article class="product-card">
      <a class="product-media" [routerLink]="['/products', product().slug]" [attr.aria-label]="product().name">
        @if (product().imageUrl) {
          <img [src]="product().imageUrl" [alt]="product().name" />
        } @else {
          <span class="product-media-empty">{{ product().name }}</span>
        }
        @if (product().isFeatured) { <span class="badge">Featured</span> }
      </a>
      <div class="product-body">
        @if (product().brand) { <span class="product-brand">{{ product().brand }}</span> }
        <a class="product-name" [routerLink]="['/products', product().slug]">{{ product().name }}</a>
        <div class="product-price">
          <span class="now">{{ product().price | money: money.display() }}</span>
          @if (product().discountPrice) {
            <span class="was">{{ product().discountPrice | money: money.display() }}</span>
          }
        </div>
        <span class="product-stock">{{ stockLabel() }}</span>
        <button type="button" class="btn btn-primary product-add" [disabled]="product().stockQuantity <= 0" (click)="add()">
          {{ product().stockQuantity <= 0 ? 'Out of stock' : 'Add to cart' }}
        </button>
      </div>
    </article>
  `,
  styleUrl: './product-card.scss',
})
export class ProductCard {
  protected readonly money = inject(CurrencyService);
  private readonly cart = inject(CartService);

  readonly product = input.required<Product>();

  protected add(): void {
    this.cart.add(this.product());
    this.cart.open();
  }

  protected readonly stockLabel = computed(() => {
    const q = this.product().stockQuantity;
    if (q <= 0) return 'Out of stock';
    if (q < 10) return `Only ${q} left`;
    return 'In stock';
  });
}
