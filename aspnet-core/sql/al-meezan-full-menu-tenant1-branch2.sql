/*
  Al Meezan Foods — FINAL seed
  TenantId = 1, BranchId = 2

  - Creates Unit + Brand + Categories
  - Inserts ONE product per menu item
  - Sizes / portions are modifiers (Price = extra over base product price)
  - Soft-deletes previous BranchId=2 products/modifiers/categories
    (hard DELETE is blocked when order lines reference products)
  - Sets Unsplash HTTPS images

  Run:
    sqlcmd -S localhost -d SmartPosDb -E -I -i "E:\Github\POSsystem\aspnet-core\sql\al-meezan-full-menu-tenant1-branch2.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @TenantId INT = 1;
DECLARE @BranchId INT = 2;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @UnitId INT;
DECLARE @BrandId INT;

IF NOT EXISTS (SELECT 1 FROM AppBranches WHERE Id = @BranchId AND TenantId = @TenantId)
BEGIN
    RAISERROR('BranchId=2 for TenantId=1 was not found.', 16, 1);
    RETURN;
END

UPDATE AbpTenants
SET IsDeleted = 0, IsActive = 1, DeletionTime = NULL
WHERE Id = @TenantId AND (IsDeleted = 1 OR IsActive = 0);

UPDATE AppBranches
SET IsDeleted = 0, IsActive = 1, DeletionTime = NULL
WHERE Id = @BranchId AND TenantId = @TenantId AND (IsDeleted = 1 OR IsActive = 0);

BEGIN TRAN;

/* ---------- Soft-delete previous Branch 2 menu data (keep FK history) ---------- */
DECLARE @OldProductIds TABLE (Id INT PRIMARY KEY);
INSERT INTO @OldProductIds (Id)
SELECT Id FROM AppProducts
WHERE BranchId = @BranchId AND TenantId = @TenantId AND IsDeleted = 0;

DECLARE @OldGroupIds TABLE (Id INT PRIMARY KEY);
INSERT INTO @OldGroupIds (Id)
SELECT g.Id
FROM AppProductModifierGroups g
INNER JOIN @OldProductIds p ON p.Id = g.ProductId
WHERE g.IsDeleted = 0;

UPDATE o
SET o.IsDeleted = 1,
    o.DeletionTime = @Now
FROM AppProductModifierOptions o
INNER JOIN @OldGroupIds g ON g.Id = o.ModifierGroupId
WHERE o.IsDeleted = 0;

UPDATE g
SET g.IsDeleted = 1,
    g.DeletionTime = @Now
FROM AppProductModifierGroups g
INNER JOIN @OldGroupIds x ON x.Id = g.Id
WHERE g.IsDeleted = 0;

UPDATE p
SET p.IsDeleted = 1,
    p.DeletionTime = @Now,
    p.LastModificationTime = @Now
FROM AppProducts p
INNER JOIN @OldProductIds x ON x.Id = p.Id
WHERE p.IsDeleted = 0;

UPDATE AppCategories
SET IsDeleted = 1,
    DeletionTime = @Now,
    LastModificationTime = @Now
WHERE BranchId = @BranchId
  AND TenantId = @TenantId
  AND IsDeleted = 0;

/* ---------- Unit ---------- */
IF NOT EXISTS (SELECT 1 FROM AppUnits WHERE BranchId = @BranchId AND IsDeleted = 0 AND Name = N'Portion')
    INSERT INTO AppUnits (TenantId, BranchId, Name, Description, Symbol, IsActive, CreationTime, IsDeleted)
    VALUES (@TenantId, @BranchId, N'Portion', N'Standard serving / portion', N'pc', 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM AppUnits WHERE BranchId = @BranchId AND IsDeleted = 0 AND Name = N'Piece')
    INSERT INTO AppUnits (TenantId, BranchId, Name, Description, Symbol, IsActive, CreationTime, IsDeleted)
    VALUES (@TenantId, @BranchId, N'Piece', N'Single piece', N'pc', 1, @Now, 0);

SELECT @UnitId = Id FROM AppUnits WHERE BranchId = @BranchId AND IsDeleted = 0 AND Name = N'Portion';

