namespace Kartly.Application.Products;

/// <summary>The fixed set of product categories. Mirrored on the frontend (product.models.ts).</summary>
public static class ProductCategories
{
    public const string Smartphones = "Smartphones";
    public const string Laptops = "Laptops";
    public const string Audio = "Audio";
    public const string Monitors = "Monitors";
    public const string Accessories = "Accessories";

    public static readonly IReadOnlyList<string> All =
        [Smartphones, Laptops, Audio, Monitors, Accessories];

    /// <summary>True when <paramref name="value"/> matches an allowed category (case-insensitive).</summary>
    public static bool IsValid(string? value) =>
        value is not null && All.Any(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase));
}
