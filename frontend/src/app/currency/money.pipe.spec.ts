import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MoneyPipe } from './money.pipe';
import { MoneyContext } from './money.models';

const EUR: MoneyContext = { code: 'EUR', rate: 1 };
const USD: MoneyContext = { code: 'USD', rate: 1.14 };
const RSD: MoneyContext = { code: 'RSD', rate: 117.2 };

describe('MoneyPipe', () => {
  const pipe = () => TestBed.runInInjectionContext(() => new MoneyPipe());

  it('renders a base-currency amount unconverted', () => {
    expect(pipe().transform(10, EUR)).toBe('€10.00');
  });

  it('converts at the context rate', () => {
    expect(pipe().transform(10, USD)).toBe('$11.40');
  });

  it('uses each currency ISO minor units — RSD has none', () => {
    const out = pipe().transform(10, RSD)!;
    // 10 × 117.2 = 1172, and RSD carries 0 decimals, so no fractional part at all.
    expect(out).toContain('1,172');
    expect(out).not.toContain('.');
  });

  it('rounds half-up at an exactly representable midpoint', () => {
    // 0.125 is exact in binary, so this is a true half-way case: 12.5 minor units → 13.
    expect(pipe().transform(0.125, EUR)).toBe('€0.13');
  });

  it('rounds by the value actually held, not the literal as written', () => {
    // 1.005 is stored as 1.00499999…, so it rounds down. Documenting the behaviour rather than
    // papering over it with an epsilon that would be wrong in the other direction.
    expect(pipe().transform(1.005, EUR)).toBe('€1.00');
  });

  it('converts then rounds, so the shown figure matches the computed one', () => {
    // 1299 × 117.398963 = 152,501.25 → RSD has no minor units, so 152,501.
    expect(pipe().transform(1299, { code: 'RSD', rate: 117.398963 })).toContain('152,501');
  });

  it.each([null, undefined, '', 'abc', Number.NaN])('returns null for %p', value => {
    expect(pipe().transform(value as never, EUR)).toBeNull();
  });

  it('accepts numeric strings', () => {
    expect(pipe().transform('10', EUR)).toBe('€10.00');
  });
});

/**
 * The test that earns its keep: MoneyPipe is pure, so Angular skips transform() whenever its
 * arguments are unchanged. Passing the context as an argument is what makes a currency change
 * actually re-render. A well-meaning refactor to read the signal inside transform() instead
 * would leave prices silently stale — and would fail right here.
 */
describe('MoneyPipe reactivity', () => {
  @Component({
    selector: 'app-money-host',
    imports: [MoneyPipe],
    template: `{{ 10 | money: ctx() }}`,
  })
  class Host {
    readonly ctx = signal<MoneyContext>(EUR);
  }

  it('re-renders when the context changes', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent.trim()).toBe('€10.00');

    fixture.componentInstance.ctx.set(USD);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent.trim()).toBe('$11.40');
  });
});
