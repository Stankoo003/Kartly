using System.Net.Http.Json;
using System.Text.Json;
using Kartly.Application.Currency;
using Kartly.Application.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Kartly.Infrastructure.Currency;

/// <summary>
/// Fetches exchange rates from open.er-api.com (keyless, daily updates) and caches them until
/// the provider's own next-update time.
///
/// Registered through AddHttpClient, which makes this type *transient* — so no state may live in
/// instance fields. Everything that must survive between requests goes into IMemoryCache, which
/// is a singleton.
/// </summary>
public sealed class OpenErApiCurrencyRateService(
    HttpClient http,
    IMemoryCache cache,
    ILogger<OpenErApiCurrencyRateService> logger) : ICurrencyRateService
{
    private const string CacheKey = "currency:rates";
    private const string LastGoodCacheKey = "currency:rates:lastgood";

    /// <summary>Floor on cache lifetime — guards against a past/bogus next-update turning into a fetch per request.</summary>
    private static readonly TimeSpan MinCacheLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Ceiling on cache lifetime — guards against a far-future next-update freezing rates forever.</summary>
    private static readonly TimeSpan MaxCacheLifetime = TimeSpan.FromHours(24);

    /// <summary>How long a degraded (fallback) result is cached, so a dead provider isn't called per request.</summary>
    private static readonly TimeSpan FallbackCacheLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Serialises refreshes so a cache miss under load issues one upstream request, not N.</summary>
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public async Task<ExchangeRatesResponse> GetRatesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out ExchangeRatesResponse? cached) && cached is not null)
            return cached;

        await RefreshLock.WaitAsync(ct);
        try
        {
            // Another caller may have refreshed while we waited for the lock.
            if (cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            var fresh = await FetchAsync(ct);
            if (fresh is not null)
            {
                cache.Set(CacheKey, fresh, ClampedLifetime(fresh.NextUpdateAt));
                cache.Set(LastGoodCacheKey, fresh);
                return fresh;
            }

            var degraded = Degraded();
            cache.Set(CacheKey, degraded, FallbackCacheLifetime);
            return degraded;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    /// <summary>Returns null on any failure — the caller degrades rather than propagating.</summary>
    private async Task<ExchangeRatesResponse?> FetchAsync(CancellationToken ct)
    {
        try
        {
            var payload = await http.GetFromJsonAsync<OpenErApiResponse>(Currencies.Base, ct);
            return Validate(payload) ? Project(payload!) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Exchange-rate fetch failed; serving fallback rates.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching exchange rates; serving fallback rates.");
            return null;
        }
    }

    /// <summary>
    /// Defensive validation so an upstream shape change degrades to fallback instead of throwing.
    /// </summary>
    private bool Validate(OpenErApiResponse? payload)
    {
        if (payload is null || !string.Equals(payload.Result, "success", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Exchange-rate provider returned result '{Result}'.", payload?.Result);
            return false;
        }

        if (!string.Equals(payload.BaseCode, Currencies.Base, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Exchange-rate provider returned base '{Base}', expected '{Expected}'.",
                payload.BaseCode, Currencies.Base);
            return false;
        }

        if (payload.Rates is not { Count: > 0 })
        {
            logger.LogWarning("Exchange-rate provider returned no rates.");
            return false;
        }

        foreach (var code in Currencies.All)
        {
            if (!payload.Rates.TryGetValue(code, out var rate) || rate <= 0)
            {
                logger.LogWarning("Exchange-rate provider is missing a usable rate for {Code}.", code);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Narrows the provider's ~166 currencies down to the four the admin can actually pick.
    /// Keeps the payload small and makes it structurally impossible to serve a rate for a
    /// currency the storefront can't display.
    /// </summary>
    private static ExchangeRatesResponse Project(OpenErApiResponse payload)
    {
        var rates = Currencies.All.ToDictionary(
            code => code,
            code => payload.Rates![code],
            StringComparer.OrdinalIgnoreCase);

        rates[Currencies.Base] = 1m; // by definition, whatever the provider says

        return new ExchangeRatesResponse(
            Currencies.Base,
            rates,
            DateTimeOffset.FromUnixTimeSeconds(payload.TimeLastUpdateUnix).UtcDateTime,
            DateTimeOffset.FromUnixTimeSeconds(payload.TimeNextUpdateUnix).UtcDateTime,
            ExchangeRatesResponse.SourceLive);
    }

    /// <summary>Last known-good rates if we ever had them, otherwise the built-in table.</summary>
    private ExchangeRatesResponse Degraded()
    {
        if (cache.TryGetValue(LastGoodCacheKey, out ExchangeRatesResponse? lastGood) && lastGood is not null)
            return lastGood with { Source = ExchangeRatesResponse.SourceFallback };

        var now = DateTime.UtcNow;
        return new ExchangeRatesResponse(
            Currencies.Base,
            FallbackExchangeRates.Rates,
            now,
            now.Add(FallbackCacheLifetime),
            ExchangeRatesResponse.SourceFallback);
    }

    private static TimeSpan ClampedLifetime(DateTime nextUpdateAt)
    {
        // +5min so we don't wake up a moment before the provider has actually published.
        var lifetime = nextUpdateAt - DateTime.UtcNow + TimeSpan.FromMinutes(5);
        if (lifetime < MinCacheLifetime) return MinCacheLifetime;
        return lifetime > MaxCacheLifetime ? MaxCacheLifetime : lifetime;
    }
}
