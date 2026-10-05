using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_BranchNotificationSetting_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBranchNotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnSale = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnPurchase = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnExpense = table.Column<bool>(type: "bit", nullable: false),
                    Emails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBranchNotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBranchNotificationSettings_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchNotificationSettings_BranchId",
                table: "AppBranchNotificationSettings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchNotificationSettings_TenantId_BranchId",
                table: "AppBranchNotificationSettings",
                columns: new[] { "TenantId", "BranchId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBranchNotificationSettings");
        }
    }
}
