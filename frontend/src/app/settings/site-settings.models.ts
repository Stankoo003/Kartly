/**
 * Frontend mirror of the backend settings DTOs
 * (backend/src/Kartly.Application/Settings/SettingsDtos.cs).
 */

/** Supported currencies — mirrors backend Currencies.All. */
export const CURRENCIES = ['RSD', 'EUR', 'USD', 'GBP'] as const;
export type Currency = (typeof CURRENCIES)[number];

/**
 * The currency every stored amount is in — product prices, order totals, cart lines.
 * Mirrors backend Currencies.Base. The site currency is a display choice layered on top;
 * conversion happens only when rendering.
 */
export const BASE_CURRENCY = 'EUR';

export interface SiteSettings {
  siteName: string;
  contactEmail: string;
  /** The currency to display prices in. */
  currency: string;
  bannerTitle: string;
  bannerSubtitle: string;
  updatedAt: string;
  /** The currency stored amounts are denominated in. Read-only — not part of the update payload. */
  baseCurrency: string;
}

/** Full-replace payload — all fields are required. */
export interface UpdateSiteSettingsRequest {
  siteName: string;
  contactEmail: string;
  currency: string;
  bannerTitle: string;
  bannerSubtitle: string;
}
