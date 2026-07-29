using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kartly.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added nullable, backfilled, then tightened — rather than the scaffolder's
            // `nullable: false, defaultValue: ""`, which would stamp every existing order with an
            // empty currency. Backfilling with EUR is correct: every order placed so far was
            // denominated in the base currency, that just was not recorded anywhere.
            //
            // A persistent DDL default is deliberately avoided too: it would not exist in the EF
            // model, so the database could silently supply a value the application never chose.
            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.Sql("UPDATE orders SET currency = 'EUR' WHERE currency IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency",
                table: "orders");
        }
    }
}
