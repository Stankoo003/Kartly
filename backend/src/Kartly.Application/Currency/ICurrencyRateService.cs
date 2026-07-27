namespace Kartly.Application.Currency;

/// <summary>
/// Supplies exchange rates for converting stored (base-currency) amounts into the
/// currency the storefront is displaying.
/// </summary>
public interface ICurrencyRateService
{
    /// <summary>
    /// Current rates. Never throws and never returns null: on any upstream failure it degrades
    /// to the last known-good rates, or to <see cref="FallbackExchangeRates"/>, with
    /// <see cref="ExchangeRatesResponse.Source"/> set to "fallback". A rate outage must not
    /// take prices off the storefront.
    /// </summary>
    Task<ExchangeRatesResponse> GetRatesAsync(CancellationToken ct = default);
}
