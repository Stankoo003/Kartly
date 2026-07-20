using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kartly.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kartly.IntegrationTests;

/// <summary>
/// End-to-end tests that boot the real API (against the running Postgres) and
/// exercise the auth acceptance criteria over HTTP.
/// </summary>
public sealed class AuthTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AuthTests(PostgresApiFactory factory) => _factory = factory;

    private sealed record AuthResponse(string Token, string Email, string Role, DateTimeOffset ExpiresAt);

    // --- Criterion: POST /auth/login returns a valid JWT for correct credentials, 401 otherwise ---

    [Fact]
    public async Task Login_WithSeededAdminCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = KartlyDbInitializer.DefaultAdminEmail,
            password = KartlyDbInitializer.DefaultAdminPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Admin", body.Role);
        // A JWT is three dot-separated segments.
        Assert.Equal(3, body.Token.Split('.').Length);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = KartlyDbInitializer.DefaultAdminEmail,
            password = "definitely-the-wrong-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"nobody-{Guid.NewGuid():N}@kartly.local",
            password = "Whatever123!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Criterion: [Authorize(Roles="Admin")] returns 403 for a Customer, 200/201 for an Admin ---

    [Fact]
    public async Task AdminOnlyEndpoint_WithCustomerToken_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAsync(client, role: "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/products", new { name = "Widget", price = 9.99m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_WithAdminToken_Succeeds()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, KartlyDbInitializer.DefaultAdminEmail, KartlyDbInitializer.DefaultAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/products", new { name = "Widget", price = 9.99m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new { name = "Widget", price = 9.99m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Criterion: Passwords are hashed, never stored in plaintext ---

    [Fact]
    public async Task RegisteredPassword_IsStoredHashed_NotPlaintext()
    {
        var client = _factory.CreateClient();
        const string password = "Sup3rSecret!";
        var email = $"hash-check-{Guid.NewGuid():N}@kartly.local";
        await RegisterAsync(client, role: "Customer", email: email, password: password);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.False(string.IsNullOrEmpty(user!.PasswordHash));
        Assert.DoesNotContain(password, user.PasswordHash);
        // The stored hash must actually verify against the original password.
        Assert.True(await userManager.CheckPasswordAsync(user, password));
    }

    // --- Criterion: At least one seeded Admin user exists for local dev ---

    [Fact]
    public async Task SeededAdminUser_ExistsWithAdminRole()
    {
        // Touch the client so the factory boots and the initializer runs.
        _ = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(KartlyDbInitializer.DefaultAdminEmail);

        Assert.NotNull(admin);
        Assert.Contains("Admin", await userManager.GetRolesAsync(admin!));
    }

    // --- helpers ---

    private static async Task<string> RegisterAsync(
        HttpClient client, string role, string? email = null, string password = "Passw0rd!")
    {
        email ??= $"user-{Guid.NewGuid():N}@kartly.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password, role });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }
}