import { Component } from '@angular/core';

/** Placeholder Users section. Role management endpoints exist server-side; the UI is TBD. */
@Component({
  selector: 'app-admin-users',
  template: `
    <main class="screen">
      <div class="card elev-sm">
        <h2>Users</h2>
        <p class="text-muted">User management is coming soon.</p>
      </div>
    </main>
  `,
  styles: [`
    .screen { max-width: 920px; margin: var(--space-6) auto; padding: 0 var(--space-4); }
    .screen h2 { margin: 0; }
  `],
})
export class AdminUsers {}
