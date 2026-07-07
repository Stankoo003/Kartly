namespace Kartly.Application.Products;

/// <summary>Application/business logic. Depends only on the repository contract.</summary>
public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default);
    Task<Product> CreateProductAsync(string name, decimal price, CancellationToken ct = default);
}

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default)
        => repository.GetAllAsync(ct);

    public async Task<Product> CreateProductAsync(string name, decimal price, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

        var product = new Product { Name = name.Trim(), Price = price };
        await repository.AddAsync(product, ct);
        return product;
    }
}
