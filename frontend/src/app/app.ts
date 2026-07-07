import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Product {
  id: string;
  name: string;
  price: number;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly http = inject(HttpClient);

  protected readonly products = signal<Product[]>([]);
  protected readonly health = signal('checking…');
  protected readonly name = signal('');
  protected readonly price = signal(0);

  constructor() {
    this.checkHealth();
    this.load();
  }

  protected checkHealth(): void {
    this.http
      .get('/api/health', { responseType: 'text' })
      .subscribe({
        next: status => this.health.set(status),
        error: () => this.health.set('unreachable')
      });
  }

  protected load(): void {
    this.http.get<Product[]>('/api/products').subscribe(p => this.products.set(p));
  }

  protected add(): void {
    this.http
      .post<Product>('/api/products', { name: this.name(), price: this.price() })
      .subscribe(() => {
        this.name.set('');
        this.price.set(0);
        this.load();
      });
  }
}
