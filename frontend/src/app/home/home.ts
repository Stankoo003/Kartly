import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '../products/product.service';
import { SettingsService } from '../settings/settings.service';
import { ProductCard } from '../products/product-card';
import { PRODUCT_CATEGORIES, Product } from '../products/product.models';

/** Public storefront home: hero, categories, featured + recent products, promo. No auth required. */
@Component({
  selector: 'app-home',
  imports: [RouterLink, ProductCard],
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
}
