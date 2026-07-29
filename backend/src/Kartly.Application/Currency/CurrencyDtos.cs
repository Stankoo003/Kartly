namespace Kartly.Application.Currency;

/// <summary>
/// Exchange rates for the supported currencies, anchored to the base currency.
/// </summary>
/// <param name="Base">The anchor — always <see cref="Settings.Currencies.Base"/>.</param>
/// <param name="Rates">Units of each currency per 1 unit of <paramref name="Base"/>. Always contains the base itself at 1.</param>
/// <param name="UpdatedAt">When the upstream provider last published these rates (UTC).</param>
/// <param name="NextUpdateAt">When the provider expects to publish next (UTC). Drives cache expiry.</param>
/// <param name="Source">"live" when fetched from the provider, "fallback" when serving cached-last-good or built-in rates.</param>
public sealed record ExchangeRatesResponse(
    string Base,
    IReadOnlyDictionary<string, decimal> Rates,
    DateTime UpdatedAt,
    DateTime NextUpdateAt,
    string Source)
{
    public const string SourceLive = "live";
    public const string SourceFallback = "fallback";
}
