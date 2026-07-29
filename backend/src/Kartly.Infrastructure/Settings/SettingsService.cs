using Kartly.Application.Settings;
using Kartly.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Settings;

/// <summary>
/// Reads/updates the single site-settings row. If the row is somehow missing (e.g. a database
/// created before the seeder ran) it is created on demand, so callers always get settings back.
/// </summary>
public sealed class SettingsService(KartlyDbContext context) : ISettingsService
{
    public async Task<SiteSettingsResponse> GetAsync(CancellationToken ct = default)
        => SiteSettingsResponse.FromEntity(await GetOrCreateAsync(ct));

    public async Task<SiteSettingsResponse> UpdateAsync(
        UpdateSiteSettingsRequest request, CancellationToken ct = default)
    {
        var settings = await GetOrCreateAsync(ct);

        settings.SiteName = request.SiteName.Trim();
        settings.ContactEmail = request.ContactEmail.Trim();
        settings.Currency = request.Currency.Trim().ToUpperInvariant();
        settings.BannerTitle = request.BannerTitle.Trim();
        settings.BannerSubtitle = request.BannerSubtitle.Trim();
        settings.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return SiteSettingsResponse.FromEntity(settings);
    }

    private async Task<SiteSettings> GetOrCreateAsync(CancellationToken ct)
    {
        var settings = await context.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SiteSettings.SingletonId, ct);

        if (settings is not null)
            return settings;

        settings = SiteSettings.CreateDefault();
        context.SiteSettings.Add(settings);
        await context.SaveChangesAsync(ct);
        return settings;
    }
}
