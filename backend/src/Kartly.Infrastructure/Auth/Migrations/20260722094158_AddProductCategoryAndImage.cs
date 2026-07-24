using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kartly.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                // Existing rows predate categories; back-fill with a valid default (see ProductCategories).
                defaultValue: "Accessories");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "products",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "products");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "products");
        }
    }
}
