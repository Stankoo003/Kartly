namespace Kartly.Application.Products;

/// <summary>
/// Data-access contract. Defined in the Application layer, implemented in
/// Infrastructure — so the domain never depends on how/where data is stored.
/// </summary>
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
}
