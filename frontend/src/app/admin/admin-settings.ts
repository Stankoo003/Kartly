import { Component } from '@angular/core';

/** Placeholder Settings section. No backend settings exist yet; content is TBD. */
@Component({
  selector: 'app-admin-settings',
  template: `
    <main class="screen">
      <div class="card elev-sm">
        <h2>Settings</h2>
        <p class="text-muted">Settings are coming soon.</p>
      </div>
    </main>
  `,
  styles: [`
    .screen { max-width: 920px; margin: var(--space-6) auto; padding: 0 var(--space-4); }
    .screen h2 { margin: 0; }
  `],
})
export class AdminSettings {}
