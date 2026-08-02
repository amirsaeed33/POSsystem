using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_MultiBranch_Support : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_AppBranches_TenantId_Code",
                table: "AppBranches",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // Seed a default branch for every known tenant scope (including host/null).
            migrationBuilder.Sql(@"
INSERT INTO AppBranches (TenantId, Name, Code, IsActive, IsDefault, CreationTime, IsDeleted)
SELECT t.TenantId, N'Main', N'DEFAULT', 1, 1, SYSUTCDATETIME(), 0
FROM (
    SELECT DISTINCT TenantId FROM AbpUsers
    UNION
    SELECT DISTINCT TenantId FROM AppProducts
    UNION
    SELECT DISTINCT Id AS TenantId FROM AbpTenants WHERE IsDeleted = 0
) t
WHERE NOT EXISTS (
    SELECT 1 FROM AppBranches b
    WHERE ((b.TenantId = t.TenantId) OR (b.TenantId IS NULL AND t.TenantId IS NULL))
      AND b.Code = N'DEFAULT'
      AND b.IsDeleted = 0
);
");

            // Ensure host + default tenant always have a default branch even with empty tables.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM AppBranches WHERE TenantId IS NULL AND Code = N'DEFAULT' AND IsDeleted = 0)
    INSERT INTO AppBranches (TenantId, Name, Code, IsActive, IsDefault, CreationTime, IsDeleted)
    VALUES (NULL, N'Main', N'DEFAULT', 1, 1, SYSUTCDATETIME(), 0);

IF NOT EXISTS (SELECT 1 FROM AppBranches WHERE TenantId = 1 AND Code = N'DEFAULT' AND IsDeleted = 0)
    INSERT INTO AppBranches (TenantId, Name, Code, IsActive, IsDefault, CreationTime, IsDeleted)
    VALUES (1, N'Main', N'DEFAULT', 1, 1, SYSUTCDATETIME(), 0);
");

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

            // Move Product.StockQuantity into BranchStock for the default branch.
            migrationBuilder.Sql(@"
INSERT INTO AppBranchStocks (TenantId, BranchId, ProductId, Quantity, CreationTime, IsDeleted)
SELECT p.TenantId, b.Id, p.Id, p.StockQuantity, SYSUTCDATETIME(), 0
FROM AppProducts p
INNER JOIN AppBranches b
    ON ((b.TenantId = p.TenantId) OR (b.TenantId IS NULL AND p.TenantId IS NULL))
   AND b.Code = N'DEFAULT'
   AND b.IsDeleted = 0
WHERE p.IsDeleted = 0;
");

            migrationBuilder.CreateTable(
                name: "AppUserBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserBranches_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserBranches_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserBranches_BranchId",
                table: "AppUserBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserBranches_TenantId_UserId_BranchId",
                table: "AppUserBranches",
                columns: new[] { "TenantId", "UserId", "BranchId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserBranches_UserId",
                table: "AppUserBranches",
                column: "UserId");

            migrationBuilder.Sql(@"
INSERT INTO AppUserBranches (TenantId, UserId, BranchId, CreationTime)
SELECT u.TenantId, u.Id, b.Id, SYSUTCDATETIME()
FROM AbpUsers u
INNER JOIN AppBranches b
    ON ((b.TenantId = u.TenantId) OR (b.TenantId IS NULL AND u.TenantId IS NULL))
   AND b.Code = N'DEFAULT'
   AND b.IsDeleted = 0
WHERE u.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM AppUserBranches ub
      WHERE ub.UserId = u.Id AND ub.BranchId = b.Id
  );
");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppStockAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppSales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppSaleReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppPurchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppPurchaseReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppExpenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AppCustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE AppSales SET BranchId = b.Id
FROM AppSales s
INNER JOIN AppBranches b ON ((b.TenantId = s.TenantId) OR (b.TenantId IS NULL AND s.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE s.BranchId IS NULL;

UPDATE AppPurchases SET BranchId = b.Id
FROM AppPurchases p
INNER JOIN AppBranches b ON ((b.TenantId = p.TenantId) OR (b.TenantId IS NULL AND p.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE p.BranchId IS NULL;

UPDATE AppSaleReturns SET BranchId = b.Id
FROM AppSaleReturns r
INNER JOIN AppBranches b ON ((b.TenantId = r.TenantId) OR (b.TenantId IS NULL AND r.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE r.BranchId IS NULL;

UPDATE AppPurchaseReturns SET BranchId = b.Id
FROM AppPurchaseReturns r
INNER JOIN AppBranches b ON ((b.TenantId = r.TenantId) OR (b.TenantId IS NULL AND r.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE r.BranchId IS NULL;

UPDATE AppStockAdjustments SET BranchId = b.Id
FROM AppStockAdjustments a
INNER JOIN AppBranches b ON ((b.TenantId = a.TenantId) OR (b.TenantId IS NULL AND a.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE a.BranchId IS NULL;

UPDATE AppCustomerOrders SET BranchId = b.Id
FROM AppCustomerOrders o
INNER JOIN AppBranches b ON ((b.TenantId = o.TenantId) OR (b.TenantId IS NULL AND o.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE o.BranchId IS NULL;

UPDATE AppExpenses SET BranchId = b.Id
FROM AppExpenses e
INNER JOIN AppBranches b ON ((b.TenantId = e.TenantId) OR (b.TenantId IS NULL AND e.TenantId IS NULL))
 AND b.Code = N'DEFAULT' AND b.IsDeleted = 0
WHERE e.BranchId IS NULL;
");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppStockAdjustments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppSales",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppSaleReturns",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppPurchases",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppPurchaseReturns",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppExpenses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "AppCustomerOrders",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustments_BranchId",
                table: "AppStockAdjustments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSales_BranchId",
                table: "AppSales",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSaleReturns_BranchId",
                table: "AppSaleReturns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPurchases_BranchId",
                table: "AppPurchases",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPurchaseReturns_BranchId",
                table: "AppPurchaseReturns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExpenses_BranchId",
                table: "AppExpenses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomerOrders_BranchId",
                table: "AppCustomerOrders",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCustomerOrders_AppBranches_BranchId",
                table: "AppCustomerOrders",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppExpenses_AppBranches_BranchId",
                table: "AppExpenses",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppPurchaseReturns_AppBranches_BranchId",
                table: "AppPurchaseReturns",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppPurchases_AppBranches_BranchId",
                table: "AppPurchases",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppSaleReturns_AppBranches_BranchId",
                table: "AppSaleReturns",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppSales_AppBranches_BranchId",
                table: "AppSales",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppStockAdjustments_AppBranches_BranchId",
                table: "AppStockAdjustments",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "AppProducts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StockQuantity",
                table: "AppProducts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
UPDATE AppProducts SET StockQuantity = ISNULL((
    SELECT SUM(bs.Quantity)
    FROM AppBranchStocks bs
    WHERE bs.ProductId = AppProducts.Id AND bs.IsDeleted = 0
), 0);
");

            migrationBuilder.DropForeignKey(
                name: "FK_AppCustomerOrders_AppBranches_BranchId",
                table: "AppCustomerOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_AppExpenses_AppBranches_BranchId",
                table: "AppExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_AppPurchaseReturns_AppBranches_BranchId",
                table: "AppPurchaseReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_AppPurchases_AppBranches_BranchId",
                table: "AppPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_AppSaleReturns_AppBranches_BranchId",
                table: "AppSaleReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_AppSales_AppBranches_BranchId",
                table: "AppSales");

            migrationBuilder.DropForeignKey(
                name: "FK_AppStockAdjustments_AppBranches_BranchId",
                table: "AppStockAdjustments");

            migrationBuilder.DropTable(
                name: "AppBranchStocks");

            migrationBuilder.DropTable(
                name: "AppUserBranches");

            migrationBuilder.DropIndex(
                name: "IX_AppStockAdjustments_BranchId",
                table: "AppStockAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_AppSales_BranchId",
                table: "AppSales");

            migrationBuilder.DropIndex(
                name: "IX_AppSaleReturns_BranchId",
                table: "AppSaleReturns");

            migrationBuilder.DropIndex(
                name: "IX_AppPurchases_BranchId",
                table: "AppPurchases");

            migrationBuilder.DropIndex(
                name: "IX_AppPurchaseReturns_BranchId",
                table: "AppPurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_AppExpenses_BranchId",
                table: "AppExpenses");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomerOrders_BranchId",
                table: "AppCustomerOrders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppStockAdjustments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppSaleReturns");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppPurchases");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppPurchaseReturns");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppExpenses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppCustomerOrders");

            migrationBuilder.DropTable(
                name: "AppBranches");
        }
    }
}
