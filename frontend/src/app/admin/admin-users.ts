import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { UserService } from '../users/user.service';
import { AppUser, USER_ROLES } from '../users/user.models';
import { UiButton } from '../ui/ui-button';
import { UiDialog } from '../ui/ui-dialog';

type DialogMode = 'role' | 'deactivate' | 'details' | null;

/** Admin-only users screen: paginated + searchable list, role changes and activate/deactivate. */
@Component({
  selector: 'app-admin-users',
  imports: [FormsModule, UiButton, UiDialog],
  templateUrl: './admin-users.html',
  styleUrl: './admin-users.scss',
})
export class AdminUsers {
  private readonly api = inject(UserService);
  private readonly auth = inject(AuthService);

  protected readonly roles = USER_ROLES;
  protected readonly currentEmail = this.auth.email;

  protected readonly users = signal<AppUser[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected search = '';

  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly saving = signal(false);

  protected readonly dialogMode = signal<DialogMode>(null);
  protected readonly formError = signal('');
  protected selected: AppUser | null = null;
  protected roleValue = '';

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize)),
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api
      .list({ page: this.page(), pageSize: this.pageSize, search: this.search.trim() || undefined })
      .subscribe({
        next: result => {
          this.users.set(result.items);
          this.total.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load users. Is the API running and are you signed in as Admin?');
          this.loading.set(false);
        },
      });
  }

  protected applySearch(): void {
    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) return;
    this.page.set(page);
    this.load();
  }

  /** True when the row is the signed-in admin's own account (self-actions are blocked). */
  protected isSelf(u: AppUser): boolean {
    return u.email === this.currentEmail();
  }

  // --- dialogs ---

  protected openRole(u: AppUser): void {
    this.selected = u;
    this.roleValue = u.role;
    this.formError.set('');
    this.dialogMode.set('role');
  }

  protected openDeactivate(u: AppUser): void {
    this.selected = u;
    this.formError.set('');
    this.dialogMode.set('deactivate');
  }

  protected openDetails(u: AppUser): void {
    this.selected = u;
    this.dialogMode.set('details');
  }

  protected closeDialog(): void {
    if (this.saving()) return;
    this.dialogMode.set(null);
  }

  // --- mutations ---

  protected saveRole(): void {
    if (!this.selected || this.roleValue === this.selected.role) {
      this.dialogMode.set(null);
      return;
    }
    this.saving.set(true);
    this.formError.set('');
    this.api.changeRole(this.selected.id, this.roleValue).subscribe({
      next: () => this.afterMutation(),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.formError.set(this.describeError(err));
      },
    });
  }

  protected confirmDeactivate(): void {
    if (!this.selected) return;
    this.setActive(this.selected, false, true);
  }

  protected activate(u: AppUser): void {
    this.setActive(u, true, false);
  }

  private setActive(u: AppUser, isActive: boolean, fromDialog: boolean): void {
    this.saving.set(true);
    this.formError.set('');
    this.api.setActive(u.id, isActive).subscribe({
      next: () => this.afterMutation(),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const message = this.describeError(err);
        if (fromDialog) this.formError.set(message);
        else this.error.set(message);
      },
    });
  }

  private afterMutation(): void {
    this.saving.set(false);
    this.dialogMode.set(null);
    this.selected = null;
    this.load();
  }

  /** Turn an API error into a readable message: validation 400 fields, 409 conflict, etc. */
  private describeError(err: HttpErrorResponse): string {
    const body = err.error;
    if (body?.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, string[]>).flat();
      if (messages.length) return messages.join(' ');
    }
    if (typeof body?.error === 'string') return body.error;
    if (typeof body?.title === 'string') return body.title;
    return 'Something went wrong. Please try again.';
  }
}
