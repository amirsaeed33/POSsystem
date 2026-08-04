using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_BranchStock_Prices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "AppBranchStocks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "AppBranchStocks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WholesalePrice",
                table: "AppBranchStocks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill prices on existing branch stock from product catalog defaults.
            migrationBuilder.Sql(@"
UPDATE bs
SET bs.Price = p.Price,
    bs.WholesalePrice = p.WholesalePrice,
    bs.CostPrice = p.CostPrice
FROM AppBranchStocks bs
INNER JOIN AppProducts p ON p.Id = bs.ProductId AND p.IsDeleted = 0
WHERE bs.IsDeleted = 0;

-- Ensure every active product has a BranchStock row for each active branch of the same tenant.
INSERT INTO AppBranchStocks (
    TenantId, BranchId, ProductId, Quantity, Price, WholesalePrice, CostPrice,
    CreationTime, IsDeleted)
SELECT
    p.TenantId,
    b.Id,
    p.Id,
    0,
    p.Price,
    p.WholesalePrice,
    p.CostPrice,
    GETUTCDATE(),
    0
FROM AppProducts p
INNER JOIN AppBranches b
    ON ((b.TenantId = p.TenantId) OR (b.TenantId IS NULL AND p.TenantId IS NULL))
    AND b.IsActive = 1
    AND b.IsDeleted = 0
WHERE p.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1
      FROM AppBranchStocks bs
      WHERE bs.BranchId = b.Id
        AND bs.ProductId = p.Id
        AND bs.IsDeleted = 0
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "AppBranchStocks");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "AppBranchStocks");

            migrationBuilder.DropColumn(
                name: "WholesalePrice",
                table: "AppBranchStocks");
        }
    }
}
