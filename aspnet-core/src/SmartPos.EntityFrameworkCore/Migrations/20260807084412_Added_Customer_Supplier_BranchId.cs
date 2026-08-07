using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_Customer_Supplier_BranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppCustomers', 'BranchId') IS NULL
    ALTER TABLE AppCustomers ADD BranchId int NULL;
IF COL_LENGTH('AppSuppliers', 'BranchId') IS NULL
    ALTER TABLE AppSuppliers ADD BranchId int NULL;
");

            migrationBuilder.Sql(@"
UPDATE c SET c.BranchId = b.Id
FROM AppCustomers c
INNER JOIN AppBranches b ON ((b.TenantId = c.TenantId) OR (b.TenantId IS NULL AND c.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE c.BranchId IS NULL OR c.BranchId = 0;

UPDATE c SET c.BranchId = b.Id
FROM AppCustomers c
INNER JOIN (
    SELECT TenantId, MIN(Id) AS Id
    FROM AppBranches
    WHERE IsDeleted = 0
    GROUP BY TenantId
) b ON ((b.TenantId = c.TenantId) OR (b.TenantId IS NULL AND c.TenantId IS NULL))
WHERE c.BranchId IS NULL OR c.BranchId = 0;

UPDATE s SET s.BranchId = b.Id
FROM AppSuppliers s
INNER JOIN AppBranches b ON ((b.TenantId = s.TenantId) OR (b.TenantId IS NULL AND s.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE s.BranchId IS NULL OR s.BranchId = 0;

UPDATE s SET s.BranchId = b.Id
FROM AppSuppliers s
INNER JOIN (
    SELECT TenantId, MIN(Id) AS Id
    FROM AppBranches
    WHERE IsDeleted = 0
    GROUP BY TenantId
) b ON ((b.TenantId = s.TenantId) OR (b.TenantId IS NULL AND s.TenantId IS NULL))
WHERE s.BranchId IS NULL OR s.BranchId = 0;

IF EXISTS (SELECT 1 FROM AppCustomers WHERE BranchId IS NULL)
    THROW 50001, 'AppCustomers.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppSuppliers WHERE BranchId IS NULL)
    THROW 50001, 'AppSuppliers.BranchId backfill failed', 1;
");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppCustomers') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppCustomers ALTER COLUMN BranchId int NOT NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppSuppliers') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppSuppliers ALTER COLUMN BranchId int NOT NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_AppSuppliers_BranchId",
                table: "AppSuppliers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_BranchId",
                table: "AppCustomers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCustomers_AppBranches_BranchId",
                table: "AppCustomers",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppSuppliers_AppBranches_BranchId",
                table: "AppSuppliers",
                column: "BranchId",
                principalTable: "AppBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCustomers_AppBranches_BranchId",
                table: "AppCustomers");

            migrationBuilder.DropForeignKey(
                name: "FK_AppSuppliers_AppBranches_BranchId",
                table: "AppSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_AppSuppliers_BranchId",
                table: "AppSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_BranchId",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppSuppliers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppCustomers");
        }
    }
}
