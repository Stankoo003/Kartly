import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';

interface Product {
  id: string;
  name: string;
  price: number;
}

/** Public-ish landing: shows API health and the product catalog (requires a token). */
@Component({
  selector: 'app-home',
  imports: [DecimalPipe],
  template: `
    <main style="max-width: 480px; margin: 2rem auto; font-family: system-ui, sans-serif">
      <h1>Kartly — products</h1>
      <p>API health (<code>/api/health</code>): <strong>{{ health() }}</strong></p>
      <p>The catalog requires a valid token; sign in first.</p>

      @if (products().length === 0) {
        <p>No products to show — sign in, or ask an admin to add some.</p>
      } @else {
        <ul>
          @for (p of products(); track p.id) {
            <li>{{ p.name }} — {{ p.price | number: '1.2-2' }}</li>
          }
        </ul>
      }
    </main>
  `,
})
export class Home {
  private readonly http = inject(HttpClient);

  protected readonly products = signal<Product[]>([]);
  protected readonly health = signal('checking…');

  constructor() {
    this.checkHealth();
    this.load();
  }

  private checkHealth(): void {
    this.http.get('/api/health', { responseType: 'text' }).subscribe({
      next: status => this.health.set(status),
      error: () => this.health.set('unreachable'),
    });
  }

  private load(): void {
    // 401 when unauthenticated — swallow so the page still renders.
    this.http.get<Product[]>('/api/products').subscribe({
      next: p => this.products.set(p),
      error: () => this.products.set([]),
    });
  }
}
