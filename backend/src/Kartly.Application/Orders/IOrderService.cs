using Kartly.Application.Products; // PagedResult<T>

namespace Kartly.Application.Orders;

/// <summary>Order placement, retrieval and lifecycle. Implemented in Infrastructure.</summary>
public interface IOrderService
{
    /// <summary>
    /// Re-validates prices/stock, snapshots the price paid per line, creates the order (Pending),
    /// and decrements stock — atomically. Throws <see cref="OrderValidationException"/> on mismatch.
    /// </summary>
    Task<OrderResponse> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default);

    /// <summary>Returns one order, or throws <see cref="OrderNotFoundException"/>.</summary>
    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Admin: a paged, optionally status-filtered list, newest first.</summary>
    Task<PagedResult<OrderSummaryResponse>> GetPagedAsync(OrderQueryParameters query, CancellationToken ct = default);

    /// <summary>
    /// Admin: moves an order to <paramref name="target"/>. Throws
    /// <see cref="InvalidOrderTransitionException"/> for an illegal transition; restocks on cancel.
    /// </summary>
    Task<OrderResponse> UpdateStatusAsync(Guid id, OrderStatus target, CancellationToken ct = default);
}
