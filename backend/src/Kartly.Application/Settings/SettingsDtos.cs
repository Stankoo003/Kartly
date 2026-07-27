using System.ComponentModel.DataAnnotations;

namespace Kartly.Application.Settings;

/// <summary>Public shape of the site settings. Readable by anyone (the storefront consumes it).</summary>
public sealed record SiteSettingsResponse(
    string SiteName,
    string ContactEmail,
    string Currency,
    string BannerTitle,
    string BannerSubtitle,
    DateTime UpdatedAt,
    // Not editable, hence absent from UpdateSiteSettingsRequest — settings updates are
    // full-replace, so exposing it there would let a client redenominate the whole catalogue.
    // The admin product screens need it to label price inputs before any rate is loaded.
    string BaseCurrency)
{
    public static SiteSettingsResponse FromEntity(SiteSettings s) =>
        new(s.SiteName, s.ContactEmail, s.Currency, s.BannerTitle, s.BannerSubtitle, s.UpdatedAt,
            Currencies.Base);
}

/// <summary>Full-replace payload for the settings record. Admin only.</summary>
public sealed record UpdateSiteSettingsRequest(
    [Required]
    [MaxLength(100)]
    string SiteName,

    [Required]
    [EmailAddress(ErrorMessage = "Contact email must be a valid email address.")]
    [MaxLength(200)]
    string ContactEmail,

    [Required]
    [MaxLength(3)]
    string Currency,

    [Required]
    [MaxLength(100)]
    string BannerTitle,

    [Required]
    [MaxLength(200)]
    string BannerSubtitle) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Currencies.IsValid(Currency))
            yield return new ValidationResult(
                $"Currency must be one of: {string.Join(", ", Currencies.All)}.", [nameof(Currency)]);
    }
}
