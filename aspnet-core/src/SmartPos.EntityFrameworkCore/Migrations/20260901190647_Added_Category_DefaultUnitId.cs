using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_Category_DefaultUnitId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultUnitId",
                table: "AppCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCategories_DefaultUnitId",
                table: "AppCategories",
                column: "DefaultUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCategories_AppUnits_DefaultUnitId",
                table: "AppCategories",
                column: "DefaultUnitId",
                principalTable: "AppUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCategories_AppUnits_DefaultUnitId",
                table: "AppCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppCategories_DefaultUnitId",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "DefaultUnitId",
                table: "AppCategories");
        }
    }
}
