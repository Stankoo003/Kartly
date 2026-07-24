using System.ComponentModel.DataAnnotations;

namespace Kartly.Application.Settings;

/// <summary>Public shape of the site settings. Readable by anyone (the storefront consumes it).</summary>
public sealed record SiteSettingsResponse(
    string SiteName,
    string ContactEmail,
    string Currency,
    DateTime UpdatedAt)
{
    public static SiteSettingsResponse FromEntity(SiteSettings s) =>
        new(s.SiteName, s.ContactEmail, s.Currency, s.UpdatedAt);
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
    string Currency) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Currencies.IsValid(Currency))
            yield return new ValidationResult(
                $"Currency must be one of: {string.Join(", ", Currencies.All)}.", [nameof(Currency)]);
    }
}
