using System.Net;
using System.Net.Http.Json;
using System.Text;
using Kartly.Application.Currency;
using Kartly.Application.Settings;
using Kartly.Infrastructure.Currency;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kartly.IntegrationTests;

/// <summary>
/// End-to-end tests for the exchange-rate endpoint. Every test stubs the upstream provider —
/// hitting the real open.er-api.com would make the suite depend on the internet, on someone
/// else's uptime, and on rates that change daily.
/// </summary>
public sealed class CurrencyTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public CurrencyTests(PostgresApiFactory factory) => _factory = factory;

    private sealed record RatesResponse(
        string Base,
        Dictionary<string, decimal> Rates,
        DateTime UpdatedAt,
        DateTime NextUpdateAt,
        string Source);

    private const string LivePayload = """
        {
          "result": "success",
          "base_code": "EUR",
          "time_last_update_unix": 1785110551,
          "time_next_update_unix": 1785196941,
          "rates": { "EUR": 1, "RSD": 117.4, "USD": 1.14, "GBP": 0.85, "JPY": 178.2 }
        }
        """;

    /// <summary>Serves a canned response (or fails) and counts how many times it was called.</summary>
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    /// <summary>A client whose rate provider is the given stub, with a cache isolated to this host.</summary>
    private HttpClient ClientWith(StubHandler stub) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<ICurrencyRateService, OpenErApiCurrencyRateService>(client =>
                    {
                        client.BaseAddress = new Uri("https://rates.invalid/");
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => stub)))
            .CreateClient();

    [Fact]
    public async Task GetRates_IsAnonymous_AndProjectsToSupportedCurrencies()
    {
        var client = ClientWith(new StubHandler(HttpStatusCode.OK, LivePayload)); // no token at all

        var response = await client.GetAsync("/api/currency/rates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RatesResponse>();

        Assert.Equal(Currencies.Base, body!.Base);
        Assert.Equal("live", body.Source);

        // The provider sent JPY too; only the currencies an admin can pick come back.
        Assert.Equal(Currencies.All.Count, body.Rates.Count);
        foreach (var code in Currencies.All)
        {
            Assert.True(body.Rates.ContainsKey(code), $"missing rate for {code}");
            Assert.True(body.Rates[code] > 0, $"non-positive rate for {code}");
        }

        // The base is 1 by definition, whatever the provider claims.
        Assert.Equal(1m, body.Rates[Currencies.Base]);
    }

    [Fact]
    public async Task GetRates_WhenProviderFails_StillReturns200WithFallbackRates()
    {
        var client = ClientWith(new StubHandler(HttpStatusCode.ServiceUnavailable, "upstream is down"));

        var response = await client.GetAsync("/api/currency/rates");

        // The whole point: a rate outage must not take prices off the storefront.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RatesResponse>();

        Assert.Equal("fallback", body!.Source);
        Assert.Equal(Currencies.Base, body.Base);
        foreach (var code in Currencies.All)
            Assert.True(body.Rates[code] > 0, $"non-positive fallback rate for {code}");
    }

    [Fact]
    public async Task GetRates_WhenProviderReturnsUnexpectedShape_DegradesRatherThanThrowing()
    {
        // 200 OK, valid JSON, but result != success — the failure mode a status-code check misses.
        var client = ClientWith(new StubHandler(
            HttpStatusCode.OK, """{ "result": "error", "error-type": "unsupported-code" }"""));

        var response = await client.GetAsync("/api/currency/rates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RatesResponse>();
        Assert.Equal("fallback", body!.Source);
    }

    [Fact]
    public async Task GetRates_CachesUpstream_SoRepeatedCallsHitTheProviderOnce()
    {
        var stub = new StubHandler(HttpStatusCode.OK, LivePayload);
        var client = ClientWith(stub);

        await client.GetAsync("/api/currency/rates");
        await client.GetAsync("/api/currency/rates");
        await client.GetAsync("/api/currency/rates");

        Assert.Equal(1, stub.Calls);
    }
}
