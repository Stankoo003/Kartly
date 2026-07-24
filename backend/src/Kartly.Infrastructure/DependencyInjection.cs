using System.Security.Claims;
using System.Text;
using Kartly.Application.Auth;
using Kartly.Application.Products;
using Kartly.Application.Settings;
using Kartly.Application.Users;
using Kartly.Infrastructure.Auth;
using Kartly.Infrastructure.Products;
using Kartly.Infrastructure.Users;
using SettingsService = Kartly.Infrastructure.Settings.SettingsService;
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
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ISettingsService, SettingsService>();

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

                // Tokens are long-lived (no refresh), so re-check the user on every request:
                // reject deactivated/deleted accounts and refresh role claims from the DB so a
                // role change or deactivation is enforced on the next request (no re-login needed).
                // Cost: ~2 DB reads per authenticated request — acceptable for this app's scale.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        var userManager = ctx.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();

                        var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? ctx.Principal?.FindFirstValue("sub");
                        var user = userId is null ? null : await userManager.FindByIdAsync(userId);

                        if (user is null || !user.IsActive)
                        {
                            ctx.Fail("Account is inactive or no longer exists.");
                            return;
                        }

                        if (ctx.Principal!.Identity is ClaimsIdentity identity)
                        {
                            foreach (var stale in identity.FindAll(identity.RoleClaimType).ToList())
                                identity.RemoveClaim(stale);
                            foreach (var role in await userManager.GetRolesAsync(user))
                                identity.AddClaim(new Claim(identity.RoleClaimType, role));
                        }
                    },
                };
            });
    }
}
