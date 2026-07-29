using Kartly.Application.Auth;
using Kartly.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // any authenticated user (Customer or Admin) may browse the catalog
[Produces("application/json")]
public sealed class ProductsController(
    IProductService productService, IImageStorage imageStorage) : ControllerBase
{
    /// <summary>Returns a filtered, sorted, paginated list of products. Public — the storefront browses anonymously.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
        [FromQuery] ProductQueryParameters query, CancellationToken ct)
    {
        // Only admins may see soft-deleted (inactive) products; force active-only for everyone else,
        // so an anonymous caller can't surface them by passing isActive=false.
        if (!User.IsInRole(Roles.Admin))
            query = query with { IsActive = true };

        return Ok(await productService.GetProductsAsync(query, ct));
    }

    /// <summary>Returns a single product by id. Public — the storefront browses anonymously.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [AllowAnonymous]
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

    /// <summary>Uploads a product image to the API's local storage and returns its URL. Admin only.</summary>
    [HttpPost("images")]
    [Authorize(Roles = Roles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)] // hard backstop well above the 5 MB rule so oversize gets a clean 400
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        if (file.Length > ImageUploadRules.MaxSizeBytes)
            return BadRequest(new { error = $"Image exceeds the {ImageUploadRules.MaxSizeBytes / (1024 * 1024)} MB limit." });

        if (!ImageUploadRules.AllowedContentTypes.TryGetValue(file.ContentType, out var extension))
            return BadRequest(new { error = "Unsupported image type. Allowed types: JPEG, PNG, WebP." });

        await using var stream = file.OpenReadStream();
        var url = await imageStorage.SaveAsync(stream, extension, ct);
        return Created(url, new { url });
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
