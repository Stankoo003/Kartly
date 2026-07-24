using Kartly.Application.Products;
using Kartly.Application.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Auth;

/// <summary>
/// EF Core context backing ASP.NET Identity (users, roles, claims) plus the
/// product catalog, on PostgreSQL.
/// </summary>
public sealed class KartlyDbContext(DbContextOptions<KartlyDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(product =>
        {
            product.ToTable("products");

            product.HasKey(p => p.Id);
            product.Property(p => p.Id).HasColumnName("id");

            product.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            product.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            product.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(200).IsRequired();
            product.Property(p => p.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            product.Property(p => p.Brand).HasColumnName("brand").HasMaxLength(200);
            product.Property(p => p.Model).HasColumnName("model").HasMaxLength(200);
            product.Property(p => p.Description).HasColumnName("description");
            product.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(400);

            product.Property(p => p.Price).HasColumnName("price").HasPrecision(18, 2);
            product.Property(p => p.DiscountPrice).HasColumnName("discount_price").HasPrecision(18, 2);

            product.Property(p => p.StockQuantity).HasColumnName("stock_quantity");
            product.Property(p => p.WarrantyMonths).HasColumnName("warranty_months");

            product.Property(p => p.IsFeatured).HasColumnName("is_featured");
            product.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            product.Property(p => p.CreatedAt).HasColumnName("created_at");
            product.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            product.HasIndex(p => p.Slug).IsUnique();
            product.HasIndex(p => p.Sku).IsUnique();
        });

        builder.Entity<SiteSettings>(settings =>
        {
            settings.ToTable("site_settings");

            settings.HasKey(s => s.Id);
            settings.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

            settings.Property(s => s.SiteName).HasColumnName("site_name").HasMaxLength(100).IsRequired();
            settings.Property(s => s.ContactEmail).HasColumnName("contact_email").HasMaxLength(200).IsRequired();
            settings.Property(s => s.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            settings.Property(s => s.BannerTitle).HasColumnName("banner_title").HasMaxLength(100).IsRequired();
            settings.Property(s => s.BannerSubtitle).HasColumnName("banner_subtitle").HasMaxLength(200).IsRequired();
            settings.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