/* ---------- Brand ---------- */
IF NOT EXISTS (SELECT 1 FROM AppBrands WHERE BranchId = @BranchId AND IsDeleted = 0 AND Name = N'Al Meezan')
    INSERT INTO AppBrands (TenantId, BranchId, Name, Description, IsActive, CreationTime, IsDeleted)
    VALUES (@TenantId, @BranchId, N'Al Meezan', N'Al Meezan Foods — Taste & Quality', 1, @Now, 0);

SELECT @BrandId = Id FROM AppBrands WHERE BranchId = @BranchId AND IsDeleted = 0 AND Name = N'Al Meezan';

/* ---------- Categories ---------- */
DECLARE @Cats TABLE (Name NVARCHAR(128), Description NVARCHAR(512));
INSERT INTO @Cats (Name, Description) VALUES
 (N'BBQ Pieces', N'Desi BBQ pieces'),
 (N'BBQ Specialties', N'Desi BBQ specialties'),
 (N'BBQ Platters', N'Desi BBQ platters'),
 (N'BBQ Fry', N'Desi BBQ fry'),
 (N'Tandoori', N'Tandoori breads'),
 (N'Raita & Salad', N'Raita and salad'),
 (N'Burgers', N'Fast food burgers'),
 (N'Shawarma', N'Shawarma'),
 (N'Paratha & Spring Rolls', N'Paratha rolls and spring rolls'),
 (N'Signature Wraps', N'Signature wraps'),
 (N'Pasta', N'Pasta'),
 (N'Fries', N'Fries'),
 (N'Wings & Nuggets', N'Wings and nuggets'),
 (N'Pizza', N'Pizza'),
 (N'Stuffed & Crown Crust Pizza', N'Stuffed and crown crust pizza'),
 (N'Boneless Chicken Handi', N'Boneless chicken handi'),
 (N'Boneless Chicken Specials', N'Boneless chicken specials'),
 (N'Chicken Karahi', N'Chicken karahi'),
 (N'Traditional Curry', N'Traditional curry'),
 (N'Mutton & Beef', N'Mutton and beef'),
 (N'Rice', N'Biryani and pulao'),
 (N'Regular Salan', N'Regular salan'),
 (N'Deals', N'Combo deals');

INSERT INTO AppCategories (TenantId, BranchId, Name, Description, IsActive, DefaultUnitId, CreationTime, IsDeleted)
SELECT @TenantId, @BranchId, c.Name, c.Description, 1, @UnitId, @Now, 0
FROM @Cats c;

DECLARE @CatBbqPieces INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'BBQ Pieces' AND IsDeleted=0);
DECLARE @CatBbqSpecialties INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'BBQ Specialties' AND IsDeleted=0);
DECLARE @CatBbqPlatters INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'BBQ Platters' AND IsDeleted=0);
DECLARE @CatBbqFry INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'BBQ Fry' AND IsDeleted=0);
DECLARE @CatTandoori INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Tandoori' AND IsDeleted=0);
DECLARE @CatRaitaSalad INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Raita & Salad' AND IsDeleted=0);
DECLARE @CatBurgers INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Burgers' AND IsDeleted=0);
DECLARE @CatShawarma INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Shawarma' AND IsDeleted=0);
DECLARE @CatRolls INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Paratha & Spring Rolls' AND IsDeleted=0);
DECLARE @CatWraps INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Signature Wraps' AND IsDeleted=0);
DECLARE @CatPasta INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Pasta' AND IsDeleted=0);
DECLARE @CatFries INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Fries' AND IsDeleted=0);
DECLARE @CatWings INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Wings & Nuggets' AND IsDeleted=0);
DECLARE @CatPizza INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Pizza' AND IsDeleted=0);
DECLARE @CatStuffed INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Stuffed & Crown Crust Pizza' AND IsDeleted=0);
DECLARE @CatHandi INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Boneless Chicken Handi' AND IsDeleted=0);
DECLARE @CatBonelessSp INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Boneless Chicken Specials' AND IsDeleted=0);
DECLARE @CatKarahi INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Chicken Karahi' AND IsDeleted=0);
DECLARE @CatCurry INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Traditional Curry' AND IsDeleted=0);
DECLARE @CatMuttonBeef INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Mutton & Beef' AND IsDeleted=0);
DECLARE @CatRice INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Rice' AND IsDeleted=0);
DECLARE @CatSalan INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Regular Salan' AND IsDeleted=0);
DECLARE @CatDeals INT = (SELECT Id FROM AppCategories WHERE BranchId=@BranchId AND Name=N'Deals' AND IsDeleted=0);

