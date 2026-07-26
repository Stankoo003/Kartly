using Kartly.Application.Auth;
using Kartly.Application.Orders;
using Kartly.Application.Products; // PagedResult<T>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

/// <summary>Admin-only order administration: list, view, and advance the lifecycle.</summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminOrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Returns a paged, optionally status-filtered list of orders (newest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderSummaryResponse>>> GetAll(
        [FromQuery] OrderQueryParameters query, CancellationToken ct)
        => Ok(await orders.GetPagedAsync(query, ct));

    /// <summary>Returns a single order with its lines.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await orders.GetByIdAsync(id, ct));
        }
        catch (OrderNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Advances/changes an order's status. Illegal transitions are rejected with 409.</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        Guid id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var target))
            return BadRequest(new { error = $"Unknown status '{request.Status}'." });

        try
        {
            return Ok(await orders.UpdateStatusAsync(id, target, ct));
        }
        catch (OrderNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOrderTransitionException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
