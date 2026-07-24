using Kartly.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace Kartly.Infrastructure.Auth;

/// <summary>
/// Seeds a few sample customer accounts so the admin Users screen is populated after a
/// fresh <c>dotnet run</c>. Idempotent — each account is created only if its email is free.
/// </summary>
public static class UserSeeder
{
    /// <summary>Shared password for all seeded sample users (development only).</summary>
    public const string DefaultPassword = "Passw0rd!";

    private static readonly (string Email, string Role)[] SampleUsers =
    [
        ("mika.mitrovic@kartly.local", Roles.Customer),
        ("pera.peric@kartly.local", Roles.Customer),
        ("laza.lazic@kartly.local", Roles.Customer),
        ("ana.anic@kartly.local", Roles.Customer),
        ("zika.zikic@kartly.local", Roles.Customer),
        ("tiba.tibic@kartly.local", Roles.Customer),
    ];

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
    {
        foreach (var (email, role) in SampleUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true,
            };

            var created = await userManager.CreateAsync(user, DefaultPassword);
            if (created.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
