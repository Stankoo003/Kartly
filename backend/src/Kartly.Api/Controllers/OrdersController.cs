using Kartly.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

/// <summary>Public order placement + read-back for the confirmation page. No payment is collected.</summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Places an order. Server re-validates prices/stock, snapshots prices and decrements stock.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        try
        {
            var order = await orders.PlaceAsync(request, ct);
            return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
        }
        catch (OrderValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Reads one order back (for the confirmation page). The id is an unguessable GUID.</summary>
    [HttpGet("{id:guid}", Name = "GetOrderById")]
    [AllowAnonymous]
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
}
