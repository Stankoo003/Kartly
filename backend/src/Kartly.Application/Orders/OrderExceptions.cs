namespace Kartly.Application.Orders;

/// <summary>Raised when an order cannot be found. Mapped to HTTP 404.</summary>
public sealed class OrderNotFoundException(Guid id)
    : Exception($"Order '{id}' was not found.");

/// <summary>
/// Raised when a checkout is invalid — an empty cart, an unknown/inactive product, or a price/stock
/// that no longer matches. Mapped to HTTP 400 with a clear message.
/// </summary>
public sealed class OrderValidationException(string message) : Exception(message);

/// <summary>Raised on an illegal lifecycle transition. Mapped to HTTP 409.</summary>
public sealed class InvalidOrderTransitionException(OrderStatus from, OrderStatus to)
    : Exception($"Cannot move an order from {from} to {to}.");
