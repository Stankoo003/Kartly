import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './auth/auth.interceptor';
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
    // Load site settings before the first render so the nav/title never flash defaults.
    provideAppInitializer(() => inject(SettingsService).load()),
  ]
};
