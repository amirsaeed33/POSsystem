using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_Product_IsShared : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "AppProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Preserve prior behavior: existing products remain visible to all branches.
            migrationBuilder.Sql("UPDATE AppProducts SET IsShared = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "AppProducts");
        }
    }
}
