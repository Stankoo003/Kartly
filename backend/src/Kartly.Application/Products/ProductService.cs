namespace Kartly.Application.Products;

/// <summary>Application/business logic. Depends only on the repository contract.</summary>
public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetProductsAsync(ProductQueryParameters query, CancellationToken ct = default);
    Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteProductAsync(Guid id, CancellationToken ct = default);
}

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public async Task<PagedResult<ProductResponse>> GetProductsAsync(
        ProductQueryParameters query, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetPagedAsync(query, ct);
        var responses = items.Select(ProductResponse.FromEntity).ToList();
        return new PagedResult<ProductResponse>(responses, query.Page, query.PageSize, total);
    }

    public async Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct)
            ?? throw new ProductNotFoundException(id);
        return ProductResponse.FromEntity(product);
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(name) : Slugify(request.Slug);
        var sku = string.IsNullOrWhiteSpace(request.Sku)
            ? $"SKU-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
            : request.Sku.Trim();

        if (await repository.SlugExistsAsync(slug, null, ct))
            throw new ProductConflictException($"A product with slug '{slug}' already exists.");
        if (await repository.SkuExistsAsync(sku, null, ct))
            throw new ProductConflictException($"A product with sku '{sku}' already exists.");

        var now = DateTime.UtcNow;
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
            CreatedAt = now,
            UpdatedAt = now,
        };
        await repository.AddAsync(product, ct);
        return ProductResponse.FromEntity(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(
        Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct)
            ?? throw new ProductNotFoundException(id);

        var slug = Slugify(request.Slug);
        var sku = request.Sku.Trim();

        if (await repository.SlugExistsAsync(slug, id, ct))
            throw new ProductConflictException($"A product with slug '{slug}' already exists.");
        if (await repository.SkuExistsAsync(sku, id, ct))
            throw new ProductConflictException($"A product with sku '{sku}' already exists.");

        product.Name = request.Name.Trim();
        product.Slug = slug;
        product.Sku = sku;
        product.Brand = request.Brand?.Trim();
        product.Model = request.Model?.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.StockQuantity = request.StockQuantity;
        product.WarrantyMonths = request.WarrantyMonths;
        product.IsFeatured = request.IsFeatured;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(product, ct);
        return ProductResponse.FromEntity(product);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct)
            ?? throw new ProductNotFoundException(id);

        // Soft delete — keep the row (order history / future FKs) but hide it from listings.
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(product, ct);
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
