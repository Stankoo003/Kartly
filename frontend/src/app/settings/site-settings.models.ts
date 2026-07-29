/**
 * Frontend mirror of the backend settings DTOs
 * (backend/src/Kartly.Application/Settings/SettingsDtos.cs).
 */

/** Supported currencies — mirrors backend Currencies.All. */
export const CURRENCIES = ['RSD', 'EUR', 'USD', 'GBP'] as const;
export type Currency = (typeof CURRENCIES)[number];

export interface SiteSettings {
  siteName: string;
  contactEmail: string;
  currency: string;
  bannerTitle: string;
  bannerSubtitle: string;
  updatedAt: string;
}

/** Full-replace payload — all fields are required. */
export interface UpdateSiteSettingsRequest {
  siteName: string;
  contactEmail: string;
  currency: string;
  bannerTitle: string;
  bannerSubtitle: string;
}
