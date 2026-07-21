namespace Kartly.Application.Products;

/// <summary>
/// Data-access contract. Defined in the Application layer, implemented in
/// Infrastructure — so the domain never depends on how/where data is stored.
/// </summary>
public interface IProductRepository
{
    /// <summary>Returns a filtered, sorted page of products plus the total match count.</summary>
    Task<(IReadOnlyList<Product> Items, int Total)> GetPagedAsync(
        ProductQueryParameters query, CancellationToken ct = default);

    /// <summary>Loads a tracked product (usable for updates), or null if missing.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>True if another product already uses this slug (optionally excluding one id).</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>True if another product already uses this sku (optionally excluding one id).</summary>
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken ct = default);
}
