using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WindowDemo.Migrations
{
    /// <inheritdoc />
    public partial class RebuildSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderLineId",
                table: "OrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Date",
                table: "Orders",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderLineId",
                table: "OrderLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ProductId",
                table: "OrderLines",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_OrderLines_OrderLineId",
                table: "OrderLines",
                column: "OrderLineId",
                principalTable: "OrderLines",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_OrderLines_OrderLineId",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Date",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_OrderLineId",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_ProductId",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "OrderLineId",
                table: "OrderLines");
        }
    }
}
