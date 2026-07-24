import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { SettingsService } from './settings/settings.service';
import { UiButton } from './ui/ui-button';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UiButton],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly settings = inject(SettingsService);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );

  /** The admin panel brings its own chrome, so the storefront nav/footer step aside. */
  protected readonly isAdminArea = computed(() => this.url().startsWith('/admin'));

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
