using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Removed_Product_IsShared : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Former IsShared products with only zero-qty stocks become tenant-level
            // (no BranchStock rows ⇒ visible in every location).
            migrationBuilder.Sql(@"
                DELETE bs
                FROM AppBranchStocks bs
                INNER JOIN AppProducts p ON p.Id = bs.ProductId
                WHERE p.IsShared = 1
                  AND bs.Quantity = 0
                  AND bs.IsDeleted = 0;
            ");

            // Shared products that still hold stock: keep them on every active branch.
            migrationBuilder.Sql(@"
                INSERT INTO AppBranchStocks
                (
                    TenantId, BranchId, ProductId, Quantity, Price, WholesalePrice, CostPrice,
                    CreationTime, IsDeleted
                )
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
                    ON b.IsDeleted = 0
                   AND b.IsActive = 1
                   AND (
                        (b.TenantId IS NULL AND p.TenantId IS NULL)
                        OR b.TenantId = p.TenantId
                   )
                WHERE p.IsShared = 1
                  AND p.IsDeleted = 0
                  AND EXISTS (
                      SELECT 1 FROM AppBranchStocks bs2
                      WHERE bs2.ProductId = p.Id AND bs2.IsDeleted = 0
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM AppBranchStocks bs3
                      WHERE bs3.ProductId = p.Id
                        AND bs3.BranchId = b.Id
                        AND bs3.IsDeleted = 0
                  );
            ");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "AppProducts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "AppProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Best-effort restore: products with no location stocks are treated as shared.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.IsShared = 1
                FROM AppProducts p
                WHERE p.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM AppBranchStocks bs
                      WHERE bs.ProductId = p.Id AND bs.IsDeleted = 0
                  );
            ");
        }
    }
}
