/*
  Reset transactional data so account balances become 0.

  Keeps: branches, products, categories, customers, staff, tables, deals, catalog, users.
  Clears: sales, restaurant/online orders, purchases, expenses, ledger, stock adjustments,
          payroll (optional), print logs, chat messages.

  HOW TO USE:
  1. Set @TenantId below (NULL = ALL tenants - dangerous).
  2. Optionally set @BranchId to limit to one branch (NULL = all branches for tenant).
  3. Take a DB backup first, then run the script.
  4. If results look correct, the script COMMITs at the end.

  NOTE: AppAccounts has NO BranchId (tenant-level only).
        AppLedgerEntries may have BranchId.
*/

DECLARE @TenantId INT = 1;      -- e.g. 1 ; NULL = all tenants
DECLARE @BranchId INT = NULL;   -- e.g. 2 ; NULL = all branches
DECLARE @AlsoClearPayroll BIT = 0;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

------------------------------------------------------------
-- Preview counts (before delete)
------------------------------------------------------------
SELECT 'AppSales' AS [TableName], COUNT(*) AS [RowCount]
FROM AppSales s
WHERE (@TenantId IS NULL OR s.TenantId = @TenantId)
  AND (@BranchId IS NULL OR s.BranchId = @BranchId)

UNION ALL
SELECT 'AppRestaurantOrders', COUNT(*)
FROM AppRestaurantOrders o
WHERE (@TenantId IS NULL OR o.TenantId = @TenantId)
  AND (@BranchId IS NULL OR o.BranchId = @BranchId)

UNION ALL
SELECT 'AppCustomerOrders', COUNT(*)
FROM AppCustomerOrders o
WHERE (@TenantId IS NULL OR o.TenantId = @TenantId)
  AND (@BranchId IS NULL OR o.BranchId = @BranchId)

UNION ALL
SELECT 'AppExpenses', COUNT(*)
FROM AppExpenses e
WHERE (@TenantId IS NULL OR e.TenantId = @TenantId)
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)

UNION ALL
SELECT 'AppPurchases', COUNT(*)
FROM AppPurchases p
WHERE (@TenantId IS NULL OR p.TenantId = @TenantId)
  AND (@BranchId IS NULL OR p.BranchId = @BranchId)

UNION ALL
SELECT 'AppLedgerEntries', COUNT(*)
FROM AppLedgerEntries l
INNER JOIN AppAccounts a ON a.Id = l.AccountId
WHERE (@TenantId IS NULL OR a.TenantId = @TenantId)
  AND (@BranchId IS NULL OR ISNULL(l.BranchId, @BranchId) = @BranchId);

------------------------------------------------------------
-- 1) Online order chat + lines + orders
------------------------------------------------------------
DELETE m
FROM AppCustomerChatMessages m
INNER JOIN AppCustomerOrders o ON o.Id = m.OrderId
WHERE (@TenantId IS NULL OR o.TenantId = @TenantId)
  AND (@BranchId IS NULL OR o.BranchId = @BranchId);

DELETE l
FROM AppCustomerOrderLines l
INNER JOIN AppCustomerOrders o ON o.Id = l.OrderId
WHERE (@TenantId IS NULL OR o.TenantId = @TenantId)
  AND (@BranchId IS NULL OR o.BranchId = @BranchId);

UPDATE AppCustomerOrders
SET SaleId = NULL
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

DELETE FROM AppCustomerOrders
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

------------------------------------------------------------
-- 2) Restaurant print logs / lines / orders + free tables
------------------------------------------------------------
DELETE p
FROM AppRestaurantPrintLogs p
INNER JOIN AppRestaurantOrders o ON o.Id = p.OrderId
WHERE (@TenantId IS NULL OR o.TenantId = @TenantId)
  AND (@BranchId IS NULL OR o.BranchId = @BranchId);

