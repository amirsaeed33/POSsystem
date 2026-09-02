using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_BusinessAccount_AccountTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountTypeId",
                table: "AppAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAccounts_AccountTypeId",
                table: "AppAccounts",
                column: "AccountTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppAccounts_AppLookUps_AccountTypeId",
                table: "AppAccounts",
                column: "AccountTypeId",
                principalTable: "AppLookUps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppAccounts_AppLookUps_AccountTypeId",
                table: "AppAccounts");

            migrationBuilder.DropIndex(
                name: "IX_AppAccounts_AccountTypeId",
                table: "AppAccounts");

            migrationBuilder.DropColumn(
                name: "AccountTypeId",
                table: "AppAccounts");
        }
    }
}
