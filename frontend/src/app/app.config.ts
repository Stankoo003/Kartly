import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './auth/auth.interceptor';
import { CurrencyService } from './currency/currency.service';
import { SettingsService } from './settings/settings.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Cross-fade between routes via the browser's native View Transitions API;
    // styled by the ::view-transition-* rules in styles.scss. Where the API is
    // missing the router just navigates normally — pure progressive enhancement.
    // skipInitialTransition because provideAppInitializer below already blocks
    // the first render, and fading in over that would read as a stall.
    provideRouter(routes, withViewTransitions({ skipInitialTransition: true })),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Load site settings and exchange rates before the first render. Rates block too: the
    // default display currency is RSD while amounts are stored in EUR, so the very first paint
    // already converts — loading them afterwards would visibly re-format every price.
    // Both load() calls swallow their errors, so an unreachable API delays boot by at most the
    // request timeout and then falls back rather than failing.
    provideAppInitializer(() =>
      forkJoin([inject(SettingsService).load(), inject(CurrencyService).load()])),
  ]
};
