using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyUserBranchAndMergeCompanyProfileToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AbpUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    InvoiceAddress = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    InvoiceContactEmail = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InvoiceContactPhone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InvoiceFooter = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
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
                    table.PrimaryKey("PK_AppBranches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppBranchStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_AppBranchStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBranchStocks_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppBranchStocks_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_BranchId",
                table: "AbpUsers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranches_TenantId_Code",
                table: "AppBranches",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchStocks_BranchId",
                table: "AppBranchStocks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchStocks_ProductId",
                table: "AppBranchStocks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchStocks_TenantId_BranchId_ProductId",
                table: "AppBranchStocks",
                columns: new[] { "TenantId", "BranchId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AbpUsers_AppBranches_BranchId",
                table: "AbpUsers",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbpUsers_AppBranches_BranchId",
                table: "AbpUsers");

            migrationBuilder.DropTable(
                name: "AppBranchStocks");

            migrationBuilder.DropTable(
                name: "AppBranches");

            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_BranchId",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AbpUsers");
        }
    }
}
