namespace Kartly.Application.Settings;

/// <summary>The fixed set of supported currencies (ISO 4217). Mirrored on the frontend.</summary>
public static class Currencies
{
    public const string Rsd = "RSD";
    public const string Eur = "EUR";
    public const string Usd = "USD";
    public const string Gbp = "GBP";

    public static readonly IReadOnlyList<string> All = [Rsd, Eur, Usd, Gbp];

    /// <summary>True when <paramref name="value"/> is a supported currency (case-insensitive).</summary>
    public static bool IsValid(string? value) =>
        value is not null && All.Any(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase));
}
