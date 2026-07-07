using Kartly.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<Product>> GetAll(CancellationToken ct)
        => await productService.GetProductsAsync(ct);

    [HttpPost]
    public async Task<ActionResult<Product>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var product = await productService.CreateProductAsync(request.Name, request.Price, ct);
        return CreatedAtAction(nameof(GetAll), new { id = product.Id }, product);
    }
}

public sealed record CreateProductRequest(string Name, decimal Price);
