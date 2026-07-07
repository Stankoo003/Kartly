using System.Collections.Concurrent;
using Kartly.Application.Products;

namespace Kartly.Infrastructure.Products;

/// <summary>
/// In-memory implementation of the repository contract. Swap this for an
/// EF Core / SQL implementation later without touching the Application layer.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _store = new();

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Product>>(_store.Values.ToList());

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(Product product, CancellationToken ct = default)
    {
        _store[product.Id] = product;
        return Task.CompletedTask;
    }
}
