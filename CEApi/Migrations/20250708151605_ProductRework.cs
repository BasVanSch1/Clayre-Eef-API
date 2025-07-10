using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CEApi.Migrations
{
    /// <inheritdoc />
    public partial class ProductRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EAN",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "ImageLink",
                table: "Products",
                newName: "ImageUrl");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Products",
                type: "float(14)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "EanCode",
                table: "Products",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EanCode",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Products",
                newName: "ImageLink");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Products",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(14)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "EAN",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
