import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { ProductService } from './product.service';
import { SettingsService } from '../settings/settings.service';
import { CartService } from '../cart/cart.service';
import { Product } from './product.models';

type Status = 'loading' | 'loaded' | 'not-found' | 'error';

/** Public, deep-linkable single-product view (/products/:slug). 404 → not-found state. */
@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail {
  private readonly api = inject(ProductService);
  protected readonly settings = inject(SettingsService);
  private readonly cart = inject(CartService);

  private readonly slug = toSignal(
    inject(ActivatedRoute).paramMap.pipe(map(p => p.get('slug') ?? '')),
    { initialValue: '' },
  );

  protected readonly status = signal<Status>('loading');
  protected readonly product = signal<Product | null>(null);

  protected readonly outOfStock = computed(() => (this.product()?.stockQuantity ?? 0) <= 0);
  protected readonly stockLabel = computed(() => {
    const q = this.product()?.stockQuantity ?? 0;
    if (q <= 0) return 'Out of stock';
    if (q < 10) return `Only ${q} left`;
    return 'In stock';
  });

  constructor() {
    // Re-fetch whenever the slug in the URL changes (e.g. navigating between products).
    effect(() => {
      const slug = this.slug();
      if (!slug) return;
      this.load(slug);
    });
  }

  protected retry(): void {
    const slug = this.slug();
    if (slug) this.load(slug);
  }

  protected addToCart(): void {
    const p = this.product();
    if (!p) return;
    this.cart.add(p);
    this.cart.open();
  }

  private load(slug: string): void {
    this.status.set('loading');
    this.api.getBySlug(slug).subscribe({
      next: p => {
        this.product.set(p);
        this.status.set('loaded');
      },
      error: (err: HttpErrorResponse) => {
        this.product.set(null);
        this.status.set(err.status === 404 ? 'not-found' : 'error');
      },
    });
  }
}
