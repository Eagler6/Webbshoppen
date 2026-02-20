using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WindowDemo.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCategoryEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryEnum",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryEnum",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
