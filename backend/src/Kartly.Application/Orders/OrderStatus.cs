namespace Kartly.Application.Orders;

/// <summary>The lifecycle states an order moves through.</summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled,
}

/// <summary>
/// The order lifecycle state machine. Transitions are enforced server-side; anything not listed
/// here is illegal and rejected.
/// </summary>
public static class OrderStatusRules
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Allowed =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
            [OrderStatus.Shipped] = [],
            [OrderStatus.Cancelled] = [],
        };

    /// <summary>True if <paramref name="to"/> is a legal next state from <paramref name="from"/>.</summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to) => Allowed[from].Contains(to);

    /// <summary>The legal next states from the given status (for surfacing to the admin UI).</summary>
    public static IReadOnlyList<OrderStatus> NextStates(OrderStatus from) => Allowed[from];
}
