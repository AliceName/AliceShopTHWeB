using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AliceShop.Migrations
{
    /// <inheritdoc />
    public partial class FixDbCascadeAndDecimalWarning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeVariants_ProductSizes_ProductSizeId",
                table: "ProductSizeVariants");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizes_CategoryId",
                table: "ProductSizes",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizes_Categories_CategoryId",
                table: "ProductSizes",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeVariants_ProductSizes_ProductSizeId",
                table: "ProductSizeVariants",
                column: "ProductSizeId",
                principalTable: "ProductSizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizes_Categories_CategoryId",
                table: "ProductSizes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSizeVariants_ProductSizes_ProductSizeId",
                table: "ProductSizeVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductSizes_CategoryId",
                table: "ProductSizes");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSizeVariants_ProductSizes_ProductSizeId",
                table: "ProductSizeVariants",
                column: "ProductSizeId",
                principalTable: "ProductSizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
