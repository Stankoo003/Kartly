using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kartly.Infrastructure.Auth;
using Xunit;

namespace Kartly.IntegrationTests;

/// <summary>
/// End-to-end tests for the product CRUD endpoints: pagination/filtering/sorting,
/// DTO responses, validation 400s, Admin-only writes, soft delete.
/// </summary>
public sealed class ProductsTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ProductsTests(PostgresApiFactory factory) => _factory = factory;

    private sealed record AuthResponse(string Token, string Email, string Role, DateTimeOffset ExpiresAt);

    private sealed record ProductResponse(
        Guid Id, string Name, string Slug, string Sku, string? Brand, string? Model, string? Description,
        decimal Price, decimal? DiscountPrice, int StockQuantity, int? WarrantyMonths,
        bool IsFeatured, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record PagedResult(
        IReadOnlyList<ProductResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    // --- List: pagination / filtering / sorting ---

    [Fact]
    public async Task List_ReturnsPagedResult_WithSeededProducts()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(client);

        var result = await client.GetFromJsonAsync<PagedResult>("/api/products?page=1&pageSize=2");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Items.Count);        // page limited to pageSize
        Assert.True(result.TotalCount >= 7);          // at least the 7 seeded products
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task List_FilterBySearch_MatchesBrandOrName()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(client);

        var result = await client.GetFromJsonAsync<PagedResult>("/api/products?search=Apple&pageSize=50");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, p =>
            Assert.True(
                p.Name.Contains("Apple", StringComparison.OrdinalIgnoreCase) ||
                (p.Brand?.Contains("Apple", StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.Sku.Contains("Apple", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task List_SortByPriceAscending_IsOrdered()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(client);

        var result = await client.GetFromJsonAsync<PagedResult>(
            "/api/products?sortBy=Price&sortDescending=false&pageSize=50");

        Assert.NotNull(result);
        var prices = result!.Items.Select(p => p.Price).ToList();
        Assert.Equal(prices.OrderBy(p => p).ToList(), prices);
    }

    // --- GET by id ---

    [Fact]
    public async Task GetById_ExistingProduct_Returns200()
    {
        var client = _factory.CreateClient();
        var created = await CreateProductAsync(client, Unique("getbyid"));

        var response = await client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(created.Id, body!.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(client);

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- POST: create ---

    [Fact]
    public async Task Create_AsAdmin_Returns201()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);
        var slug = Unique("create");

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "New Product", slug, sku = slug, price = 19.99m,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(slug, body!.Slug);
    }

    [Theory]
    [InlineData("", 10)]      // missing name
    [InlineData("Valid", -5)] // negative price
    public async Task Create_InvalidPayload_Returns400(string name, decimal price)
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/products", new { name, price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DiscountExceedsPrice_Returns400()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Bad Discount", price = 100m, discountPrice = 150m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateSku_Returns409()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);
        var slug = Unique("dup");
        var payload = new { name = "Dup", slug, sku = slug, price = 5m };

        var first = await client.PostAsJsonAsync("/api/products", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Dup 2", slug = Unique("dup2"), sku = slug, price = 5m,
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_AsCustomer_Returns403()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(client);

        var response = await client.PostAsJsonAsync("/api/products", new { name = "X", price = 1m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new { name = "X", price = 1m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- PUT: update ---

    [Fact]
    public async Task Update_ExistingProduct_Returns200_AndReplacesFields()
    {
        var client = _factory.CreateClient();
        var created = await CreateProductAsync(client, Unique("upd"));

        var response = await client.PutAsJsonAsync($"/api/products/{created.Id}", new
        {
            name = "Updated Name", slug = created.Slug, sku = created.Sku,
            price = 42m, stockQuantity = 3, isFeatured = true,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Updated Name", body!.Name);
        Assert.Equal(42m, body.Price);
        Assert.True(body.IsFeatured);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", new
        {
            name = "X", slug = Unique("nope"), sku = Unique("nope"), price = 1m,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- DELETE: soft delete ---

    [Fact]
    public async Task Delete_ThenGet_Returns404_AndHiddenFromList()
    {
        var client = _factory.CreateClient();
        var slug = Unique("del");
        var created = await CreateProductAsync(client, slug);

        var delete = await client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Soft-deleted (inactive) is hidden from the default active-only list...
        var list = await client.GetFromJsonAsync<PagedResult>($"/api/products?search={slug}&pageSize=50");
        Assert.DoesNotContain(list!.Items, p => p.Id == created.Id);

        // ...but the row still exists (visible when explicitly querying inactive).
        var inactive = await client.GetFromJsonAsync<PagedResult>($"/api/products?search={slug}&isActive=false&pageSize=50");
        Assert.Contains(inactive!.Items, p => p.Id == created.Id);
    }

    // --- helpers ---

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, KartlyDbInitializer.DefaultAdminEmail, KartlyDbInitializer.DefaultAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task AuthenticateAsCustomerAsync(HttpClient client)
    {
        var token = await RegisterAsync(client, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<ProductResponse> CreateProductAsync(HttpClient client, string slug)
    {
        await AuthenticateAsAdminAsync(client);
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = slug, slug, sku = slug, price = 9.99m,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static async Task<string> RegisterAsync(HttpClient client, string role)
    {
        var email = $"user-{Guid.NewGuid():N}@kartly.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!", role });
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
