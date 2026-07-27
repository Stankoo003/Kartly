import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, tap } from 'rxjs';
import { SettingsService } from '../settings/settings.service';
import { ExchangeRates, FALLBACK_RATES } from './exchange-rates.models';
import { MoneyContext } from './money.models';

/**
 * Holds exchange rates and derives the MoneyContext that every price render site passes to
 * MoneyPipe.
 *
 * Rates are for *display only*. Everything the app stores or sends — cart lines, the unit prices
 * POSTed at checkout — stays in the base currency, because the server re-validates them against
 * Product.Price, which is also base. Convert before that point and checkout starts failing with
 * "the price has changed".
 */
@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly http = inject(HttpClient);
  private readonly settings = inject(SettingsService);

  private readonly ratesTable = signal<Readonly<Record<string, number>>>(FALLBACK_RATES);

  /** When the rates in use were published, or null before the first successful load. */
  readonly updatedAt = signal<string | null>(null);
  /** Whether the rates in use came from the provider or a fallback. Surfaced in admin settings. */
  readonly source = signal<'live' | 'fallback'>('fallback');

  /**
   * Context for storefront prices: the site's display currency at the current rate.
   * A new object identity here is exactly what invalidates MoneyPipe's memoisation, so this
   * recomputing only on a real change is load-bearing, not an optimisation.
   */
  readonly display = computed<MoneyContext>(() => {
    const code = this.settings.currency();
    return { code, rate: this.ratesTable()[code] ?? 1 };
  });

  /** Context for admin screens that show the money of record — base currency, never converted. */
  readonly base = computed<MoneyContext>(() => ({ code: this.settings.baseCurrency(), rate: 1 }));

  /** The current rate for one unit of the base currency, for display in admin settings. */
  readonly displayRate = computed(() => this.display().rate);

  private readonly ownCurrencyContexts = new Map<string, MoneyContext>();

  /**
   * Context for an amount already denominated in `code` — an order's snapshotted currency.
   * Memoised because building `{ code, rate: 1 }` inline in a template would allocate a fresh
   * object on every change-detection pass and defeat the pipe's memoisation.
   */
  ctxFor(code: string): MoneyContext {
    let ctx = this.ownCurrencyContexts.get(code);
    if (!ctx) {
      ctx = { code, rate: 1 };
      this.ownCurrencyContexts.set(code, ctx);
    }
    return ctx;
  }

  /**
   * Loads rates at bootstrap. Errors are swallowed so the app still starts when the API is
   * unreachable — FALLBACK_RATES keeps prices on screen.
   */
  load(): Observable<ExchangeRates | null> {
    return this.http.get<ExchangeRates>('/api/currency/rates').pipe(
      tap(rates => this.apply(rates)),
      catchError(() => of(null)),
    );
  }

  private apply(rates: ExchangeRates): void {
    this.ratesTable.set(rates.rates);
    this.updatedAt.set(rates.updatedAt);
    this.source.set(rates.source);
  }
}
