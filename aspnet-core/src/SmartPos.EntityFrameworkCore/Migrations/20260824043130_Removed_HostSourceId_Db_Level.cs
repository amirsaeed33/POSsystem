using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Removed_HostSourceId_Db_Level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostSourceId",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "HostSourceId",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "HostSourceId",
                table: "AppUnits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HostSourceId",
                table: "AppCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HostSourceId",
                table: "AppBrands",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HostSourceId",
                table: "AppUnits",
                type: "int",
                nullable: true);
        }
    }
}
