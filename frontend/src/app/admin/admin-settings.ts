import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { SettingsService } from '../settings/settings.service';
import { CURRENCIES, UpdateSiteSettingsRequest } from '../settings/site-settings.models';
import { UiButton } from '../ui/ui-button';

/** Admin-only site settings: name, contact email and currency, consumed by the storefront. */
@Component({
  selector: 'app-admin-settings',
  imports: [FormsModule, UiButton],
  templateUrl: './admin-settings.html',
  styleUrl: './admin-settings.scss',
})
export class AdminSettings {
  private readonly settings = inject(SettingsService);

  protected readonly currencies = CURRENCIES;

  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly formError = signal('');
  protected readonly fieldErrors = signal<Record<string, string>>({});

  /** Seeded from the already-loaded settings, so the form opens populated. */
  protected form: UpdateSiteSettingsRequest = this.settings.snapshot();

  protected submit(): void {
    if (!this.validate()) return;

    this.saving.set(true);
    this.formError.set('');
    this.saved.set(false);

    this.settings.update({
      siteName: this.form.siteName.trim(),
      contactEmail: this.form.contactEmail.trim(),
      currency: this.form.currency,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.formError.set(this.describeError(err));
      },
    });
  }

  protected reset(): void {
    this.form = this.settings.snapshot();
    this.fieldErrors.set({});
    this.formError.set('');
    this.saved.set(false);
  }

  /** Client-side checks mirroring the server rules. */
  private validate(): boolean {
    const errors: Record<string, string> = {};

    if (!this.form.siteName.trim()) errors['siteName'] = 'Site name is required.';
    else if (this.form.siteName.trim().length > 100) errors['siteName'] = 'Site name is too long (max 100).';

    const email = this.form.contactEmail.trim();
    if (!email) errors['contactEmail'] = 'Contact email is required.';
    else if (!/^\S+@\S+\.\S+$/.test(email)) errors['contactEmail'] = 'Enter a valid email address.';

    if (!this.form.currency) errors['currency'] = 'Currency is required.';

    this.fieldErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  /** Turn an API error into a readable message: validation 400 fields, etc. */
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
