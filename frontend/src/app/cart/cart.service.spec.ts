import { TestBed } from '@angular/core/testing';
import { CartService } from './cart.service';
import { Product } from '../products/product.models';

/** Minimal Product for cart tests — only the fields the cart reads matter. */
function product(over: Partial<Product> = {}): Product {
  return {
    id: 'p1', name: 'Widget', slug: 'widget', sku: 'W1', category: 'Accessories',
    brand: null, model: null, description: null, imageUrl: null,
    price: 10, discountPrice: null, stockQuantity: 5,
    warrantyMonths: null, isFeatured: false, isActive: true,
    createdAt: '', updatedAt: '', ...over,
  };
}

/** A deterministic in-memory localStorage so persistence is testable. */
function mockStorage() {
  const map = new Map<string, string>();
  const store = {
    getItem: (k: string) => (map.has(k) ? map.get(k)! : null),
    setItem: (k: string, v: string) => void map.set(k, v),
    removeItem: (k: string) => void map.delete(k),
    clear: () => map.clear(),
  };
  vi.stubGlobal('localStorage', store);
  return map;
}

describe('CartService', () => {
  let storage: Map<string, string>;

  beforeEach(() => {
    storage = mockStorage();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
  });

  const make = () => TestBed.inject(CartService);

  it('adds a product and counts quantity', () => {
    const cart = make();
    cart.add(product());
    cart.add(product());
    expect(cart.count()).toBe(2);
    expect(cart.lines().length).toBe(1);
  });

  it('caps quantity at available stock', () => {
    const cart = make();
    const p = product({ stockQuantity: 2 });
    cart.add(p); cart.add(p); cart.add(p); // third is capped
    expect(cart.count()).toBe(2);
  });

  it('ignores out-of-stock products', () => {
    const cart = make();
    cart.add(product({ stockQuantity: 0 }));
    expect(cart.isEmpty()).toBe(true);
  });

  it('setQty clamps to [1, stock]', () => {
    const cart = make();
    cart.add(product({ stockQuantity: 3 }));
    cart.setQty('p1', 99);
    expect(cart.lines()[0].qty).toBe(3);
    cart.setQty('p1', 0);
    expect(cart.lines()[0].qty).toBe(1);
  });

  it('removes and clears', () => {
    const cart = make();
    cart.add(product({ id: 'a' }));
    cart.add(product({ id: 'b' }));
    cart.remove('a');
    expect(cart.lines().length).toBe(1);
    cart.clear();
    expect(cart.isEmpty()).toBe(true);
  });

  it('computes money-safe totals (no float drift)', () => {
    const cart = make();
    cart.add(product({ id: 'x', price: 9.99, stockQuantity: 10 }));
    cart.setQty('x', 3);
    expect(cart.subtotalCents()).toBe(2997);
    expect(cart.subtotal()).toBe(29.97);

    cart.add(product({ id: 'y', price: 0.1, stockQuantity: 10 }));
    cart.add(product({ id: 'z', price: 0.2, stockQuantity: 10 }));
    // 29.97 + 0.10 + 0.20 = 30.27 exactly
    expect(cart.subtotalCents()).toBe(3027);
    expect(cart.subtotal()).toBe(30.27);
  });

  it('persists to localStorage and restores across instances', () => {
    const cart = make();
    cart.add(product({ id: 'keep', price: 5, stockQuantity: 9 }));
    cart.setQty('keep', 4);
    expect(storage.get('kartly.cart')).toContain('keep');

    // A fresh service instance reads the persisted cart back.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const restored = TestBed.inject(CartService);
    expect(restored.count()).toBe(4);
    expect(restored.lines()[0].productId).toBe('keep');
  });
});
