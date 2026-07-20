namespace Kartly.Infrastructure.Auth;

/// <summary>Bound from the "Jwt" configuration section.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Kartly";
    public string Audience { get; set; } = "Kartly";

    /// <summary>Symmetric signing key. Keep out of source control in real deployments.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Access-token lifetime. We deliberately use a single long-lived token and no
    /// refresh token (see README "Refresh strategy") — so this is set fairly high.
    /// </summary>
    public int ExpiryHours { get; set; } = 8;
}
