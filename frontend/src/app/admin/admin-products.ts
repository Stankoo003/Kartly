import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

/** Admin-only screen for adding products. Reachable only past the adminGuard. */
@Component({
  selector: 'app-admin-products',
  imports: [FormsModule],
  template: `
    <main style="max-width: 480px; margin: 2rem auto; font-family: system-ui, sans-serif">
      <h1>Admin — add product</h1>
      <form (submit)="$event.preventDefault(); add()" style="display: flex; gap: 0.5rem">
        <input [(ngModel)]="name" name="name" placeholder="Name" required />
        <input [(ngModel)]="price" name="price" type="number" step="0.01" placeholder="Price" style="width: 90px" />
        <button type="submit">Add</button>
      </form>
      @if (message()) {
        <p>{{ message() }}</p>
      }
    </main>
  `,
})
export class AdminProducts {
  private readonly http = inject(HttpClient);

  protected readonly name = signal('');
  protected readonly price = signal(0);
  protected readonly message = signal('');

  protected add(): void {
    this.http.post('/api/products', { name: this.name(), price: this.price() }).subscribe({
      next: () => {
        this.message.set(`Added "${this.name()}".`);
        this.name.set('');
        this.price.set(0);
      },
      error: () => this.message.set('Failed — are you signed in as Admin?'),
    });
  }
}
