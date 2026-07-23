namespace Kartly.Application.Settings;

/// <summary>Reads and updates the single site-settings record.</summary>
public interface ISettingsService
{
    Task<SiteSettingsResponse> GetAsync(CancellationToken ct = default);

    Task<SiteSettingsResponse> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default);
}
