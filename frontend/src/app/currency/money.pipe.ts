import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';
import { formatCurrency, getCurrencySymbol, getNumberOfCurrencyDigits } from '@angular/common';
import { MoneyContext } from './money.models';

/**
 * Formats a base-currency (EUR) amount in the currency described by `ctx`, converting on the way.
 *
 * Pure on purpose, and the context *must* be passed as an argument rather than read from a service
 * inside transform(). Angular memoises a pure pipe on its arguments: ɵɵpipeBind2 delegates to
 * pureFunction2Internal, which returns the previously cached result whenever the arguments are
 * unchanged — transform() is not called at all. A pipe that read the currency signal internally
 * would therefore keep rendering the old price after a currency change, even though the signal
 * read correctly marks the view dirty. Passing ctx from the template subscribes the view directly
 * and changes the argument identity exactly when a re-format is due.
 */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);

  transform(value: number | string | null | undefined, ctx: MoneyContext): string | null {
    if (value === null || value === undefined || value === '') return null;

    const amount = typeof value === 'string' ? Number(value) : value;
    if (!Number.isFinite(amount)) return null;

    // ISO 4217 minor units, straight from Angular's own currency data: 0 for RSD, 2 for
    // EUR/USD/GBP. No hand-maintained table needed.
    const digits = getNumberOfCurrencyDigits(ctx.code);
    const converted = round(amount * ctx.rate, digits);

    return formatCurrency(
      converted,
      this.locale,
      getCurrencySymbol(ctx.code, 'wide', this.locale),
      ctx.code,
    );
  }
}

/**
 * Rounds to `digits` decimals with Math.round's half-up-on-the-actual-double semantics.
 *
 * formatCurrency would round for display anyway; doing it here as well guarantees the number we
 * computed is the number shown, so nothing downstream can disagree with the rendered text.
 *
 * No epsilon fudge: a literal like 1.005 is stored as 1.00499999…, so it rounds *down*, and that
 * is arithmetically right for the value actually held. Nudging it up would need a relative
 * epsilon (an absolute one is meaningless once the value is scaled by 100) and would trade a
 * correct answer for a more intuitive-looking one. It does not arise in practice regardless:
 * stored prices are numeric(18,2), and a converted amount lands exactly on a half-minor-unit
 * boundary essentially never.
 */
function round(value: number, digits: number): number {
  const factor = 10 ** digits;
  return Math.round(value * factor) / factor;
}
