using Kartly.Application.Settings;

namespace Kartly.Application.Orders;

/// <summary>A placed order. Prices are snapshotted per line at placement time.</summary>
public sealed class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string ContactEmail { get; set; }
    public required string ContactPhone { get; set; }

    public required string ShipFirstName { get; set; }
    public required string ShipLastName { get; set; }
    public required string ShipAddress { get; set; }
    public required string ShipCity { get; set; }
    public required string ShipZip { get; set; }
    public required string ShipCountry { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Order total — the sum of the line totals, snapshotted at placement.</summary>
    public decimal Total { get; set; }

    /// <summary>
    /// ISO 4217 code the amounts on this order are denominated in — the base currency as of
    /// placement. Snapshotted so changing the site's display currency can never restate what a
    /// past order was worth. Lines deliberately have no currency of their own: they belong to
    /// exactly one order, so a per-line column could only ever disagree with this one.
    /// </summary>
    public string Currency { get; set; } = Currencies.Base;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderLineItem> Lines { get; set; } = [];
}

/// <summary>A single line in an order — snapshots the product name and the price paid.</summary>
public sealed class OrderLineItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Product name at purchase time (snapshot).</summary>
    public required string ProductName { get; set; }

    /// <summary>The unit price actually charged (snapshot).</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary><see cref="UnitPrice"/> × <see cref="Quantity"/>, snapshotted.</summary>
    public decimal LineTotal { get; set; }
}
