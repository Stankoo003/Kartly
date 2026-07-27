/**
 * Frontend mirror of the backend currency DTOs
 * (backend/src/Kartly.Application/Currency/CurrencyDtos.cs).
 */

export interface ExchangeRates {
  /** Anchor currency — always the base currency (EUR). */
  base: string;
  /** Units of each currency per 1 unit of `base`. Contains the base itself at 1. */
  rates: Record<string, number>;
  /** When the provider last published these rates (ISO 8601, UTC). */
  updatedAt: string;
  /** When the provider expects to publish next (ISO 8601, UTC). */
  nextUpdateAt: string;
  /** 'live' when fetched from the provider, 'fallback' when degraded. */
  source: 'live' | 'fallback';
}

/**
 * Used until /api/currency/rates responds, and if it never does. Mirrors the backend's
 * FallbackExchangeRates so a client-side and a server-side outage look the same to the shopper.
 * Approximate and display-only.
 */
export const FALLBACK_RATES: Readonly<Record<string, number>> = Object.freeze({
  EUR: 1.0,
  RSD: 117.2,
  USD: 1.14,
  GBP: 0.85,
});
