using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kartly.Application.Settings;
using Kartly.Infrastructure.Auth;
using Xunit;

namespace Kartly.IntegrationTests;

/// <summary>
/// End-to-end tests for checkout and the order lifecycle: order creation with price snapshots and
/// stock decrement, server-side price/stock re-validation, and the enforced state machine
/// (including at least one illegal transition).
/// </summary>
public sealed class OrdersTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public OrdersTests(PostgresApiFactory factory) => _factory = factory;

    private sealed record AuthResponse(string Token, string Email, string Role, DateTimeOffset ExpiresAt);
    private sealed record ProductResponse(Guid Id, string Slug, string Sku, decimal Price, int StockQuantity);
    private sealed record OrderLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
    private sealed record OrderResponse(
        Guid Id, string Status, decimal Total, string Currency, IReadOnlyList<OrderLine> Lines);

    // --- placement ---

    [Fact]
    public async Task Place_AsAnonymous_CreatesOrder_SnapshotsPrice_AndDecrementsStock()
    {
        var admin = _factory.CreateClient();
        var product = await CreateProductAsync(admin, price: 25m, stock: 10);

        var anon = _factory.CreateClient(); // no token — public checkout
        var response = await anon.PostAsJsonAsync("/api/orders", OrderPayload(product, qty: 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Pending", order!.Status);
        Assert.Equal(75m, order.Total);
        var line = Assert.Single(order.Lines);
        Assert.Equal(25m, line.UnitPrice);   // snapshotted price paid
        Assert.Equal(3, line.Quantity);
        Assert.Equal(75m, line.LineTotal);

        // Stock decremented from 10 → 7.
        var after = await GetProductAsync(anon, product.Id);
        Assert.Equal(7, after.StockQuantity);
    }

    [Fact]
    public async Task Place_WithStalePrice_Returns400_WithClearError()
    {
        var admin = _factory.CreateClient();
        var product = await CreateProductAsync(admin, price: 30m, stock: 5);

        // Client sends a price that no longer matches the current one.
        var anon = _factory.CreateClient();
        var payload = OrderPayload(product, qty: 1) with { Items = [new(product.Id, 1, 29m)] };
        var response = await anon.PostAsJsonAsync("/api/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains("price", body!.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Place_WithInsufficientStock_Returns400()
    {
        var admin = _factory.CreateClient();
        var product = await CreateProductAsync(admin, price: 15m, stock: 2);

        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/orders", OrderPayload(product, qty: 5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains("stock", body!.Error, StringComparison.OrdinalIgnoreCase);
    }

    // --- lifecycle ---

    [Fact]
    public async Task Admin_CanAdvance_Pending_To_Confirmed_To_Shipped()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);
        var product = await CreateProductAsync(admin, price: 10m, stock: 5);
        var orderId = await PlaceOrderAsync(product, qty: 1);

        Assert.Equal("Confirmed", await SetStatusAsync(admin, orderId, "Confirmed"));
        Assert.Equal("Shipped", await SetStatusAsync(admin, orderId, "Shipped"));
    }

    [Fact]
    public async Task Admin_IllegalTransition_IsRejected_With409()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);
        var product = await CreateProductAsync(admin, price: 10m, stock: 5);
        var orderId = await PlaceOrderAsync(product, qty: 1);

        // Pending → Shipped skips Confirmed — illegal.
        var skip = await admin.PutAsJsonAsync($"/api/admin/orders/{orderId}/status", new { status = "Shipped" });
        Assert.Equal(HttpStatusCode.Conflict, skip.StatusCode);

        // Shipped is terminal: once there, it cannot go back to Confirmed.
        await SetStatusAsync(admin, orderId, "Confirmed");
        await SetStatusAsync(admin, orderId, "Shipped");
        var back = await admin.PutAsJsonAsync($"/api/admin/orders/{orderId}/status", new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.Conflict, back.StatusCode);
    }

    [Fact]
    public async Task Cancel_RestocksTheReservedQuantities()
    {
        var admin = _factory.CreateClient();
        await AuthenticateAsAdminAsync(admin);
        var product = await CreateProductAsync(admin, price: 10m, stock: 8);
        var orderId = await PlaceOrderAsync(product, qty: 3); // stock 8 → 5

        Assert.Equal(5, (await GetProductAsync(admin, product.Id)).StockQuantity);

        Assert.Equal("Cancelled", await SetStatusAsync(admin, orderId, "Cancelled"));
        Assert.Equal(8, (await GetProductAsync(admin, product.Id)).StockQuantity); // restored
    }

    [Fact]
    public async Task Admin_StatusChange_RequiresAdmin_Returns403ForCustomer()
    {
        var admin = _factory.CreateClient();
        var product = await CreateProductAsync(admin, price: 10m, stock: 5);
        var orderId = await PlaceOrderAsync(product, qty: 1);

        var customer = _factory.CreateClient();
        await AuthenticateAsCustomerAsync(customer);
        var response = await customer.PutAsJsonAsync($"/api/admin/orders/{orderId}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- helpers ---

    [Fact]
    public async Task Place_SnapshotsTheBaseCurrency()
    {
        var admin = _factory.CreateClient();
        var product = await CreateProductAsync(admin, price: 40m, stock: 4);

        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/orders", OrderPayload(product, qty: 1));

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        // Recorded at placement so changing the site's display currency can never restate what a
        // past order was worth.
        Assert.Equal(Currencies.Base, order!.Currency);
    }

    /// <summary>
    /// Pins the core constraint of the currency feature at the API boundary: display conversion
    /// happens at render time only. If a client ever converts before POSTing — say it sends the
    /// RSD figure the shopper saw — the server must reject it, because UnitPrice is compared
    /// against Product.Price, which is a base-currency amount.
    /// </summary>
    [Fact]
    public async Task Place_WithConvertedUnitPrice_IsRejected()
    {
        var admin = _factory.CreateClient();
        var product = await CreateProductAsync(admin, price: 40m, stock: 4);

        var anon = _factory.CreateClient();
        var converted = product.Price * 117.4m; // as if the client sent RSD instead of EUR
        var payload = OrderPayload(product, qty: 1) with { Items = [new(product.Id, 1, converted)] };
        var response = await anon.PostAsJsonAsync("/api/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains("price", body!.Error, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ErrorBody(string Error);

    private sealed record OrderPayloadDto(
        string ContactEmail, string ContactPhone, string ShipFirstName, string ShipLastName,
        string ShipAddress, string ShipCity, string ShipZip, string ShipCountry,
        IReadOnlyList<LineDto> Items);
    private sealed record LineDto(Guid ProductId, int Quantity, decimal UnitPrice);

    private static OrderPayloadDto OrderPayload(ProductResponse p, int qty) => new(
        "buyer@kartly.test", "060123456", "Mika", "Mikic", "Cara Dusana 1", "Beograd", "11000", "Serbia",
        [new(p.Id, qty, p.Price)]);

    private async Task<Guid> PlaceOrderAsync(ProductResponse product, int qty)
    {
        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/orders", OrderPayload(product, qty));
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Id;
    }

    private static async Task<string> SetStatusAsync(HttpClient admin, Guid orderId, string status)
    {
        var response = await admin.PutAsJsonAsync($"/api/admin/orders/{orderId}/status", new { status });
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Status;
    }

    private async Task<ProductResponse> CreateProductAsync(HttpClient client, decimal price, int stock)
    {
        await AuthenticateAsAdminAsync(client);
        var slug = $"ord-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = slug, slug, sku = slug, category = "Accessories", price, stockQuantity = stock,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static async Task<ProductResponse> GetProductAsync(HttpClient client, Guid id)
        => (await client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}"))!;

    private async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, KartlyDbInitializer.DefaultAdminEmail, KartlyDbInitializer.DefaultAdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task AuthenticateAsCustomerAsync(HttpClient client)
    {
        var email = $"cust-{Guid.NewGuid():N}@kartly.local";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!", role = "Customer" });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<AuthResponse>();
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
