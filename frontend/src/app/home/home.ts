import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ProductService } from '../products/product.service';
import { SettingsService } from '../settings/settings.service';
import { PagedResult, Product } from '../products/product.models';

/** Public-ish landing: shows API health and the product catalog (requires a token). */
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

  private load(): void {
    // The endpoint returns a paged envelope, not a bare array.
    // 401 when unauthenticated — swallow so the page still renders.
    this.http.get<PagedResult<Product>>('/api/products').subscribe({
      next: result => this.products.set(result.items),
      error: () => this.products.set([]),
    });
  }
}
