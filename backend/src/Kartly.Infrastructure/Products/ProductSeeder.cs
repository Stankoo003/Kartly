using Kartly.Application.Products;
using Kartly.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Products;

/// <summary>
/// Idempotently seeds a handful of sample products so the catalog is populated
/// after a fresh <c>dotnet run</c>. Fixed <see cref="Guid"/>s keep IDs stable
/// across runs; the <c>AnyAsync</c> guard prevents duplication.
/// </summary>
public static class ProductSeeder
{
    public static async Task SeedAsync(KartlyDbContext context, CancellationToken ct = default)
    {
        if (await context.Products.AnyAsync(ct))
            return;

        var seededAt = DateTime.UtcNow;

        var products = new[]
        {
            new Product
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Apple iPhone 15 Pro 256GB",
                Slug = "apple-iphone-15-pro-256gb",
                Sku = "APL-IP15P-256",
                Brand = "Apple",
                Model = "iPhone 15 Pro",
                Description = "Titanium design, A17 Pro chip, 256GB storage.",
                Price = 1299.00m,
                DiscountPrice = 1199.00m,
                StockQuantity = 25,
                WarrantyMonths = 24,
                IsFeatured = true,
                IsActive = true,
            },
            new Product
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Samsung Galaxy S24 Ultra 512GB",
                Slug = "samsung-galaxy-s24-ultra-512gb",
                Sku = "SAM-GS24U-512",
                Brand = "Samsung",
                Model = "Galaxy S24 Ultra",
                Description = "6.8\" Dynamic AMOLED, Snapdragon 8 Gen 3, S Pen included.",
                Price = 1399.00m,
                DiscountPrice = null,
                StockQuantity = 18,
                WarrantyMonths = 24,
                IsFeatured = true,
                IsActive = true,
            },
            new Product
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Dell XPS 13 Plus Laptop",
                Slug = "dell-xps-13-plus-laptop",
                Sku = "DEL-XPS13P",
                Brand = "Dell",
                Model = "XPS 13 Plus 9320",
                Description = "13.4\" OLED, Intel Core i7, 16GB RAM, 512GB SSD.",
                Price = 1599.00m,
                DiscountPrice = 1449.00m,
                StockQuantity = 12,
                WarrantyMonths = 24,
                IsFeatured = false,
                IsActive = true,
            },
            new Product
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Sony WH-1000XM5 Headphones",
                Slug = "sony-wh-1000xm5-headphones",
                Sku = "SNY-WH1000XM5",
                Brand = "Sony",
                Model = "WH-1000XM5",
                Description = "Industry-leading noise cancelling wireless headphones.",
                Price = 399.00m,
                DiscountPrice = 349.00m,
                StockQuantity = 40,
                WarrantyMonths = 12,
                IsFeatured = true,
                IsActive = true,
            },
            new Product
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Logitech MX Master 3S Mouse",
                Slug = "logitech-mx-master-3s-mouse",
                Sku = "LOG-MXM3S",
                Brand = "Logitech",
                Model = "MX Master 3S",
                Description = "Ergonomic wireless mouse with 8K DPI sensor.",
                Price = 109.00m,
                DiscountPrice = null,
                StockQuantity = 75,
                WarrantyMonths = 12,
                IsFeatured = false,
                IsActive = true,
            },
            new Product
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "LG UltraGear 27\" Gaming Monitor",
                Slug = "lg-ultragear-27-gaming-monitor",
                Sku = "LG-UG27-165",
                Brand = "LG",
                Model = "27GP850",
                Description = "27\" QHD Nano IPS, 165Hz, 1ms, G-Sync compatible.",
                Price = 449.00m,
                DiscountPrice = 399.00m,
                StockQuantity = 22,
                WarrantyMonths = 24,
                IsFeatured = false,
                IsActive = true,
            },
            new Product
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Name = "Anker 737 Power Bank 24000mAh",
                Slug = "anker-737-power-bank-24000mah",
                Sku = "ANK-737-24K",
                Brand = "Anker",
                Model = "737 PowerCore",
                Description = "140W output, 24000mAh capacity, USB-C fast charging.",
                Price = 149.00m,
                DiscountPrice = 129.00m,
                StockQuantity = 60,
                WarrantyMonths = 18,
                IsFeatured = false,
                IsActive = true,
            },
        };

        foreach (var product in products)
        {
            product.CreatedAt = seededAt;
            product.UpdatedAt = seededAt;
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync(ct);
    }
}
