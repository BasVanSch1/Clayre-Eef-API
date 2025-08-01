using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CEApi.Migrations
{
    /// <inheritdoc />
    public partial class RemovedImageUrlFromUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "imageUrl",
                table: "UserAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "imageUrl",
                table: "UserAccounts",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
