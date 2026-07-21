namespace Kartly.Application.Products;

/// <summary>
/// Data needed to create a product. <see cref="Slug"/> and <see cref="Sku"/> are
/// optional — when omitted they are derived from the name / generated automatically.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    decimal Price,
    string? Slug = null,
    string? Sku = null,
    string? Brand = null,
    string? Model = null,
    string? Description = null,
    decimal? DiscountPrice = null,
    int StockQuantity = 0,
    int? WarrantyMonths = null,
    bool IsFeatured = false,
    bool IsActive = true);

/// <summary>Application/business logic. Depends only on the repository contract.</summary>
public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default);
    Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
}

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct = default)
        => repository.GetAllAsync(ct);

    public async Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.", nameof(request));
        if (request.Price < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Price cannot be negative.");
        if (request.DiscountPrice is < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Discount price cannot be negative.");
        if (request.DiscountPrice > request.Price)
            throw new ArgumentException("Discount price cannot exceed price.", nameof(request));
        if (request.StockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Stock quantity cannot be negative.");
        if (request.WarrantyMonths is < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Warranty months cannot be negative.");

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(name) : Slugify(request.Slug);
        var sku = string.IsNullOrWhiteSpace(request.Sku)
            ? $"SKU-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
            : request.Sku.Trim();

        var product = new Product
        {
            Name = name,
            Slug = slug,
            Sku = sku,
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            StockQuantity = request.StockQuantity,
            WarrantyMonths = request.WarrantyMonths,
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
        };
        await repository.AddAsync(product, ct);
        return product;
    }

    private static string Slugify(string value)
    {
        var slug = new string(value.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
