using System.ComponentModel.DataAnnotations;

namespace Kartly.Application.Orders;

/// <summary>A requested line: the product, quantity, and the unit price the client expects to pay.</summary>
public sealed record OrderLineRequest(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice);

/// <summary>Checkout payload: contact + shipping details and the cart lines. No payment is collected.</summary>
public sealed record PlaceOrderRequest(
    [Required, EmailAddress, MaxLength(200)] string ContactEmail,
    [Required, MaxLength(40)] string ContactPhone,
    [Required, MaxLength(100)] string ShipFirstName,
    [Required, MaxLength(100)] string ShipLastName,
    [Required, MaxLength(200)] string ShipAddress,
    [Required, MaxLength(100)] string ShipCity,
    [Required, MaxLength(20)] string ShipZip,
    [Required, MaxLength(100)] string ShipCountry,
    [Required, MinLength(1)] IReadOnlyList<OrderLineRequest> Items);

public sealed record OrderLineResponse(
    Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal)
{
    public static OrderLineResponse FromEntity(OrderLineItem l) =>
        new(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity, l.LineTotal);
}

/// <summary>Full order shape returned on placement and to the admin detail view.</summary>
public sealed record OrderResponse(
    Guid Id,
    string ContactEmail,
    string ContactPhone,
    string ShipFirstName,
    string ShipLastName,
    string ShipAddress,
    string ShipCity,
    string ShipZip,
    string ShipCountry,
    string Status,
    decimal Total,
    // The currency Total and every line amount are in. Clients must render this order in its own
    // currency, never in the site's current display currency.
    string Currency,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse FromEntity(Order o) => new(
        o.Id, o.ContactEmail, o.ContactPhone, o.ShipFirstName, o.ShipLastName, o.ShipAddress,
        o.ShipCity, o.ShipZip, o.ShipCountry, o.Status.ToString(), o.Total, o.Currency, o.CreatedAt,
        o.Lines.Select(OrderLineResponse.FromEntity).ToList());
}

/// <summary>Compact order shape for the admin list.</summary>
public sealed record OrderSummaryResponse(
    Guid Id, string ContactEmail, string Status, decimal Total, string Currency, int ItemCount,
    DateTime CreatedAt)
{
    public static OrderSummaryResponse FromEntity(Order o) =>
        new(o.Id, o.ContactEmail, o.Status.ToString(), o.Total, o.Currency,
            o.Lines.Sum(l => l.Quantity), o.CreatedAt);
}

/// <summary>Body for the admin status-change endpoint.</summary>
public sealed record UpdateOrderStatusRequest([Required] string Status);

/// <summary>Query-string parameters for the admin orders list.</summary>
public sealed record OrderQueryParameters
{
    /// <summary>Optional status filter (Pending/Confirmed/Shipped/Cancelled).</summary>
    public string? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
