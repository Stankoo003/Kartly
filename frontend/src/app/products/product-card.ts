import { Component, computed, inject, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SettingsService } from '../settings/settings.service';
import { Product } from './product.models';

/**
 * Presentational product card used on the home page and the catalog listing.
 * Non-clickable this phase; the "Add to cart" button is inert (cart is a later phase).
 */
@Component({
  selector: 'app-product-card',
  imports: [CurrencyPipe, RouterLink],
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
          <span class="now">{{ product().price | currency: settings.currency() }}</span>
          @if (product().discountPrice) {
            <span class="was">{{ product().discountPrice | currency: settings.currency() }}</span>
          }
        </div>
        <span class="product-stock">{{ stockLabel() }}</span>
        <button type="button" class="btn btn-primary product-add" [disabled]="product().stockQuantity <= 0">
          {{ product().stockQuantity <= 0 ? 'Out of stock' : 'Add to cart' }}
        </button>
      </div>
    </article>
  `,
  styleUrl: './product-card.scss',
})
export class ProductCard {
  protected readonly settings = inject(SettingsService);

  readonly product = input.required<Product>();

  protected readonly stockLabel = computed(() => {
    const q = this.product().stockQuantity;
    if (q <= 0) return 'Out of stock';
    if (q < 10) return `Only ${q} left`;
    return 'In stock';
  });
}
