import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/**
 * Admin shell: a horizontal sub-nav (Products / Users / Settings) rendered under the
 * global nav, with a router-outlet for the active section. Every route beneath it is
 * protected by the adminGuard wired up in app.routes.ts.
 */
@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <nav class="subnav elev-sm">
      <span class="subnav-label">Admin</span>
      <a routerLink="products" routerLinkActive="active">Products</a>
      <a routerLink="users" routerLinkActive="active">Users</a>
      <a routerLink="settings" routerLinkActive="active">Settings</a>
    </nav>

    <router-outlet />
  `,
  styles: [`
    .subnav {
      display: flex;
      align-items: center;
      gap: var(--space-4);
      padding: var(--space-2) var(--space-4);
      background: var(--color-surface);
    }
    .subnav-label {
      font-family: var(--font-heading);
      font-weight: var(--font-heading-weight);
      font-size: 14px;
      margin-right: var(--space-2);
    }
    .subnav a {
      color: inherit;
      text-decoration: none;
      font-size: 14px;
      padding: var(--space-1) 0;
    }
    .subnav a:hover { color: var(--color-accent); }
    .subnav a.active { color: var(--color-accent); }
  `],
})
export class AdminLayout {}
