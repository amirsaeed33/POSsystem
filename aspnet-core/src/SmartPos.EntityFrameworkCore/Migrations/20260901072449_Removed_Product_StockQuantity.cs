using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Removed_Product_StockQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "AppBranches");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppUnits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "AppUnits",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoginPassword",
                table: "AppStaff",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "AppStaff",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppBrands",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppBrands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationTokenExpiresAt",
                table: "AppBranches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationTokenHash",
                table: "AppBranches",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "AppBranches",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "AppBranches",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercent",
                table: "AppBranches",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AppHostCatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CompanyTypeId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AppHostCatalogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppHostCatalogItems_AppHostCatalogItems_CompanyTypeId",
                        column: x => x.CompanyTypeId,
                        principalTable: "AppHostCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppBranchSeedRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyTypeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppBranchSeedRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBranchSeedRequests_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppBranchSeedRequests_AppHostCatalogItems_CompanyTypeId",
                        column: x => x.CompanyTypeId,
                        principalTable: "AppHostCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppBranchSeedRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchSeedRequestId = table.Column<int>(type: "int", nullable: false),
                    HostItemId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBranchSeedRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppBranchSeedRequestItems_AppBranchSeedRequests_BranchSeedRequestId",
                        column: x => x.BranchSeedRequestId,
                        principalTable: "AppBranchSeedRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppBranchSeedRequestItems_AppHostCatalogItems_HostItemId",
                        column: x => x.HostItemId,
                        principalTable: "AppHostCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUnits_BranchId",
                table: "AppUnits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUnits_TenantId_BranchId_Name",
                table: "AppUnits",
                columns: new[] { "TenantId", "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppStaff_UserId",
                table: "AppStaff",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCategories_BranchId",
                table: "AppCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCategories_TenantId_BranchId_Name",
                table: "AppCategories",
                columns: new[] { "TenantId", "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppBrands_BranchId",
                table: "AppBrands",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBrands_TenantId_BranchId_Name",
                table: "AppBrands",
                columns: new[] { "TenantId", "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchSeedRequestItems_BranchSeedRequestId_HostItemId",
                table: "AppBranchSeedRequestItems",
                columns: new[] { "BranchSeedRequestId", "HostItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchSeedRequestItems_HostItemId",
                table: "AppBranchSeedRequestItems",
                column: "HostItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchSeedRequests_BranchId",
                table: "AppBranchSeedRequests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchSeedRequests_CompanyTypeId",
                table: "AppBranchSeedRequests",
                column: "CompanyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBranchSeedRequests_TenantId_BranchId_Status",
                table: "AppBranchSeedRequests",
                columns: new[] { "TenantId", "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppHostCatalogItems_CompanyTypeId",
                table: "AppHostCatalogItems",
                column: "CompanyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppHostCatalogItems_Type_CompanyTypeId_IsActive",
                table: "AppHostCatalogItems",
                columns: new[] { "Type", "CompanyTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppHostCatalogItems_Type_CompanyTypeId_Name",
                table: "AppHostCatalogItems",
                columns: new[] { "Type", "CompanyTypeId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AppBrands_AppBranches_BranchId",
                table: "AppBrands",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppCategories_AppBranches_BranchId",
                table: "AppCategories",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppUnits_AppBranches_BranchId",
                table: "AppUnits",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppBrands_AppBranches_BranchId",
                table: "AppBrands");

            migrationBuilder.DropForeignKey(
                name: "FK_AppCategories_AppBranches_BranchId",
                table: "AppCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_AppUnits_AppBranches_BranchId",
                table: "AppUnits");

            migrationBuilder.DropTable(
                name: "AppBranchSeedRequestItems");

            migrationBuilder.DropTable(
                name: "AppBranchSeedRequests");

            migrationBuilder.DropTable(
                name: "AppHostCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_AppUnits_BranchId",
                table: "AppUnits");

            migrationBuilder.DropIndex(
                name: "IX_AppUnits_TenantId_BranchId_Name",
                table: "AppUnits");

            migrationBuilder.DropIndex(
                name: "IX_AppStaff_UserId",
                table: "AppStaff");

            migrationBuilder.DropIndex(
                name: "IX_AppCategories_BranchId",
                table: "AppCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppCategories_TenantId_BranchId_Name",
                table: "AppCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppBrands_BranchId",
                table: "AppBrands");

            migrationBuilder.DropIndex(
                name: "IX_AppBrands_TenantId_BranchId_Name",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppUnits");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppUnits");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "AppUnits");

            migrationBuilder.DropColumn(
                name: "LoginPassword",
                table: "AppStaff");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AppStaff");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiresAt",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "ActivationTokenHash",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "TaxPercent",
                table: "AppBranches");

            migrationBuilder.AddColumn<decimal>(
                name: "StockQuantity",
                table: "AppProducts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "AppBranches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
