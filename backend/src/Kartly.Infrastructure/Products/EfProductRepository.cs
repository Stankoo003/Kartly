using Kartly.Application.Products;
using Kartly.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Products;

/// <summary>
/// EF Core / PostgreSQL implementation of the product repository contract.
/// </summary>
public sealed class EfProductRepository(KartlyDbContext context) : IProductRepository
{
    public async Task<(IReadOnlyList<Product> Items, int Total)> GetPagedAsync(
        ProductQueryParameters query, CancellationToken ct = default)
    {
        var products = context.Products.AsNoTracking().AsQueryable();

        if (query.IsActive is { } isActive)
            products = products.Where(p => p.IsActive == isActive);

        if (query.IsFeatured is { } isFeatured)
            products = products.Where(p => p.IsFeatured == isFeatured);

        if (!string.IsNullOrWhiteSpace(query.Brand))
            products = products.Where(p => p.Brand == query.Brand);

        if (!string.IsNullOrWhiteSpace(query.Category))
            products = products.Where(p => p.Category == query.Category);

        if (query.MinPrice is { } min)
            products = products.Where(p => p.Price >= min);

        if (query.MaxPrice is { } max)
            products = products.Where(p => p.Price <= max);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, term) ||
                EF.Functions.ILike(p.Sku, term) ||
                (p.Brand != null && EF.Functions.ILike(p.Brand, term)));
        }

        products = (query.SortBy, query.SortDescending) switch
        {
            (ProductSortBy.Name, false) => products.OrderBy(p => p.Name),
            (ProductSortBy.Name, true) => products.OrderByDescending(p => p.Name),
            (ProductSortBy.Price, false) => products.OrderBy(p => p.Price),
            (ProductSortBy.Price, true) => products.OrderByDescending(p => p.Price),
            (ProductSortBy.CreatedAt, false) => products.OrderBy(p => p.CreatedAt),
            _ => products.OrderByDescending(p => p.CreatedAt),
        };

        var total = await products.CountAsync(ct);

        var items = await products
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Update(product);
        await context.SaveChangesAsync(ct);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        => context.Products.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), ct);

    public Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken ct = default)
        => context.Products.AnyAsync(p => p.Sku == sku && (excludeId == null || p.Id != excludeId), ct);
}
