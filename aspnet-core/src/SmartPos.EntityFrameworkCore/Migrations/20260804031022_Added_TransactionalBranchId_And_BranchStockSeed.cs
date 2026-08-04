using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_TransactionalBranchId_And_BranchStockSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Separate Sql() calls so SQL Server recompiles after ALTER TABLE ADD Column.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM AppBranches WHERE TenantId IS NULL AND Code = N'MAIN' AND IsDeleted = 0)
    INSERT INTO AppBranches (TenantId, Name, Code, IsActive, IsDefault, CreationTime, IsDeleted)
    VALUES (NULL, N'Main', N'MAIN', 1, 1, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM AppBranches WHERE TenantId = 1 AND Code = N'MAIN' AND IsDeleted = 0)
    INSERT INTO AppBranches (TenantId, Name, Code, IsActive, IsDefault, CreationTime, IsDeleted)
    VALUES (1, N'Main', N'MAIN', 1, 1, GETUTCDATE(), 0);

INSERT INTO AppBranches (TenantId, Name, Code, IsActive, IsDefault, CreationTime, IsDeleted)
SELECT DISTINCT p.TenantId, N'Main', N'MAIN', 1, 1, GETUTCDATE(), 0
FROM AppProducts p
WHERE p.IsDeleted = 0
  AND p.TenantId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM AppBranches b
      WHERE b.TenantId = p.TenantId AND b.Code = N'MAIN' AND b.IsDeleted = 0
  );
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('AppSales', 'BranchId') IS NULL
    ALTER TABLE AppSales ADD BranchId int NULL;
IF COL_LENGTH('AppPurchases', 'BranchId') IS NULL
    ALTER TABLE AppPurchases ADD BranchId int NULL;
IF COL_LENGTH('AppSaleReturns', 'BranchId') IS NULL
    ALTER TABLE AppSaleReturns ADD BranchId int NULL;
IF COL_LENGTH('AppPurchaseReturns', 'BranchId') IS NULL
    ALTER TABLE AppPurchaseReturns ADD BranchId int NULL;
IF COL_LENGTH('AppStockAdjustments', 'BranchId') IS NULL
    ALTER TABLE AppStockAdjustments ADD BranchId int NULL;
IF COL_LENGTH('AppCustomerOrders', 'BranchId') IS NULL
    ALTER TABLE AppCustomerOrders ADD BranchId int NULL;
IF COL_LENGTH('AppExpenses', 'BranchId') IS NULL
    ALTER TABLE AppExpenses ADD BranchId int NULL;
