namespace Kartly.Application.Settings;

/// <summary>
/// Site-wide configuration. Deliberately a single row — <see cref="SingletonId"/> is the
/// only key ever used, so reads and writes always target the same record.
/// </summary>
public sealed class SiteSettings
{
    /// <summary>The one and only settings row id.</summary>
    public const int SingletonId = 1;

    public const string DefaultSiteName = "Kartly";
    public const string DefaultContactEmail = "contact@kartly.local";
    public const string DefaultCurrency = Currencies.Rsd;
    public const string DefaultBannerTitle = "Everything you love, in one cart.";
    public const string DefaultBannerSubtitle =
        "Curated products from the brands you trust — delivered fast, returned free.";

    public int Id { get; set; } = SingletonId;

    public required string SiteName { get; set; }

    public required string ContactEmail { get; set; }

    /// <summary>ISO 4217 code, one of <see cref="Currencies.All"/>.</summary>
    public required string Currency { get; set; }

    /// <summary>Headline shown in the storefront home hero.</summary>
    public required string BannerTitle { get; set; }

    /// <summary>Supporting line shown under the hero headline.</summary>
    public required string BannerSubtitle { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The out-of-the-box settings row, used by the seeder and as a self-healing fallback.</summary>
    public static SiteSettings CreateDefault() => new()
    {
        Id = SingletonId,
        SiteName = DefaultSiteName,
        ContactEmail = DefaultContactEmail,
        Currency = DefaultCurrency,
        BannerTitle = DefaultBannerTitle,
        BannerSubtitle = DefaultBannerSubtitle,
        UpdatedAt = DateTime.UtcNow,
    };
}
