using Kartly.Application.Auth;
using Kartly.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // any authenticated user (Customer or Admin) may browse the catalog
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<Product>> GetAll(CancellationToken ct)
        => await productService.GetProductsAsync(ct);

    [HttpPost]
    [Authorize(Roles = Roles.Admin)] // only admins may add products
    public async Task<ActionResult<Product>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var product = await productService.CreateProductAsync(request.Name, request.Price, ct);
        return CreatedAtAction(nameof(GetAll), new { id = product.Id }, product);
    }
}

public sealed record CreateProductRequest(string Name, decimal Price);
