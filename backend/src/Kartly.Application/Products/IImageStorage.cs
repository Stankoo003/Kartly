namespace Kartly.Application.Products;

/// <summary>Persists product images to some backing store and returns their public URL.</summary>
public interface IImageStorage
{
    /// <summary>
    /// Saves <paramref name="content"/> under a generated file name with the given
    /// <paramref name="fileExtension"/> (including the leading dot) and returns the
    /// public URL path at which it can be retrieved.
    /// </summary>
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct = default);
}

/// <summary>Server-side constraints for product image uploads. Mirrored (loosely) on the client.</summary>
public static class ImageUploadRules
{
    /// <summary>Maximum accepted upload size, in bytes (5 MB).</summary>
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    /// <summary>Allowed content types mapped to their canonical file extension.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };
}
