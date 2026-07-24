using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kartly.Infrastructure.Auth;
using Xunit;

namespace Kartly.IntegrationTests;

/// <summary>
/// End-to-end tests for the site settings endpoints: the public (anonymous) read the storefront
/// relies on, the admin-only update, and validation of the small settings payload.
/// </summary>
public sealed class SettingsTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public SettingsTests(PostgresApiFactory factory) => _factory = factory;

    private sealed record AuthResponse(string Token, string Email, string Role, DateTimeOffset ExpiresAt);
    private sealed record SettingsResponse(string SiteName, string ContactEmail, string Currency, DateTime UpdatedAt);

    // --- read ---

    [Fact]
    public async Task Get_IsAnonymous_AndReturnsSeededDefaults()
    {
        var client = _factory.CreateClient(); // no token at all

        var response = await client.GetAsync("/api/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SettingsResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.SiteName));
        Assert.False(string.IsNullOrWhiteSpace(body.ContactEmail));
        Assert.False(string.IsNullOrWhiteSpace(body.Currency));
    }

    // --- update ---

    [Fact]
    public async Task Update_AsAdmin_PersistsAndIsVisibleToAnonymousReaders()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);

        var payload = new { siteName = "Kartly Shop", contactEmail = "hello@kartly.test", currency = "EUR" };
        var update = await admin.PutAsJsonAsync("/api/settings", payload);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<SettingsResponse>();
        Assert.Equal("Kartly Shop", updated!.SiteName);
        Assert.Equal("EUR", updated.Currency);

        // The storefront (anonymous) sees the new values.
        var anonymous = _factory.CreateClient();
        var read = await anonymous.GetFromJsonAsync<SettingsResponse>("/api/settings");
        Assert.Equal("Kartly Shop", read!.SiteName);
        Assert.Equal("hello@kartly.test", read.ContactEmail);
        Assert.Equal("EUR", read.Currency);
    }

    [Fact]
    public async Task Update_NormalisesCurrencyToUpperCase()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync("/api/settings", new
        {
            siteName = "Case Test", contactEmail = "case@kartly.test", currency = "usd",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SettingsResponse>();
        Assert.Equal("USD", body!.Currency);
    }

    // --- authorization ---

    [Fact]
    public async Task Update_AsCustomer_Returns403()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(client);

        var response = await client.PutAsJsonAsync("/api/settings", new
        {
            siteName = "Nope", contactEmail = "nope@kartly.test", currency = "EUR",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/settings", new
        {
            siteName = "Nope", contactEmail = "nope@kartly.test", currency = "EUR",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- validation ---

    [Theory]
    [InlineData("", "ok@kartly.test", "EUR")]            // blank site name
    [InlineData("Shop", "not-an-email", "EUR")]          // malformed contact email
    [InlineData("Shop", "ok@kartly.test", "XYZ")]        // unsupported currency
    [InlineData("Shop", "ok@kartly.test", "")]           // missing currency
    public async Task Update_InvalidPayload_Returns400(string siteName, string contactEmail, string currency)
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync("/api/settings", new { siteName, contactEmail, currency });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- helpers ---

    private async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, KartlyDbInitializer.DefaultAdminEmail, KartlyDbInitializer.DefaultAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task AuthenticateAsCustomerAsync(HttpClient client)
    {
        var email = $"settings-{Guid.NewGuid():N}@kartly.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }
}
