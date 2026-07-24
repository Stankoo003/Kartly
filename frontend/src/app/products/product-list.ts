import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { ProductService } from './product.service';
import { PRODUCT_CATEGORIES, Product, ProductQuery, ProductSortBy } from './product.models';
import { ProductCard } from './product-card';

type SortKey = 'newest' | 'price-asc' | 'price-desc' | 'name';

interface SortOption {
  key: SortKey;
  label: string;
  sortBy: ProductSortBy;
  desc: boolean;
}

const SORTS: readonly SortOption[] = [
  { key: 'newest', label: 'Newest', sortBy: 'CreatedAt', desc: true },
  { key: 'price-asc', label: 'Price: low to high', sortBy: 'Price', desc: false },
  { key: 'price-desc', label: 'Price: high to low', sortBy: 'Price', desc: true },
  { key: 'name', label: 'Name: A–Z', sortBy: 'Name', desc: false },
];

/** Upper bound of the price slider — comfortably above the seeded catalog's top price. */
const MAX_PRICE = 2000;
const PAGE_SIZE = 9;

/** Public catalog: category + price filters, search, sort and pagination, all driven by the URL. */
@Component({
  selector: 'app-product-list',
  imports: [ProductCard, RouterLink],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList {
  private readonly api = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly allCategories = PRODUCT_CATEGORIES;
  protected readonly sorts = SORTS;
  protected readonly maxPriceCap = MAX_PRICE;
  protected readonly pageSize = PAGE_SIZE;
  protected readonly skeletons = Array.from({ length: 6 });

  /** URL query params are the single source of truth for the filter state. */
  private readonly params = toSignal(this.route.queryParamMap.pipe(map(p => ({
    category: p.get('category') ?? '',
    search: p.get('search') ?? '',
    sort: (p.get('sort') as SortKey) ?? 'newest',
    maxPrice: Number(p.get('maxPrice')) || MAX_PRICE,
    page: Math.max(1, Number(p.get('page')) || 1),
  }))), { initialValue: { category: '', search: '', sort: 'newest' as SortKey, maxPrice: MAX_PRICE, page: 1 } });

  protected readonly category = computed(() => this.params().category);
  protected readonly search = computed(() => this.params().search);
  protected readonly sort = computed<SortKey>(() => this.params().sort);
  protected readonly maxPrice = computed(() => this.params().maxPrice);
  protected readonly page = computed(() => this.params().page);

  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly items = signal<Product[]>([]);
  protected readonly total = signal(0);

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / PAGE_SIZE)));
  protected readonly pages = computed(() => {
    const last = this.totalPages();
    const start = Math.max(1, Math.min(this.page() - 2, last - 4));
    const end = Math.min(last, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  protected readonly resultLabel = computed(() => {
    if (this.search()) return `Results for “${this.search()}”`;
    if (this.category()) return this.category();
    return 'All products';
  });

  protected readonly countLabel = computed(() => {
    const t = this.total();
    return t === 1 ? '1 product' : `${t} products`;
  });

  protected readonly showingLabel = computed(() => {
    const t = this.total();
    if (t === 0) return 'No products';
    const from = (this.page() - 1) * PAGE_SIZE + 1;
    const to = Math.min(t, this.page() * PAGE_SIZE);
    return `Showing ${from}–${to} of ${t}`;
  });

  constructor() {
    // Re-fetch whenever any URL-derived filter changes.
    effect(() => {
      const p = this.params();
      this.load(p);
    });
  }

  private load(p: ReturnType<typeof this.params>): void {
    const sort = SORTS.find(s => s.key === p.sort) ?? SORTS[0];
    const query: ProductQuery = {
      page: p.page,
      pageSize: PAGE_SIZE,
      sortBy: sort.sortBy,
      sortDescending: sort.desc,
      // isActive is intentionally omitted → API defaults to active-only (and clamps for anonymous).
      ...(p.category ? { category: p.category } : {}),
      ...(p.search ? { search: p.search } : {}),
      ...(p.maxPrice < MAX_PRICE ? { maxPrice: p.maxPrice } : {}),
    };

    this.loading.set(true);
    this.error.set('');
    this.api.list(query).subscribe({
      next: r => {
        this.items.set(r.items);
        this.total.set(r.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load products. Please try again.');
        this.items.set([]);
        this.total.set(0);
        this.loading.set(false);
      },
    });
  }

  /** Merge new params into the URL, always resetting to page 1 (except when paging). */
  private navigate(patch: Params, keepPage = false): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: keepPage ? patch : { ...patch, page: null },
      queryParamsHandling: 'merge',
    });
  }

  protected setCategory(c: string): void {
    this.navigate({ category: c || null });
  }

  protected setSort(key: string): void {
    this.navigate({ sort: key === 'newest' ? null : key });
  }

  protected setMaxPrice(value: string): void {
    const v = Number(value);
    this.navigate({ maxPrice: v >= MAX_PRICE ? null : v });
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.navigate({ page }, true);
  }

  protected reset(): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }
}