DELETE l
FROM AppRestaurantOrderLines l
INNER JOIN AppRestaurantOrders o ON o.Id = l.OrderId
WHERE (@TenantId IS NULL OR o.TenantId = @TenantId)
  AND (@BranchId IS NULL OR o.BranchId = @BranchId);

UPDATE AppRestaurantOrders
SET SaleId = NULL
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

DELETE FROM AppRestaurantOrders
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

UPDATE AppRestaurantTables
SET CurrentOrderId = NULL,
    Status = 0
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

------------------------------------------------------------
-- 3) Sale returns + sales (payments live on sales)
------------------------------------------------------------
DELETE rl
FROM AppSaleReturnLines rl
INNER JOIN AppSaleReturns r ON r.Id = rl.SaleReturnId
WHERE (@TenantId IS NULL OR r.TenantId = @TenantId)
  AND (@BranchId IS NULL OR r.BranchId = @BranchId);

DELETE FROM AppSaleReturns
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

DELETE sl
FROM AppSaleLines sl
INNER JOIN AppSales s ON s.Id = sl.SaleId
WHERE (@TenantId IS NULL OR s.TenantId = @TenantId)
  AND (@BranchId IS NULL OR s.BranchId = @BranchId);

DELETE FROM AppSales
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

------------------------------------------------------------
-- 4) Purchase returns + purchases
------------------------------------------------------------
DELETE rl
FROM AppPurchaseReturnLines rl
INNER JOIN AppPurchaseReturns r ON r.Id = rl.PurchaseReturnId
WHERE (@TenantId IS NULL OR r.TenantId = @TenantId)
  AND (@BranchId IS NULL OR r.BranchId = @BranchId);

DELETE FROM AppPurchaseReturns
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

DELETE pl
FROM AppPurchaseLines pl
INNER JOIN AppPurchases p ON p.Id = pl.PurchaseId
WHERE (@TenantId IS NULL OR p.TenantId = @TenantId)
  AND (@BranchId IS NULL OR p.BranchId = @BranchId);

DELETE FROM AppPurchases
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

------------------------------------------------------------
-- 5) Expenses
------------------------------------------------------------
DELETE FROM AppExpenses
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

------------------------------------------------------------
-- 6) Stock adjustments
------------------------------------------------------------
DELETE l
FROM AppStockAdjustmentLines l
INNER JOIN AppStockAdjustments a ON a.Id = l.StockAdjustmentId
WHERE (@TenantId IS NULL OR a.TenantId = @TenantId)
  AND (@BranchId IS NULL OR a.BranchId = @BranchId);

DELETE FROM AppStockAdjustments
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
  AND (@BranchId IS NULL OR BranchId = @BranchId);

------------------------------------------------------------
-- 7) Optional payroll
------------------------------------------------------------
IF @AlsoClearPayroll = 1
BEGIN
    DELETE FROM AppStaffPayrolls
    WHERE (@TenantId IS NULL OR TenantId = @TenantId)
      AND (@BranchId IS NULL OR BranchId = @BranchId);
END

------------------------------------------------------------
-- 8) Ledger + zero opening balances
------------------------------------------------------------
DELETE l
FROM AppLedgerEntries l
INNER JOIN AppAccounts a ON a.Id = l.AccountId
WHERE (@TenantId IS NULL OR a.TenantId = @TenantId)
  AND (@BranchId IS NULL OR ISNULL(l.BranchId, @BranchId) = @BranchId);

UPDATE AppAccounts
SET OpeningBalance = 0
WHERE (@TenantId IS NULL OR TenantId = @TenantId);

------------------------------------------------------------
-- Verify balances (should be 0)
------------------------------------------------------------
SELECT
    a.Id,
    a.Name,
    a.OpeningBalance,
    ISNULL((
        SELECT SUM(le.Debit) - SUM(le.Credit)
        FROM AppLedgerEntries le
        WHERE le.AccountId = a.Id
    ), 0) AS CurrentBalance
FROM AppAccounts a
WHERE (@TenantId IS NULL OR a.TenantId = @TenantId)
ORDER BY a.Name;

COMMIT;
