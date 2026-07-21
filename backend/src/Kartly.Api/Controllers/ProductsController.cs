using Kartly.Application.Auth;
using Kartly.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // any authenticated user (Customer or Admin) may browse the catalog
[Produces("application/json")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>Returns a filtered, sorted, paginated list of products.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
        [FromQuery] ProductQueryParameters query, CancellationToken ct)
        => Ok(await productService.GetProductsAsync(query, ct));

    /// <summary>Returns a single product by id.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await productService.GetProductByIdAsync(id, ct));
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Creates a product. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await productService.CreateProductAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (ProductConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Replaces all fields of an existing product. Admin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await productService.UpdateProductAsync(id, request, ct));
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ProductConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Soft-deletes a product (marks it inactive). Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await productService.DeleteProductAsync(id, ct);
            return NoContent();
        }
        catch (ProductNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
