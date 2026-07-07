using Kartly.Application.Products;
using Kartly.Infrastructure.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Kartly.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers data-access implementations for the Application contracts.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Singleton so in-memory data survives across requests during dev.
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        return services;
    }
}
