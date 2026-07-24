using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kartly.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettingsBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill the existing singleton row with the real defaults, not "", so the
            // storefront hero renders immediately after the migration (matches SiteSettings.CreateDefault).
            migrationBuilder.AddColumn<string>(
                name: "banner_subtitle",
                table: "site_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Curated products from the brands you trust — delivered fast, returned free.");

            migrationBuilder.AddColumn<string>(
                name: "banner_title",
                table: "site_settings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Everything you love, in one cart.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "banner_subtitle",
                table: "site_settings");

            migrationBuilder.DropColumn(
                name: "banner_title",
                table: "site_settings");
        }
    }
}
