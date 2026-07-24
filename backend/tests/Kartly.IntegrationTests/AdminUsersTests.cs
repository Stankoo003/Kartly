using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kartly.Infrastructure.Auth;
using Xunit;

namespace Kartly.IntegrationTests;

/// <summary>
/// End-to-end tests for the admin user-management endpoints: listing/search/paging, role
/// changes enforced on the next request, activate/deactivate, the "can't lock yourself out"
/// guards, and that password hashes are never returned.
/// </summary>
public sealed class AdminUsersTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AdminUsersTests(PostgresApiFactory factory) => _factory = factory;

    private sealed record AuthResponse(string Token, string Email, string Role, DateTimeOffset ExpiresAt);
    private sealed record UserResponse(string Id, string Email, string Role, bool IsActive);
    private sealed record PagedUsers(IReadOnlyList<UserResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    // --- listing / search / auth ---

    [Fact]
    public async Task List_AsAdmin_ReturnsPagedUsers()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var result = await client.GetFromJsonAsync<PagedUsers>("/api/admin/users?page=1&pageSize=10");

        Assert.NotNull(result);
        Assert.Equal(1, result!.Page);
        Assert.Contains(result.Items, u => u.Email == KartlyDbInitializer.DefaultAdminEmail && u.Role == "Admin");
    }

    [Fact]
    public async Task List_AsCustomer_Returns403()
    {
        var client = _factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, role: "Customer");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_SearchByEmail_FiltersToMatch()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail("search");
        await RegisterAsync(client, email, role: "Customer");
        await AuthenticateAsAdminAsync(client);

        var result = await client.GetFromJsonAsync<PagedUsers>($"/api/admin/users?search={email}&pageSize=50");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, u => Assert.Contains(email, u.Email, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task List_ResponseNeverContainsPasswordHash()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var raw = await client.GetStringAsync("/api/admin/users?pageSize=50");

        Assert.DoesNotContain("passwordhash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securitystamp", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"password\"", raw, StringComparison.OrdinalIgnoreCase);
    }

    // --- change role + per-request enforcement ---

    [Fact]
    public async Task ChangeRole_PromotingCustomer_IsEnforcedOnNextRequest()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);

        // A customer with their own long-lived token.
        var customer = _factory.CreateClient();
        var email = UniqueEmail("promote");
        var token = await RegisterAsync(customer, email, role: "Customer");
        customer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Admin-only create is refused while they're a Customer.
        var before = await customer.PostAsJsonAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

        // Promote them to Admin.
        var id = await GetUserIdByEmailAsync(admin, email);
        var change = await admin.PutAsJsonAsync($"/api/admin/users/{id}/role", new { role = "Admin" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        // The SAME (unchanged) token now grants admin access — enforced on the next request.
        var after = await customer.PostAsJsonAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Created, after.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_MultipleAdmins_DemoteOther_Succeeds()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);

        var email = UniqueEmail("secondadmin");
        await RegisterAsync(admin, email, role: "Admin"); // registration carries no auth; fine
        var id = await GetUserIdByEmailAsync(admin, email);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{id}/role", new { role = "Customer" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Customer", body!.Role);
    }

    [Fact]
    public async Task ChangeRole_UnknownUser_Returns404()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/role", new { role = "Admin" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- last-admin / self guards ---

    [Fact]
    public async Task ChangeRole_SelfDemotion_IsBlocked()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);
        var selfId = await GetUserIdByEmailAsync(client, KartlyDbInitializer.DefaultAdminEmail);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{selfId}/role", new { role = "Customer" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SetActive_SelfDeactivation_IsBlocked()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);
        var selfId = await GetUserIdByEmailAsync(client, KartlyDbInitializer.DefaultAdminEmail);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{selfId}/active", new { isActive = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // --- activate / deactivate enforcement ---

    [Fact]
    public async Task Deactivate_RejectsExistingToken_AndBlocksLogin_UntilReactivated()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);

        var customer = _factory.CreateClient();
        var email = UniqueEmail("deact");
        var password = "Passw0rd!";
        var token = await RegisterAsync(customer, email, password, "Customer");
        customer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Works while active.
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/products")).StatusCode);

        // Deactivate.
        var id = await GetUserIdByEmailAsync(admin, email);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync($"/api/admin/users/{id}/active", new { isActive = false })).StatusCode);

        // Existing token is now rejected, and they cannot obtain a new one.
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await customer.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);

        // Reactivate → login works again.
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync($"/api/admin/users/{id}/active", new { isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await customer.PostAsJsonAsync("/api/auth/login", new { email, password })).StatusCode);
    }

    // --- helpers ---

    private static object NewProduct() => new
    {
        name = "Enforcement probe", slug = Guid.NewGuid().ToString("N"), sku = Guid.NewGuid().ToString("N"),
        category = "Accessories", price = 1m,
    };

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@kartly.local";

    private async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, KartlyDbInitializer.DefaultAdminEmail, KartlyDbInitializer.DefaultAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task RegisterAndAuthenticateAsync(HttpClient client, string role)
    {
        var token = await RegisterAsync(client, UniqueEmail("user"), role: role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> GetUserIdByEmailAsync(HttpClient adminClient, string email)
    {
        var result = await adminClient.GetFromJsonAsync<PagedUsers>($"/api/admin/users?search={email}&pageSize=50");
        var user = result!.Items.Single(u => u.Email == email);
        return user.Id;
    }

    private static async Task<string> RegisterAsync(
        HttpClient client, string email, string password = "Passw0rd!", string? role = null)
    {
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
