using Kartly.Application.Settings;

namespace Kartly.Application.Currency;

/// <summary>
/// Built-in rates used when the provider is unreachable and nothing better is cached.
/// Approximate and display-only — good enough to keep the storefront showing sane prices
/// through an outage, not good enough to price a real transaction against. RSD is effectively
/// pegged to EUR so it barely drifts; USD and GBP will.
/// </summary>
public static class FallbackExchangeRates
{
    public static readonly IReadOnlyDictionary<string, decimal> Rates =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            [Currencies.Eur] = 1.00m,
            [Currencies.Rsd] = 117.20m,
            [Currencies.Usd] = 1.14m,
            [Currencies.Gbp] = 0.85m,
        };
}