/* ---------- Simple products (no size modifiers) ---------- */
DECLARE @Simple TABLE (
    Name NVARCHAR(256),
    Price DECIMAL(18,2),
    CategoryId INT,
    ImagePath NVARCHAR(512),
    IsDeal BIT
);

INSERT INTO @Simple (Name, Price, CategoryId, ImagePath, IsDeal) VALUES
-- BBQ Pieces
(N'Chicken Chest Piece', 400, @CatBbqPieces, N'https://images.unsplash.com/photo-1529193591184-b1d58069ecdd?auto=format&fit=crop&w=800&q=80', 0),
(N'White Chicken Chest Piece', 450, @CatBbqPieces, N'https://images.unsplash.com/photo-1529193591184-b1d58069ecdd?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Leg Piece', 350, @CatBbqPieces, N'https://images.unsplash.com/photo-1529193591184-b1d58069ecdd?auto=format&fit=crop&w=800&q=80', 0),
(N'White Chicken Leg Piece', 400, @CatBbqPieces, N'https://images.unsplash.com/photo-1529193591184-b1d58069ecdd?auto=format&fit=crop&w=800&q=80', 0),
-- BBQ Specialties
(N'Chicken Tikka Boti', 180, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Malai Boti', 240, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Seekh Kabab', 150, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Reshmi Kabab', 200, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Cheese Kabab', 220, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Afghani Boti', 200, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Kastoori Boti', 200, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
(N'Achari Boti', 200, @CatBbqSpecialties, N'https://images.unsplash.com/photo-1599487488170-d11ec9c172f2?auto=format&fit=crop&w=800&q=80', 0),
-- BBQ Platters
(N'Seekh Kabab Plate', 600, @CatBbqPlatters, N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 0),
(N'Reshmi Kabab Plate', 800, @CatBbqPlatters, N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 0),
(N'Cheese Kabab Plate', 900, @CatBbqPlatters, N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 0),
(N'Kastoori Boti Plate', 800, @CatBbqPlatters, N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 0),
(N'Achari Boti Plate', 800, @CatBbqPlatters, N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 0),
-- BBQ Fry (single items)
(N'Chicken Chest Piece Fry', 600, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Leg Piece Fry', 550, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'2 Tikka Seekh Fry', 600, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'2 Seekh Kabab Fry', 520, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'Malai Boti Fry', 700, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'1 Tikka Fry', 300, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'1 Seekh Kabab Fry', 260, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'1 Malai Boti Fry', 350, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Qeema Fry', 800, @CatBbqFry, N'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?auto=format&fit=crop&w=800&q=80', 0),
-- Tandoori
(N'Desi Gandam Roti', 20, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Sada Roti', 15, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Milky Khameeri Roti', 40, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Khameeri Roti', 30, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Roghni Naan', 50, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Special Roghni Naan', 70, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Garlic Naan', 100, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Kalonji Naan', 100, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
(N'Tandoori Paratha', 100, @CatTandoori, N'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=800&q=80', 0),
-- Salad / single
(N'Fresh Salad (Plate)', 150, @CatRaitaSalad, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 0),
-- Burgers
(N'Single Anda Shami Burger', 150, @CatBurgers, N'https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=800&q=80', 0),
(N'Double Anda Shami Burger', 250, @CatBurgers, N'https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Burger', 250, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
(N'Special Chicken Burger', 280, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
(N'Patty Burger', 280, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
(N'Regular Zinger Burger', 300, @CatBurgers, N'https://images.unsplash.com/photo-1606755962773-d324e0a13086?auto=format&fit=crop&w=800&q=80', 0),
(N'Large Zinger Burger', 350, @CatBurgers, N'https://images.unsplash.com/photo-1606755962773-d324e0a13086?auto=format&fit=crop&w=800&q=80', 0),
(N'Special Zinger Burger', 400, @CatBurgers, N'https://images.unsplash.com/photo-1606755962773-d324e0a13086?auto=format&fit=crop&w=800&q=80', 0),
(N'Kabab Burger', 300, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
(N'Pizza Burger', 500, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
(N'Grilled Burger', 500, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
(N'Stuffed Burger', 500, @CatBurgers, N'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&q=80', 0),
-- Shawarma
(N'RG Chicken Shawarma', 200, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'SP Chicken Shawarma', 220, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'RG Zinger Shawarma', 250, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'SP Zinger Shawarma', 270, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'Malai Boti Shawarma', 270, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'Turkish Shawarma', 270, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'Twister Shawarma', 270, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'Tikka Shawarma', 260, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'Kabab Shawarma', 250, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'RG Grilled Shawarma', 350, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
(N'SP Grilled Shawarma', 400, @CatShawarma, N'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=800&q=80', 0),
-- Rolls
(N'Chicken Paratha Roll', 250, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Zinger Paratha Roll', 300, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Malai Boti Paratha Roll', 300, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Kabab Paratha Roll', 280, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Turkish Paratha Roll', 300, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Tikka Paratha Roll', 300, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Shapata Roll', 400, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Spring Roll', 400, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
(N'Vegetable Spring Roll', 400, @CatRolls, N'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=80', 0),
-- Wraps
(N'Chicken Wrap', 400, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Zinger Wrap', 400, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Special Wrap', 500, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Tikka Wrap', 400, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Malai Boti Wrap', 400, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Kabab Wrap', 400, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'BBQ Special Wrap', 500, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Arabic Chicken Wrap', 450, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Arabic Zinger Wrap', 450, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
(N'Arabic Special Wrap', 500, @CatWraps, N'https://images.unsplash.com/photo-1626700051175-6818013e1d4f?auto=format&fit=crop&w=800&q=80', 0),
-- Pasta singles without F1/F2 pair
(N'Spicy Pasta', 400, @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', 0),
(N'Flaming Pasta', 750, @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', 0),
-- Boneless specials (single price)
(N'Patiala Chicken', 950, @CatBonelessSp, N'https://images.unsplash.com/photo-1603894584379-2f9c97c47aec?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Ginger', 750, @CatBonelessSp, N'https://images.unsplash.com/photo-1603894584379-2f9c97c47aec?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Jalfrezi Red', 700, @CatBonelessSp, N'https://images.unsplash.com/photo-1603894584379-2f9c97c47aec?auto=format&fit=crop&w=800&q=80', 0),
(N'Chicken Jalfrezi White', 770, @CatBonelessSp, N'https://images.unsplash.com/photo-1603894584379-2f9c97c47aec?auto=format&fit=crop&w=800&q=80', 0),
-- Traditional
(N'Chicken Qorma Plate', 450, @CatCurry, N'https://images.unsplash.com/photo-1588166524941-3bf61a9c41db?auto=format&fit=crop&w=800&q=80', 0),
-- Rice add-ons
(N'Shami Kabab Add on', 70, @CatRice, N'https://images.unsplash.com/photo-1563379091339-03b33674976c?auto=format&fit=crop&w=800&q=80', 0),
(N'Chapli Kabab Add on', 100, @CatRice, N'https://images.unsplash.com/photo-1563379091339-03b33674976c?auto=format&fit=crop&w=800&q=80', 0),
(N'Extra Chicken Piece Add on', 150, @CatRice, N'https://images.unsplash.com/photo-1563379091339-03b33674976c?auto=format&fit=crop&w=800&q=80', 0),
-- Salan singles
(N'Dal Chana Fry', 250, @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 0),
(N'Dal Mash Plate', 250, @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 0),
(N'Dal Mash Fry', 300, @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 0),
(N'Dal Makhni', 350, @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 0),
-- Deal
(N'1 Large Pizza + 1 Fries + Patty Burger + 1.5 L Drink', 1549, @CatDeals, N'https://images.unsplash.com/photo-1631515243349-e0cb75fb8d3a?auto=format&fit=crop&w=800&q=80', 1);

INSERT INTO AppProducts (
    TenantId, Name, Description, Price, WholesalePrice, CostPrice,
    AlertQuantityLimit, CategoryId, BrandId, UnitId, BranchId,
    ImagePath, Location, Specifications, Barcode,
    IsFreeDelivery, DealId, IsDeal, CreationTime, IsDeleted
)
SELECT
    @TenantId, s.Name, N'Al Meezan Foods menu', s.Price, s.Price, ROUND(s.Price * 0.70, 2),
    10, s.CategoryId, @BrandId, @UnitId, @BranchId,
    s.ImagePath, NULL, NULL, NULL,
    0, NULL, s.IsDeal, @Now, 0
FROM @Simple s;

/* ---------- Sized products: one product + Size/Portion modifiers ----------
   Product.Price = cheapest size (base)
   Option.Price  = absoluteSizePrice - base  (extra added in cart/POS)
*/
DECLARE @Sized TABLE (
    ProductName NVARCHAR(256),
    CategoryId INT,
    ImagePath NVARCHAR(512),
    GroupName NVARCHAR(128),
    OptionName NVARCHAR(128),
    AbsolutePrice DECIMAL(18,2),
    IsDefault BIT,
    SortOrder INT
);

INSERT INTO @Sized (ProductName, CategoryId, ImagePath, GroupName, OptionName, AbsolutePrice, IsDefault, SortOrder) VALUES
-- Pizza Size
(N'Chicken Tikka Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'Small', 450, 1, 1),
(N'Chicken Tikka Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 850, 0, 2),
(N'Chicken Tikka Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1150, 0, 3),
(N'Chicken Tikka Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 1700, 0, 4),

(N'Chicken Fajita Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'Small', 450, 1, 1),
(N'Chicken Fajita Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 850, 0, 2),
(N'Chicken Fajita Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1150, 0, 3),
(N'Chicken Fajita Pizza', @CatPizza, N'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 1700, 0, 4),

(N'Al Meezan Special Pizza', @CatPizza, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Small', 500, 1, 1),
(N'Al Meezan Special Pizza', @CatPizza, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 950, 0, 2),
(N'Al Meezan Special Pizza', @CatPizza, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1400, 0, 3),
(N'Al Meezan Special Pizza', @CatPizza, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 1900, 0, 4),

(N'Kabab Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'Small', 500, 1, 1),
(N'Kabab Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 950, 0, 2),
(N'Kabab Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1400, 0, 3),
(N'Kabab Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 1900, 0, 4),

(N'Malai Boti Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'Small', 500, 1, 1),
(N'Malai Boti Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 950, 0, 2),
(N'Malai Boti Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1400, 0, 3),
(N'Malai Boti Pizza', @CatPizza, N'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 1900, 0, 4),

-- Stuffed / Crown (Medium base)
(N'Chicken Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 1050, 1, 1),
(N'Chicken Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1500, 0, 2),
(N'Chicken Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 2000, 0, 3),
(N'Kabab Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 1050, 1, 1),
(N'Kabab Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1500, 0, 2),
(N'Kabab Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 2000, 0, 3),
(N'Cheese Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 1050, 1, 1),
(N'Cheese Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1500, 0, 2),
(N'Cheese Stuffed Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1574071318508-1cdbab80d264?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 2000, 0, 3),
(N'Crown Crust Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?auto=format&fit=crop&w=800&q=80', N'Size', N'Medium', 1050, 1, 1),
(N'Crown Crust Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 1500, 0, 2),
(N'Crown Crust Pizza', @CatStuffed, N'https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?auto=format&fit=crop&w=800&q=80', N'Size', N'XL', 2000, 0, 3),

-- Pasta F1/F2
(N'Al Meezan Special Pasta', @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', N'Portion', N'F1', 400, 1, 1),
(N'Al Meezan Special Pasta', @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', N'Portion', N'F2', 750, 0, 2),
(N'Crunchy Pasta', @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', N'Portion', N'F1', 400, 1, 1),
(N'Crunchy Pasta', @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', N'Portion', N'F2', 750, 0, 2),
(N'Creamy Pasta', @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', N'Portion', N'F1', 400, 1, 1),
(N'Creamy Pasta', @CatPasta, N'https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=80', N'Portion', N'F2', 750, 0, 2),

-- Fries
(N'Fries', @CatFries, N'https://images.unsplash.com/photo-1573080496219-bb080dd4f877?auto=format&fit=crop&w=800&q=80', N'Size', N'Half Plate', 150, 1, 1),
(N'Fries', @CatFries, N'https://images.unsplash.com/photo-1573080496219-bb080dd4f877?auto=format&fit=crop&w=800&q=80', N'Size', N'Full Plate', 250, 0, 2),
(N'Loaded Fries', @CatFries, N'https://images.unsplash.com/photo-1630384066600-11f4c02e1c3d?auto=format&fit=crop&w=800&q=80', N'Size', N'Small', 400, 1, 1),
(N'Loaded Fries', @CatFries, N'https://images.unsplash.com/photo-1630384066600-11f4c02e1c3d?auto=format&fit=crop&w=800&q=80', N'Size', N'Large', 750, 0, 2),

-- Wings / Nuggets
(N'Crispy Hot Wings', @CatWings, N'https://images.unsplash.com/photo-1527477396000-e2714b90eeab?auto=format&fit=crop&w=800&q=80', N'Pieces', N'6 Pcs', 400, 1, 1),
(N'Crispy Hot Wings', @CatWings, N'https://images.unsplash.com/photo-1527477396000-e2714b90eeab?auto=format&fit=crop&w=800&q=80', N'Pieces', N'12 Pcs', 750, 0, 2),
(N'Oven Baked Wings', @CatWings, N'https://images.unsplash.com/photo-1527477396000-e2714b90eeab?auto=format&fit=crop&w=800&q=80', N'Pieces', N'6 Pcs', 400, 1, 1),
(N'Oven Baked Wings', @CatWings, N'https://images.unsplash.com/photo-1527477396000-e2714b90eeab?auto=format&fit=crop&w=800&q=80', N'Pieces', N'12 Pcs', 750, 0, 2),
(N'Nuggets', @CatWings, N'https://images.unsplash.com/photo-1604908176997-125f25cc6f3d?auto=format&fit=crop&w=800&q=80', N'Pieces', N'6 Pcs', 350, 1, 1),
(N'Nuggets', @CatWings, N'https://images.unsplash.com/photo-1604908176997-125f25cc6f3d?auto=format&fit=crop&w=800&q=80', N'Pieces', N'12 Pcs', 650, 0, 2),

-- Raita
(N'Mint Raita', @CatRaitaSalad, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 60, 1, 1),
(N'Mint Raita', @CatRaitaSalad, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 100, 0, 2),

-- Handi Half/Full
(N'Special Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 900, 1, 1),
(N'Special Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1700, 0, 2),
(N'Makhni Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 900, 1, 1),
(N'Makhni Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1700, 0, 2),
(N'Hyderabadi Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 850, 1, 1),
(N'Hyderabadi Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1600, 0, 2),
(N'Madrasi Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 850, 1, 1),
(N'Madrasi Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1600, 0, 2),
(N'Achari Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 850, 1, 1),
(N'Achari Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1600, 0, 2),
(N'Chicken Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 850, 1, 1),
(N'Chicken Handi', @CatHandi, N'https://images.unsplash.com/photo-1585937421612-70a008356fbe?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1600, 0, 2),

-- Karahi
(N'Special Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 900, 1, 1),
(N'Special Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1700, 0, 2),
(N'Makhni Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 900, 1, 1),
(N'Makhni Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1700, 0, 2),
(N'Regular Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 800, 1, 1),
(N'Regular Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1500, 0, 2),
(N'Achari Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 850, 1, 1),
(N'Achari Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1600, 0, 2),
(N'Peshawari Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 850, 1, 1),
(N'Peshawari Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 1600, 0, 2),
(N'Desi Murgh Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 1800, 1, 1),
(N'Desi Murgh Karahi', @CatKarahi, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 3600, 0, 2),

-- Mutton & Beef
(N'Beef Karahi', @CatMuttonBeef, N'https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 1200, 1, 1),
(N'Beef Karahi', @CatMuttonBeef, N'https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 2300, 0, 2),
(N'Boneless Beef Karahi', @CatMuttonBeef, N'https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 1400, 1, 1),
(N'Boneless Beef Karahi', @CatMuttonBeef, N'https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 2600, 0, 2),
(N'Mutton Karahi', @CatMuttonBeef, N'https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half', 1800, 1, 1),
(N'Mutton Karahi', @CatMuttonBeef, N'https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full', 3500, 0, 2),

-- Rice
(N'Chicken Biryani', @CatRice, N'https://images.unsplash.com/photo-1631452180519-c014fe946bc7?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half Plate', 200, 1, 1),
(N'Chicken Biryani', @CatRice, N'https://images.unsplash.com/photo-1631452180519-c014fe946bc7?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full Plate', 350, 0, 2),
(N'Chicken Pulao', @CatRice, N'https://images.unsplash.com/photo-1589302168068-964664d93dc0?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half Plate', 200, 1, 1),
(N'Chicken Pulao', @CatRice, N'https://images.unsplash.com/photo-1589302168068-964664d93dc0?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full Plate', 350, 0, 2),

-- Salan portions
(N'Dal Chana', @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half Plate', 120, 1, 1),
(N'Dal Chana', @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full Plate', 200, 0, 2),
(N'Sabzi', @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', N'Portion', N'Half Plate', 120, 1, 1),
(N'Sabzi', @CatSalan, N'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', N'Portion', N'Full Plate', 200, 0, 2);

/* Insert one product per sized name (base price = min absolute) */
DECLARE @SizedProducts TABLE (
    ProductName NVARCHAR(256) PRIMARY KEY,
    CategoryId INT,
    ImagePath NVARCHAR(512),
    BasePrice DECIMAL(18,2),
    GroupName NVARCHAR(128),
    ProductId INT NULL
);

INSERT INTO @SizedProducts (ProductName, CategoryId, ImagePath, BasePrice, GroupName)
SELECT
    ProductName,
    MAX(CategoryId),
    MAX(ImagePath),
    MIN(AbsolutePrice),
    MAX(GroupName)
FROM @Sized
GROUP BY ProductName;

INSERT INTO AppProducts (
    TenantId, Name, Description, Price, WholesalePrice, CostPrice,
    AlertQuantityLimit, CategoryId, BrandId, UnitId, BranchId,
    ImagePath, Location, Specifications, Barcode,
    IsFreeDelivery, DealId, IsDeal, CreationTime, IsDeleted
)
SELECT
    @TenantId, sp.ProductName, N'Al Meezan Foods menu',
    sp.BasePrice, sp.BasePrice, ROUND(sp.BasePrice * 0.70, 2),
    10, sp.CategoryId, @BrandId, @UnitId, @BranchId,
    sp.ImagePath, NULL, NULL, NULL,
    0, NULL, 0, @Now, 0
FROM @SizedProducts sp;

UPDATE sp
SET sp.ProductId = p.Id
FROM @SizedProducts sp
INNER JOIN AppProducts p
    ON p.BranchId = @BranchId
   AND p.TenantId = @TenantId
   AND p.IsDeleted = 0
   AND p.Name = sp.ProductName;

/* Modifier groups (required, pick 1) */
INSERT INTO AppProductModifierGroups (
    TenantId, ProductId, Name, MinSelection, MaxSelection, IsRequired,
    CreationTime, IsDeleted
)
SELECT
    @TenantId, sp.ProductId, sp.GroupName, 1, 1, 1, @Now, 0
FROM @SizedProducts sp
WHERE sp.ProductId IS NOT NULL;

/* Options: Price = absolute - base */
INSERT INTO AppProductModifierOptions (
    TenantId, ModifierGroupId, Name, Price, IsDefault, CreationTime, IsDeleted
)
SELECT
    @TenantId,
    g.Id,
    s.OptionName,
    s.AbsolutePrice - sp.BasePrice,
    s.IsDefault,
    @Now,
    0
FROM @Sized s
INNER JOIN @SizedProducts sp ON sp.ProductName = s.ProductName
INNER JOIN AppProductModifierGroups g
    ON g.ProductId = sp.ProductId
   AND g.Name = s.GroupName
   AND g.IsDeleted = 0
ORDER BY s.ProductName, s.SortOrder;

COMMIT;

PRINT 'Al Meezan FINAL seed complete for TenantId=1 BranchId=2';

SELECT COUNT(*) AS Products FROM AppProducts WHERE BranchId=@BranchId AND TenantId=@TenantId AND IsDeleted=0;
SELECT COUNT(*) AS ModifierGroups FROM AppProductModifierGroups g
INNER JOIN AppProducts p ON p.Id=g.ProductId WHERE p.BranchId=@BranchId AND g.IsDeleted=0;
SELECT COUNT(*) AS ModifierOptions FROM AppProductModifierOptions o
INNER JOIN AppProductModifierGroups g ON g.Id=o.ModifierGroupId
INNER JOIN AppProducts p ON p.Id=g.ProductId
WHERE p.BranchId=@BranchId AND o.IsDeleted=0;

SELECT p.Name, p.Price AS BasePrice, g.Name AS ModifierGroup, o.Name AS OptionName, o.Price AS ExtraPrice, o.IsDefault
FROM AppProducts p
INNER JOIN AppProductModifierGroups g ON g.ProductId = p.Id AND g.IsDeleted = 0
INNER JOIN AppProductModifierOptions o ON o.ModifierGroupId = g.Id AND o.IsDeleted = 0
WHERE p.BranchId = @BranchId AND p.Name IN (N'Al Meezan Special Pizza', N'Fries', N'Special Handi', N'Crispy Hot Wings')
ORDER BY p.Name, o.Id;
