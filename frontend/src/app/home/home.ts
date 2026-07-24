import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ProductService } from '../products/product.service';
import { SettingsService } from '../settings/settings.service';
import { PRODUCT_CATEGORIES, Product } from '../products/product.models';

/** Public storefront home: hero, categories, featured + recent products, promo. No auth required. */
@Component({
  selector: 'app-home',
  imports: [CurrencyPipe],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly api = inject(ProductService);
  protected readonly settings = inject(SettingsService);

  protected readonly categories = PRODUCT_CATEGORIES;

  protected readonly featured = signal<Product[]>([]);
  protected readonly recent = signal<Product[]>([]);

  constructor() {
    // Errors are swallowed so the page still renders (hero/categories) if the API is down.
    this.api.list({ isFeatured: true, isActive: true, pageSize: 5 }).subscribe({
      next: r => this.featured.set(r.items),
      error: () => this.featured.set([]),
    });
    this.api.list({ sortBy: 'CreatedAt', sortDescending: true, isActive: true, pageSize: 5 }).subscribe({
      next: r => this.recent.set(r.items),
      error: () => this.recent.set([]),
    });
  }

  /** Whole-number stock hint shown on the card. */
  protected stockLabel(p: Product): string {
    if (p.stockQuantity <= 0) return 'Out of stock';
    if (p.stockQuantity < 10) return `Only ${p.stockQuantity} left`;
    return 'In stock';
  }
}
