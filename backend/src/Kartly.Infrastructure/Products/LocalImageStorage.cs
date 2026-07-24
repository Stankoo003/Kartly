using Kartly.Application.Products;

namespace Kartly.Infrastructure.Products;

/// <summary>
/// Stores uploaded images on the API's local disk under <c>{mediaRoot}/uploads</c> and serves
/// them from the <c>/api/media/uploads</c> static-file route configured in Program.cs.
/// </summary>
public sealed class LocalImageStorage(string mediaRootPath) : IImageStorage
{
    private const string UploadsFolder = "uploads";
    private const string PublicBase = "/api/media/uploads";

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct = default)
    {
        var uploadsDir = Path.Combine(mediaRootPath, UploadsFolder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using (var file = File.Create(fullPath))
            await content.CopyToAsync(file, ct);

        return $"{PublicBase}/{fileName}";
    }
}