");

            migrationBuilder.Sql(@"
UPDATE s SET s.BranchId = b.Id
FROM AppSales s
INNER JOIN AppBranches b ON ((b.TenantId = s.TenantId) OR (b.TenantId IS NULL AND s.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE s.BranchId IS NULL OR s.BranchId = 0;

UPDATE p SET p.BranchId = b.Id
FROM AppPurchases p
INNER JOIN AppBranches b ON ((b.TenantId = p.TenantId) OR (b.TenantId IS NULL AND p.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE p.BranchId IS NULL OR p.BranchId = 0;

UPDATE r SET r.BranchId = b.Id
FROM AppSaleReturns r
INNER JOIN AppBranches b ON ((b.TenantId = r.TenantId) OR (b.TenantId IS NULL AND r.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE r.BranchId IS NULL OR r.BranchId = 0;

UPDATE r SET r.BranchId = b.Id
FROM AppPurchaseReturns r
INNER JOIN AppBranches b ON ((b.TenantId = r.TenantId) OR (b.TenantId IS NULL AND r.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE r.BranchId IS NULL OR r.BranchId = 0;

UPDATE a SET a.BranchId = b.Id
FROM AppStockAdjustments a
INNER JOIN AppBranches b ON ((b.TenantId = a.TenantId) OR (b.TenantId IS NULL AND a.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE a.BranchId IS NULL OR a.BranchId = 0;

UPDATE o SET o.BranchId = b.Id
FROM AppCustomerOrders o
INNER JOIN AppBranches b ON ((b.TenantId = o.TenantId) OR (b.TenantId IS NULL AND o.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE o.BranchId IS NULL OR o.BranchId = 0;

UPDATE e SET e.BranchId = b.Id
FROM AppExpenses e
INNER JOIN AppBranches b ON ((b.TenantId = e.TenantId) OR (b.TenantId IS NULL AND e.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE e.BranchId IS NULL OR e.BranchId = 0;

UPDATE u SET u.BranchId = b.Id
FROM AbpUsers u
INNER JOIN AppBranches b ON ((b.TenantId = u.TenantId) OR (b.TenantId IS NULL AND u.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE u.BranchId IS NULL AND u.IsDeleted = 0;

INSERT INTO AppBranchStocks (TenantId, BranchId, ProductId, Quantity, CreationTime, IsDeleted)
SELECT p.TenantId, b.Id, p.Id, p.StockQuantity, GETUTCDATE(), 0
FROM AppProducts p
INNER JOIN AppBranches b ON ((b.TenantId = p.TenantId) OR (b.TenantId IS NULL AND p.TenantId IS NULL))
    AND b.Code = N'MAIN' AND b.IsDeleted = 0
WHERE p.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM AppBranchStocks bs
      WHERE bs.BranchId = b.Id AND bs.ProductId = p.Id AND bs.IsDeleted = 0
  );

IF EXISTS (SELECT 1 FROM AppSales WHERE BranchId IS NULL)
    THROW 50001, 'AppSales.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppPurchases WHERE BranchId IS NULL)
    THROW 50001, 'AppPurchases.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppSaleReturns WHERE BranchId IS NULL)
    THROW 50001, 'AppSaleReturns.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppPurchaseReturns WHERE BranchId IS NULL)
    THROW 50001, 'AppPurchaseReturns.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppStockAdjustments WHERE BranchId IS NULL)
    THROW 50001, 'AppStockAdjustments.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppCustomerOrders WHERE BranchId IS NULL)
    THROW 50001, 'AppCustomerOrders.BranchId backfill failed', 1;
IF EXISTS (SELECT 1 FROM AppExpenses WHERE BranchId IS NULL)
    THROW 50001, 'AppExpenses.BranchId backfill failed', 1;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppSales_AppBranches_BranchId')
    ALTER TABLE AppSales DROP CONSTRAINT FK_AppSales_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppPurchases_AppBranches_BranchId')
    ALTER TABLE AppPurchases DROP CONSTRAINT FK_AppPurchases_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppSaleReturns_AppBranches_BranchId')
    ALTER TABLE AppSaleReturns DROP CONSTRAINT FK_AppSaleReturns_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppPurchaseReturns_AppBranches_BranchId')
    ALTER TABLE AppPurchaseReturns DROP CONSTRAINT FK_AppPurchaseReturns_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppStockAdjustments_AppBranches_BranchId')
    ALTER TABLE AppStockAdjustments DROP CONSTRAINT FK_AppStockAdjustments_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppCustomerOrders_AppBranches_BranchId')
    ALTER TABLE AppCustomerOrders DROP CONSTRAINT FK_AppCustomerOrders_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppExpenses_AppBranches_BranchId')
    ALTER TABLE AppExpenses DROP CONSTRAINT FK_AppExpenses_AppBranches_BranchId;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSales_BranchId' AND object_id = OBJECT_ID(N'AppSales'))
    DROP INDEX IX_AppSales_BranchId ON AppSales;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppPurchases_BranchId' AND object_id = OBJECT_ID(N'AppPurchases'))
    DROP INDEX IX_AppPurchases_BranchId ON AppPurchases;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSaleReturns_BranchId' AND object_id = OBJECT_ID(N'AppSaleReturns'))
    DROP INDEX IX_AppSaleReturns_BranchId ON AppSaleReturns;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppPurchaseReturns_BranchId' AND object_id = OBJECT_ID(N'AppPurchaseReturns'))
    DROP INDEX IX_AppPurchaseReturns_BranchId ON AppPurchaseReturns;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppStockAdjustments_BranchId' AND object_id = OBJECT_ID(N'AppStockAdjustments'))
    DROP INDEX IX_AppStockAdjustments_BranchId ON AppStockAdjustments;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppCustomerOrders_BranchId' AND object_id = OBJECT_ID(N'AppCustomerOrders'))
    DROP INDEX IX_AppCustomerOrders_BranchId ON AppCustomerOrders;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppExpenses_BranchId' AND object_id = OBJECT_ID(N'AppExpenses'))
    DROP INDEX IX_AppExpenses_BranchId ON AppExpenses;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppSales') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppSales ALTER COLUMN BranchId int NOT NULL;
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppPurchases') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppPurchases ALTER COLUMN BranchId int NOT NULL;
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppSaleReturns') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppSaleReturns ALTER COLUMN BranchId int NOT NULL;
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppPurchaseReturns') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppPurchaseReturns ALTER COLUMN BranchId int NOT NULL;
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppStockAdjustments') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppStockAdjustments ALTER COLUMN BranchId int NOT NULL;
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppCustomerOrders') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppCustomerOrders ALTER COLUMN BranchId int NOT NULL;
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AppExpenses') AND name = 'BranchId' AND is_nullable = 1)
    ALTER TABLE AppExpenses ALTER COLUMN BranchId int NOT NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSales_BranchId' AND object_id = OBJECT_ID(N'AppSales'))
    CREATE INDEX IX_AppSales_BranchId ON AppSales (BranchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppPurchases_BranchId' AND object_id = OBJECT_ID(N'AppPurchases'))
    CREATE INDEX IX_AppPurchases_BranchId ON AppPurchases (BranchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSaleReturns_BranchId' AND object_id = OBJECT_ID(N'AppSaleReturns'))
    CREATE INDEX IX_AppSaleReturns_BranchId ON AppSaleReturns (BranchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppPurchaseReturns_BranchId' AND object_id = OBJECT_ID(N'AppPurchaseReturns'))
    CREATE INDEX IX_AppPurchaseReturns_BranchId ON AppPurchaseReturns (BranchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppStockAdjustments_BranchId' AND object_id = OBJECT_ID(N'AppStockAdjustments'))
    CREATE INDEX IX_AppStockAdjustments_BranchId ON AppStockAdjustments (BranchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppCustomerOrders_BranchId' AND object_id = OBJECT_ID(N'AppCustomerOrders'))
    CREATE INDEX IX_AppCustomerOrders_BranchId ON AppCustomerOrders (BranchId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppExpenses_BranchId' AND object_id = OBJECT_ID(N'AppExpenses'))
    CREATE INDEX IX_AppExpenses_BranchId ON AppExpenses (BranchId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppSales_AppBranches_BranchId')
    ALTER TABLE AppSales ADD CONSTRAINT FK_AppSales_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppPurchases_AppBranches_BranchId')
    ALTER TABLE AppPurchases ADD CONSTRAINT FK_AppPurchases_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppSaleReturns_AppBranches_BranchId')
    ALTER TABLE AppSaleReturns ADD CONSTRAINT FK_AppSaleReturns_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppPurchaseReturns_AppBranches_BranchId')
    ALTER TABLE AppPurchaseReturns ADD CONSTRAINT FK_AppPurchaseReturns_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppStockAdjustments_AppBranches_BranchId')
    ALTER TABLE AppStockAdjustments ADD CONSTRAINT FK_AppStockAdjustments_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppCustomerOrders_AppBranches_BranchId')
    ALTER TABLE AppCustomerOrders ADD CONSTRAINT FK_AppCustomerOrders_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppExpenses_AppBranches_BranchId')
    ALTER TABLE AppExpenses ADD CONSTRAINT FK_AppExpenses_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppSales_AppBranches_BranchId')
    ALTER TABLE AppSales DROP CONSTRAINT FK_AppSales_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppPurchases_AppBranches_BranchId')
    ALTER TABLE AppPurchases DROP CONSTRAINT FK_AppPurchases_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppSaleReturns_AppBranches_BranchId')
    ALTER TABLE AppSaleReturns DROP CONSTRAINT FK_AppSaleReturns_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppPurchaseReturns_AppBranches_BranchId')
    ALTER TABLE AppPurchaseReturns DROP CONSTRAINT FK_AppPurchaseReturns_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppStockAdjustments_AppBranches_BranchId')
    ALTER TABLE AppStockAdjustments DROP CONSTRAINT FK_AppStockAdjustments_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppCustomerOrders_AppBranches_BranchId')
    ALTER TABLE AppCustomerOrders DROP CONSTRAINT FK_AppCustomerOrders_AppBranches_BranchId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppExpenses_AppBranches_BranchId')
    ALTER TABLE AppExpenses DROP CONSTRAINT FK_AppExpenses_AppBranches_BranchId;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSales_BranchId' AND object_id = OBJECT_ID(N'AppSales'))
    DROP INDEX IX_AppSales_BranchId ON AppSales;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppPurchases_BranchId' AND object_id = OBJECT_ID(N'AppPurchases'))
    DROP INDEX IX_AppPurchases_BranchId ON AppPurchases;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppSaleReturns_BranchId' AND object_id = OBJECT_ID(N'AppSaleReturns'))
    DROP INDEX IX_AppSaleReturns_BranchId ON AppSaleReturns;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppPurchaseReturns_BranchId' AND object_id = OBJECT_ID(N'AppPurchaseReturns'))
    DROP INDEX IX_AppPurchaseReturns_BranchId ON AppPurchaseReturns;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppStockAdjustments_BranchId' AND object_id = OBJECT_ID(N'AppStockAdjustments'))
    DROP INDEX IX_AppStockAdjustments_BranchId ON AppStockAdjustments;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppCustomerOrders_BranchId' AND object_id = OBJECT_ID(N'AppCustomerOrders'))
    DROP INDEX IX_AppCustomerOrders_BranchId ON AppCustomerOrders;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppExpenses_BranchId' AND object_id = OBJECT_ID(N'AppExpenses'))
    DROP INDEX IX_AppExpenses_BranchId ON AppExpenses;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('AppSales', 'BranchId') IS NOT NULL ALTER TABLE AppSales DROP COLUMN BranchId;
IF COL_LENGTH('AppPurchases', 'BranchId') IS NOT NULL ALTER TABLE AppPurchases DROP COLUMN BranchId;
IF COL_LENGTH('AppSaleReturns', 'BranchId') IS NOT NULL ALTER TABLE AppSaleReturns DROP COLUMN BranchId;
IF COL_LENGTH('AppPurchaseReturns', 'BranchId') IS NOT NULL ALTER TABLE AppPurchaseReturns DROP COLUMN BranchId;
IF COL_LENGTH('AppStockAdjustments', 'BranchId') IS NOT NULL ALTER TABLE AppStockAdjustments DROP COLUMN BranchId;
IF COL_LENGTH('AppCustomerOrders', 'BranchId') IS NOT NULL ALTER TABLE AppCustomerOrders DROP COLUMN BranchId;
IF COL_LENGTH('AppExpenses', 'BranchId') IS NOT NULL ALTER TABLE AppExpenses DROP COLUMN BranchId;
");
        }
    }
}
