import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UiButton } from '../ui/ui-button';

@Component({
  selector: 'app-login',
  imports: [FormsModule, UiButton],
  template: `
    <main class="login">
      <div class="card elev-md">
        <h2>Sign in</h2>
        <form (submit)="$event.preventDefault(); submit()">
          <div class="field">
            <label>Email</label>
            <input class="input" [(ngModel)]="email" name="email" type="email" placeholder="you@example.com" required />
          </div>
          <div class="field">
            <label>Password</label>
            <input class="input" [(ngModel)]="password" name="password" type="password" placeholder="••••••••" required />
          </div>
          <button uiButton variant="primary" [block]="true" type="submit" [disabled]="loading()">
            {{ loading() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
      </div>
    </main>
  `,
  styles: [`
    .login { max-width: 380px; margin: 12vh auto; padding: 0 var(--space-4); }
    .login .card { gap: var(--space-3); }
    .login form { display: grid; gap: var(--space-3); }
    .login .error { margin: var(--space-2) 0 0; color: var(--color-danger); font-size: 13px; }
  `],
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  protected submit(): void {
    this.loading.set(true);
    this.error.set('');
    this.auth.login({ email: this.email(), password: this.password() }).subscribe({
      next: () => this.router.navigate([this.auth.isAdmin() ? '/admin' : '/']),
      error: () => {
        this.error.set('Invalid email or password.');
        this.loading.set(false);
      },
    });
  }
}
