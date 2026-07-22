using System.ComponentModel.DataAnnotations;

namespace Kartly.Application.Products;

/// <summary>
/// Response shape returned to clients. The EF entity <see cref="Product"/> is
/// never exposed directly — always mapped through <see cref="FromEntity"/>.
/// </summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string Category,
    string? Brand,
    string? Model,
    string? Description,
    string? ImageUrl,
    decimal Price,
    decimal? DiscountPrice,
    int StockQuantity,
    int? WarrantyMonths,
    bool IsFeatured,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProductResponse FromEntity(Product p) => new(
        p.Id, p.Name, p.Slug, p.Sku, p.Category, p.Brand, p.Model, p.Description,
        p.ImageUrl, p.Price, p.DiscountPrice, p.StockQuantity, p.WarrantyMonths,
        p.IsFeatured, p.IsActive, p.CreatedAt, p.UpdatedAt);
}

/// <summary>
/// Data needed to create a product. <see cref="Slug"/> and <see cref="Sku"/> are
/// optional — when omitted they are derived from the name / generated automatically.
/// </summary>
public sealed record CreateProductRequest(
    [Required]
    [MaxLength(200)]
    string Name,

    [Required]
    [MaxLength(100)]
    string Category,

    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    decimal Price,

    [MaxLength(200)] string? Slug = null,
    [MaxLength(200)] string? Sku = null,
    [MaxLength(200)] string? Brand = null,
    [MaxLength(200)] string? Model = null,
    string? Description = null,

    [MaxLength(400)] string? ImageUrl = null,

    [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative.")]
    decimal? DiscountPrice = null,

    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
    int StockQuantity = 0,

    [Range(0, int.MaxValue, ErrorMessage = "Warranty months cannot be negative.")]
    int? WarrantyMonths = null,

    bool IsFeatured = false,
    bool IsActive = true) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ProductCategories.IsValid(Category))
            yield return new ValidationResult(
                $"Category must be one of: {string.Join(", ", ProductCategories.All)}.", [nameof(Category)]);

        if (DiscountPrice is { } discount && discount > Price)
            yield return new ValidationResult(
                "Discount price cannot exceed price.", [nameof(DiscountPrice)]);
    }
}

/// <summary>Full-replace payload for updating a product (PUT semantics).</summary>
public sealed record UpdateProductRequest(
    [Required]
    [MaxLength(200)]
    string Name,

    [Required]
    [MaxLength(200)]
    string Slug,

    [Required]
    [MaxLength(200)]
    string Sku,

    [Required]
    [MaxLength(100)]
    string Category,

    [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
    decimal Price,

    [MaxLength(200)] string? Brand = null,
    [MaxLength(200)] string? Model = null,
    string? Description = null,

    [MaxLength(400)] string? ImageUrl = null,

    [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative.")]
    decimal? DiscountPrice = null,

    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
    int StockQuantity = 0,

    [Range(0, int.MaxValue, ErrorMessage = "Warranty months cannot be negative.")]
    int? WarrantyMonths = null,

    bool IsFeatured = false,
    bool IsActive = true) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ProductCategories.IsValid(Category))
            yield return new ValidationResult(
                $"Category must be one of: {string.Join(", ", ProductCategories.All)}.", [nameof(Category)]);

        if (DiscountPrice is { } discount && discount > Price)
            yield return new ValidationResult(
                "Discount price cannot exceed price.", [nameof(DiscountPrice)]);
    }
}

/// <summary>Fields a product list query can be sorted by.</summary>
public enum ProductSortBy
{
    CreatedAt,
    Name,
    Price,
}

/// <summary>
/// Query-string parameters for the paginated product list: filtering, sorting, paging.
/// Bound via <c>[FromQuery]</c>.
/// </summary>
public sealed record ProductQueryParameters
{
    /// <summary>Free-text match against name, sku and brand.</summary>
    public string? Search { get; init; }

    /// <summary>Exact brand filter.</summary>
    public string? Brand { get; init; }

    public bool? IsFeatured { get; init; }

    /// <summary>Defaults to true so soft-deleted (inactive) products are hidden.</summary>
    public bool? IsActive { get; init; } = true;

    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; init; }

    public ProductSortBy SortBy { get; init; } = ProductSortBy.CreatedAt;

    public bool SortDescending { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>A single page of results plus paging metadata.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
