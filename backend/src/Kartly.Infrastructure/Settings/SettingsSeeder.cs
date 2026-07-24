using Kartly.Application.Settings;
using Kartly.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Settings;

/// <summary>
/// Ensures the single site-settings row exists after a fresh <c>dotnet run</c>.
/// Idempotent — the <c>AnyAsync</c> guard prevents a second row.
/// </summary>
public static class SettingsSeeder
{
    public static async Task SeedAsync(KartlyDbContext context, CancellationToken ct = default)
    {
        if (await context.SiteSettings.AnyAsync(ct))
            return;

        context.SiteSettings.Add(SiteSettings.CreateDefault());
        await context.SaveChangesAsync(ct);
    }
}
