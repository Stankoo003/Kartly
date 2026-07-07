namespace Kartly.Application.Products;

/// <summary>Domain entity — a product in the catalog.</summary>
public sealed class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
