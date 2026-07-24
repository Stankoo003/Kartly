namespace Kartly.Application.Products;

/// <summary>Domain entity — a product in the catalog.</summary>
public sealed class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>URL-friendly identifier, unique.</summary>
    public required string Slug { get; set; }

    /// <summary>Stock-keeping unit, unique.</summary>
    public required string Sku { get; set; }

    /// <summary>One of <see cref="ProductCategories.All"/>.</summary>
    public required string Category { get; set; }

    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Description { get; set; }

    /// <summary>Public URL of the product image (e.g. <c>/api/media/uploads/{id}.jpg</c>), or null.</summary>
    public string? ImageUrl { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }

    public int StockQuantity { get; set; }
    public int? WarrantyMonths { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
