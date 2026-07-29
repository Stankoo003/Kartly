import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { SettingsService } from './settings/settings.service';
import { PRODUCT_CATEGORIES } from './products/product.models';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly settings = inject(SettingsService);

  protected readonly categories = PRODUCT_CATEGORIES;

  private readonly url = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );

  /** Admin brings its own chrome/theme, so the storefront shell + Cartly theme stand down there. */
  protected readonly isAdminArea = computed(() => this.url().startsWith('/admin'));

  /** First letter of the site name, for the header logo mark. */
  protected readonly brandInitial = computed(() => (this.settings.siteName() || 'C').charAt(0).toUpperCase());

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
