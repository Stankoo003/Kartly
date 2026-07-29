/**
 * Everything a render site needs to turn a base-currency (EUR) amount into display text.
 *
 * Deliberately a single object rather than two pipe arguments: MoneyPipe is pure, and Angular
 * memoises a pure pipe on argument *identity*. Handing it one object that CurrencyService
 * recomputes only when the currency or the rate table changes means transform() re-runs exactly
 * then — no more, no less.
 */
export interface MoneyContext {
  /** ISO 4217 code to display in. */
  readonly code: string;
  /** Units of `code` per 1 unit of the base currency. 1 when displaying the base itself. */
  readonly rate: number;
}
