import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { Observable, catchError, of, tap } from 'rxjs';
import { SiteSettings, UpdateSiteSettingsRequest } from './site-settings.models';

/** Used until the API responds (and if it never does), so the UI always renders something sane. */
const FALLBACK = { siteName: 'Kartly', contactEmail: '', currency: 'RSD' } as const;

/**
 * Holds the site-wide settings. Signal-backed so the storefront (nav, footer, prices) re-renders
 * the moment an admin saves — no page reload needed.
 */
@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly title = inject(Title);
  private readonly settings = signal<SiteSettings | null>(null);

  readonly siteName = computed(() => this.settings()?.siteName || FALLBACK.siteName);
  readonly contactEmail = computed(() => this.settings()?.contactEmail ?? FALLBACK.contactEmail);
  readonly currency = computed(() => this.settings()?.currency || FALLBACK.currency);

  /** Current values for an edit form, falling back to defaults before the first load. */
  snapshot(): UpdateSiteSettingsRequest {
    return {
      siteName: this.siteName(),
      contactEmail: this.contactEmail(),
      currency: this.currency(),
    };
  }

  /**
   * Loads settings at bootstrap. Errors are swallowed so the app still starts when the API is
   * unreachable — the fallbacks above keep the shell usable.
   */
  load(): Observable<SiteSettings | null> {
    return this.http.get<SiteSettings>('/api/settings').pipe(
      tap(settings => this.apply(settings)),
      catchError(() => of(null)),
    );
  }

  /** Admin-only save. The response is applied immediately so the storefront updates live. */
  update(request: UpdateSiteSettingsRequest): Observable<SiteSettings> {
    return this.http
      .put<SiteSettings>('/api/settings', request)
      .pipe(tap(settings => this.apply(settings)));
  }

  private apply(settings: SiteSettings): void {
    this.settings.set(settings);
    this.title.setTitle(settings.siteName);
  }
}
