using Kartly.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Kartly.Application;

public static class DependencyInjection
{
    /// <summary>Registers Application-layer services (business logic).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
