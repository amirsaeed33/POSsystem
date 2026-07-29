using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class ProductBarcode_UniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear duplicate active barcodes (keep lowest Id) so the unique index can be created.
            migrationBuilder.Sql(@"
UPDATE d
SET d.Barcode = NULL
FROM AppProducts d
INNER JOIN (
    SELECT TenantId, Barcode, MIN(Id) AS KeepId
    FROM AppProducts
    WHERE Barcode IS NOT NULL AND IsDeleted = 0
    GROUP BY TenantId, Barcode
    HAVING COUNT(*) > 1
) dup ON ((d.TenantId = dup.TenantId) OR (d.TenantId IS NULL AND dup.TenantId IS NULL))
    AND d.Barcode = dup.Barcode
    AND d.IsDeleted = 0
    AND d.Id <> dup.KeepId;
");

            migrationBuilder.DropIndex(
                name: "IX_AppProducts_Barcode",
                table: "AppProducts");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_TenantId_Barcode",
                table: "AppProducts",
                columns: new[] { "TenantId", "Barcode" },
                unique: true,
                filter: "[Barcode] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppProducts_TenantId_Barcode",
                table: "AppProducts");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_Barcode",
                table: "AppProducts",
                column: "Barcode");
        }
    }
}
