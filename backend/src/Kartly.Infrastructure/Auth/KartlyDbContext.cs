using Kartly.Application.Orders;
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
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLineItem> OrderLines => Set<OrderLineItem>();

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

        builder.Entity<Order>(order =>
        {
            order.ToTable("orders");

            order.HasKey(o => o.Id);
            order.Property(o => o.Id).HasColumnName("id");

            order.Property(o => o.ContactEmail).HasColumnName("contact_email").HasMaxLength(200).IsRequired();
            order.Property(o => o.ContactPhone).HasColumnName("contact_phone").HasMaxLength(40).IsRequired();
            order.Property(o => o.ShipFirstName).HasColumnName("ship_first_name").HasMaxLength(100).IsRequired();
            order.Property(o => o.ShipLastName).HasColumnName("ship_last_name").HasMaxLength(100).IsRequired();
            order.Property(o => o.ShipAddress).HasColumnName("ship_address").HasMaxLength(200).IsRequired();
            order.Property(o => o.ShipCity).HasColumnName("ship_city").HasMaxLength(100).IsRequired();
            order.Property(o => o.ShipZip).HasColumnName("ship_zip").HasMaxLength(20).IsRequired();
            order.Property(o => o.ShipCountry).HasColumnName("ship_country").HasMaxLength(100).IsRequired();

            order.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.Total).HasColumnName("total").HasPrecision(18, 2);
            order.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            order.Property(o => o.CreatedAt).HasColumnName("created_at");
            order.Property(o => o.UpdatedAt).HasColumnName("updated_at");

            order.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderLineItem>(line =>
        {
            line.ToTable("order_line_items");

            line.HasKey(l => l.Id);
            line.Property(l => l.Id).HasColumnName("id");
            line.Property(l => l.OrderId).HasColumnName("order_id");
            line.Property(l => l.ProductId).HasColumnName("product_id");
            line.Property(l => l.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
            line.Property(l => l.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            line.Property(l => l.Quantity).HasColumnName("quantity");
            line.Property(l => l.LineTotal).HasColumnName("line_total").HasPrecision(18, 2);
        });
    }
}
