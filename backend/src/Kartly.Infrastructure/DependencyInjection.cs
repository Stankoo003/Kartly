using System.Text;
using Kartly.Application.Auth;
using Kartly.Application.Products;
using Kartly.Infrastructure.Auth;
using Kartly.Infrastructure.Products;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Kartly.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers data-access implementations plus Identity + JWT authentication.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        AddPersistenceAndIdentity(services, config);

        // Scoped to match the (scoped) EF Core DbContext lifetime.
        services.AddScoped<IProductRepository, EfProductRepository>();

        AddJwtAuthentication(services, config);

        return services;
    }

    private static void AddPersistenceAndIdentity(IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<KartlyDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Postgres")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<KartlyDbContext>();

        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = key,
                };
            });
    }
}
