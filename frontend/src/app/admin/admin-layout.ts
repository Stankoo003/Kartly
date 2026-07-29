import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { SettingsService } from '../settings/settings.service';

/**
 * Admin shell: a dark sidebar (Products / Users / Settings + sign out) beside a
 * topbar and the active section. It deliberately replaces the storefront chrome —
 * App hides its nav and footer under /admin. Every route beneath it is protected
 * by the adminGuard wired up in app.routes.ts.
 */
@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
  host: { class: 'admin-shell' },
})
export class AdminLayout {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly auth = inject(AuthService);
  protected readonly settings = inject(SettingsService);

  /** Topbar heading, taken from the active child route's `data.title`. */
  protected readonly pageTitle = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      startWith(null),
      map(() => this.activeTitle()),
    ),
    { initialValue: this.activeTitle() },
  );

  /** First letter of the signed-in admin's email, for the avatar circle. */
  protected readonly initial = computed(() => (this.auth.email() ?? '?').charAt(0).toUpperCase());

  /**
   * Walks the snapshot tree rather than the ActivatedRoute tree: during this
   * component's construction the child route exists but its snapshot is not
   * attached yet, so reading route.firstChild.snapshot would throw.
   */
  private activeTitle(): string {
    let snapshot = this.route.snapshot;
    while (snapshot.firstChild) snapshot = snapshot.firstChild;
    return snapshot.data['title'] ?? 'Admin';
  }

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
