using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AliceShop.Migrations
{
    /// <inheritdoc />
    public partial class LinkProductSizeToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "ProductSizes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ProductSizes");
        }
    }
}
