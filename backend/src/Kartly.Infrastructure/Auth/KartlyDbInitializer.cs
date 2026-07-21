using Kartly.Application.Auth;
using Kartly.Infrastructure.Products;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kartly.Infrastructure.Auth;

/// <summary>
/// Applies pending migrations and seeds the roles plus a default admin account
/// so the app is usable immediately after a fresh <c>dotnet run</c>.
/// </summary>
public static class KartlyDbInitializer
{
    public const string DefaultAdminEmail = "admin@kartly.local";
    public const string DefaultAdminPassword = "Admin123!";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<KartlyDbContext>();
        await context.Database.MigrateAsync(ct);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(DefaultAdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(admin, DefaultAdminPassword);
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        await ProductSeeder.SeedAsync(context, ct);
    }
}
