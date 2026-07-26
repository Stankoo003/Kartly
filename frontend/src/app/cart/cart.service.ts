import { Injectable, computed, signal } from '@angular/core';
import { Product } from '../products/product.models';
import { CartItem } from './cart.models';

const STORAGE_KEY = 'kartly.cart';

/** Price → integer minor units (cents). Rounds half-up like the backend's decimal handling. */
function toCents(price: number): number {
  return Math.round(price * 100);
}

/**
 * Client-side cart. Signal-backed and persisted to localStorage so it survives a reload.
 * All money is summed in integer cents and divided only for display, so totals never drift
 * (e.g. 9.99 × 3 = 29.97 exactly) — matching the backend's decimal arithmetic.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly items = signal<CartItem[]>(this.restore());

  /** Mini-cart drawer visibility — shared UI state for the header and the drawer. */
  readonly drawerOpen = signal(false);

  readonly lines = this.items.asReadonly();
  readonly count = computed(() => this.items().reduce((n, i) => n + i.qty, 0));
  readonly isEmpty = computed(() => this.items().length === 0);
  readonly subtotalCents = computed(() =>
    this.items().reduce((c, i) => c + toCents(i.price) * i.qty, 0),
  );
  readonly subtotal = computed(() => this.subtotalCents() / 100);

  /** Write-through: update the signal and persist in the same tick. */
  private write(next: CartItem[]): void {
    this.items.set(next);
    this.persist(next);
  }

  /** Per-line total in major units (money-safe). */
  lineTotal(item: CartItem): number {
    return (toCents(item.price) * item.qty) / 100;
  }

  add(product: Product, qty = 1): void {
    if (product.stockQuantity <= 0) return; // out of stock — nothing to add
    const items = this.items();
    const existing = items.find(i => i.productId === product.id);
    if (existing) {
      this.write(items.map(i =>
        i.productId === product.id ? { ...i, qty: Math.min(i.qty + qty, i.stock) } : i,
      ));
      return;
    }
    const item: CartItem = {
      productId: product.id,
      slug: product.slug,
      name: product.name,
      imageUrl: product.imageUrl,
      price: product.price,
      stock: product.stockQuantity,
      qty: Math.min(qty, product.stockQuantity),
    };
    this.write([...items, item]);
  }

  setQty(productId: string, qty: number): void {
    this.write(this.items().map(i =>
      i.productId === productId ? { ...i, qty: Math.max(1, Math.min(qty, i.stock)) } : i,
    ));
  }

  inc(productId: string): void {
    const item = this.items().find(i => i.productId === productId);
    if (item) this.setQty(productId, item.qty + 1);
  }

  dec(productId: string): void {
    const item = this.items().find(i => i.productId === productId);
    if (item) this.setQty(productId, item.qty - 1);
  }

  remove(productId: string): void {
    this.write(this.items().filter(i => i.productId !== productId));
  }

  clear(): void {
    this.write([]);
  }

  open(): void { this.drawerOpen.set(true); }
  close(): void { this.drawerOpen.set(false); }
  toggle(): void { this.drawerOpen.update(v => !v); }

  private persist(items: CartItem[]): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
    } catch {
      // Storage unavailable (private mode / SSR) — the in-memory cart still works.
    }
  }

  private restore(): CartItem[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? (parsed as CartItem[]) : [];
    } catch {
      return [];
    }
  }
}
