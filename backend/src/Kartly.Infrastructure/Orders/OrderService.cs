using Kartly.Application.Orders;
using Kartly.Application.Products; // PagedResult<T>
using Kartly.Application.Settings; // Currencies.Base
using Kartly.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Orders;

/// <summary>
/// EF Core implementation of the order workflow: server-side price/stock re-validation, per-line
/// price snapshots, atomic stock decrement, and the lifecycle state machine.
/// </summary>
public sealed class OrderService(KartlyDbContext context) : IOrderService
{
    public async Task<OrderResponse> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
            throw new OrderValidationException("Your cart is empty.");

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await context.Products
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var order = new Order
        {
            ContactEmail = request.ContactEmail.Trim(),
            ContactPhone = request.ContactPhone.Trim(),
            ShipFirstName = request.ShipFirstName.Trim(),
            ShipLastName = request.ShipLastName.Trim(),
            ShipAddress = request.ShipAddress.Trim(),
            ShipCity = request.ShipCity.Trim(),
            ShipZip = request.ShipZip.Trim(),
            ShipCountry = request.ShipCountry.Trim(),
            Status = OrderStatus.Pending,
            Currency = Currencies.Base, // snapshot: what these amounts are denominated in, for good
        };

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                throw new OrderValidationException("A product in your cart is no longer available. Please review your cart.");

            // "Price no longer matches" — the client's expected price differs from the current one.
            // Both sides are base-currency amounts: the storefront converts only when rendering,
            // never before POSTing, so this compares like with like whatever currency is on display.
            if (product.Price != item.UnitPrice)
                throw new OrderValidationException(
                    $"The price for '{product.Name}' has changed. Please review your cart and try again.");

            // "Stock no longer matches" — not enough on hand.
            if (product.StockQuantity < item.Quantity)
                throw new OrderValidationException(
                    $"Only {product.StockQuantity} of '{product.Name}' left in stock.");

            var lineTotal = product.Price * item.Quantity;
            order.Lines.Add(new OrderLineItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,   // snapshot
                UnitPrice = product.Price,    // snapshot (server price)
                Quantity = item.Quantity,
                LineTotal = lineTotal,
            });

            product.StockQuantity -= item.Quantity; // decrement stock
        }

        order.Total = order.Lines.Sum(l => l.LineTotal);

        context.Orders.Add(order);
        await context.SaveChangesAsync(ct); // stock decrement + order commit atomically
        await tx.CommitAsync(ct);

        return OrderResponse.FromEntity(order);
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await context.Orders
            .Include(o => o.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new OrderNotFoundException(id);
        return OrderResponse.FromEntity(order);
    }

    public async Task<PagedResult<OrderSummaryResponse>> GetPagedAsync(
        OrderQueryParameters query, CancellationToken ct = default)
    {
        var orders = context.Orders.Include(o => o.Lines).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var status))
        {
            orders = orders.Where(o => o.Status == status);
        }

        orders = orders.OrderByDescending(o => o.CreatedAt);

        var total = await orders.CountAsync(ct);
        var items = await orders
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var responses = items.Select(OrderSummaryResponse.FromEntity).ToList();
        return new PagedResult<OrderSummaryResponse>(responses, query.Page, query.PageSize, total);
    }

    public async Task<OrderResponse> UpdateStatusAsync(Guid id, OrderStatus target, CancellationToken ct = default)
    {
        var order = await context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new OrderNotFoundException(id);

        if (!OrderStatusRules.CanTransition(order.Status, target))
            throw new InvalidOrderTransitionException(order.Status, target);

        if (target == OrderStatus.Cancelled)
        {
            // Return the reserved quantities to stock.
            var ids = order.Lines.Select(l => l.ProductId).ToList();
            var products = await context.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
            foreach (var line in order.Lines)
            {
                var product = products.FirstOrDefault(p => p.Id == line.ProductId);
                if (product is not null) product.StockQuantity += line.Quantity;
            }
        }

        order.Status = target;
        order.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return OrderResponse.FromEntity(order);
    }
}
