SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @TenantId INT = 2005;
DECLARE @UserId BIGINT = 30010;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- 1. Create Restaurant Branch
INSERT INTO [AppBranches] (
    [TenantId], [Name], [Code], [IsActive], [InvoiceAddress], [InvoiceContactEmail],
    [InvoiceContactPhone], [TaxNumber], [Website], [InvoiceFooter], [CreationTime],
    [CreatorUserId], [IsDeleted], [StatusId], [TaxPercent], [DiscountPercent],
    [DiscountAmount], [CurrencyFormat], [DateFormat], [BusinessType], [ServiceChargePercent]
) VALUES (
    @TenantId, N'Zohan Cafe & Restaurant', N'ZH-REST', 1,
    N'Plot 45-B, Commercial Avenue, DHA, Lahore', N'cafe@zohanpos.com',
    N'+92 300 1234567', N'NTN-9876543-1', N'https://zohancafe.com',
    N'Thank you for dining with us! Please come again.', @Now,
    @UserId, 0, 72, 5.00, 0.00,
    0.00, N'PKR', N'dd/MM/yyyy', 2, 5.00
);
DECLARE @RestaurantBranchId INT = SCOPE_IDENTITY();

-- 2. Create Retail Branch
INSERT INTO [AppBranches] (
    [TenantId], [Name], [Code], [IsActive], [InvoiceAddress], [InvoiceContactEmail],
    [InvoiceContactPhone], [TaxNumber], [Website], [InvoiceFooter], [CreationTime],
    [CreatorUserId], [IsDeleted], [StatusId], [TaxPercent], [DiscountPercent],
    [DiscountAmount], [CurrencyFormat], [DateFormat], [BusinessType], [ServiceChargePercent]
) VALUES (
    @TenantId, N'Zohan Retail Supermarket', N'ZH-RTL', 1,
    N'Shop #12-14, Ground Floor, Central Plaza, Lahore', N'retail@zohanpos.com',
    N'+92 300 7654321', N'NTN-9876543-2', N'https://zohanretail.com',
    N'Thank you for shopping with us! No return without receipt.', @Now,
    @UserId, 0, 72, 0.00, 0.00,
    0.00, N'PKR', N'dd/MM/yyyy', 1, 0.00
);
DECLARE @RetailBranchId INT = SCOPE_IDENTITY();

-- 3. Create Categories for Restaurant & Retail
INSERT INTO [AppCategories] ([TenantId], [Name], [Description], [CreationTime], [CreatorUserId], [IsDeleted], [BranchId], [IsActive])
VALUES
    (@TenantId, N'Burgers & Fast Food', N'Burgers, Wings, and Fast Food', @Now, @UserId, 0, @RestaurantBranchId, 1),
    (@TenantId, N'Pizza & Italian', N'Handcrafted Pizzas and Pastas', @Now, @UserId, 0, @RestaurantBranchId, 1),
    (@TenantId, N'Desi & Main Course', N'Biryani, Karahi, and Traditional meals', @Now, @UserId, 0, @RestaurantBranchId, 1),
    (@TenantId, N'Hot & Cold Beverages', N'Tea, Coffee, Shakes, and Mojitos', @Now, @UserId, 0, @RestaurantBranchId, 1),
    (@TenantId, N'Beverages & Soft Drinks', N'Bottled drinks, juices, mineral water', @Now, @UserId, 0, @RetailBranchId, 1),
    (@TenantId, N'Dairy & Breakfast', N'Milk, tea bags, butter, spreads', @Now, @UserId, 0, @RetailBranchId, 1),
    (@TenantId, N'Snacks & Confectionery', N'Chips, biscuits, cookies, chocolates', @Now, @UserId, 0, @RetailBranchId, 1),
    (@TenantId, N'Personal & Home Care', N'Soaps, shampoo, detergents', @Now, @UserId, 0, @RetailBranchId, 1);

DECLARE @CatFastFood INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Burgers & Fast Food');
DECLARE @CatPizza INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Pizza & Italian');
DECLARE @CatDesi INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Desi & Main Course');
DECLARE @CatCafeDrinks INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Hot & Cold Beverages');

DECLARE @CatRetailDrinks INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Beverages & Soft Drinks');
DECLARE @CatDairy INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Dairy & Breakfast');
DECLARE @CatSnacks INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Snacks & Confectionery');
DECLARE @CatHomeCare INT = (SELECT TOP 1 Id FROM [AppCategories] WHERE [TenantId] = @TenantId AND [Name] = N'Personal & Home Care');

-- 4. Brands & Units
INSERT INTO [AppBrands] ([TenantId], [Name], [Description], [CreationTime], [CreatorUserId], [IsDeleted], [BranchId], [IsActive])
VALUES
    (@TenantId, N'Chef Special', N'In-house kitchen preparation', @Now, @UserId, 0, @RestaurantBranchId, 1),
    (@TenantId, N'Coca-Cola Co.', N'The Coca-Cola Company', @Now, @UserId, 0, @RetailBranchId, 1),
    (@TenantId, N'Nestle', N'Nestle Global Brands', @Now, @UserId, 0, @RetailBranchId, 1),
    (@TenantId, N'Unilever', N'Unilever Consumer Goods', @Now, @UserId, 0, @RetailBranchId, 1),
    (@TenantId, N'PepsiCo / Frito-Lay', N'Snacks and beverages', @Now, @UserId, 0, @RetailBranchId, 1);

DECLARE @BrandChef INT = (SELECT TOP 1 Id FROM [AppBrands] WHERE [TenantId] = @TenantId AND [Name] = N'Chef Special');
DECLARE @BrandCoke INT = (SELECT TOP 1 Id FROM [AppBrands] WHERE [TenantId] = @TenantId AND [Name] = N'Coca-Cola Co.');
DECLARE @BrandNestle INT = (SELECT TOP 1 Id FROM [AppBrands] WHERE [TenantId] = @TenantId AND [Name] = N'Nestle');
DECLARE @BrandUnilever INT = (SELECT TOP 1 Id FROM [AppBrands] WHERE [TenantId] = @TenantId AND [Name] = N'Unilever');
DECLARE @BrandPepsi INT = (SELECT TOP 1 Id FROM [AppBrands] WHERE [TenantId] = @TenantId AND [Name] = N'PepsiCo / Frito-Lay');

INSERT INTO [AppUnits] ([TenantId], [Name], [Description], [CreationTime], [CreatorUserId], [IsDeleted], [BranchId], [Symbol], [IsActive])
VALUES
    (@TenantId, N'Serving', N'Single portion / serving', @Now, @UserId, 0, @RestaurantBranchId, N'srv', 1),
    (@TenantId, N'Cup', N'Beverage cup', @Now, @UserId, 0, @RestaurantBranchId, N'cup', 1),
    (@TenantId, N'Bottle', N'Packaged bottle', @Now, @UserId, 0, @RetailBranchId, N'btl', 1),
    (@TenantId, N'Packet', N'Packaged unit', @Now, @UserId, 0, @RetailBranchId, N'pkt', 1);

DECLARE @UnitServing INT = (SELECT TOP 1 Id FROM [AppUnits] WHERE [TenantId] = @TenantId AND [Name] = N'Serving');
DECLARE @UnitCup INT = (SELECT TOP 1 Id FROM [AppUnits] WHERE [TenantId] = @TenantId AND [Name] = N'Cup');
DECLARE @UnitBottle INT = (SELECT TOP 1 Id FROM [AppUnits] WHERE [TenantId] = @TenantId AND [Name] = N'Bottle');
DECLARE @UnitPacket INT = (SELECT TOP 1 Id FROM [AppUnits] WHERE [TenantId] = @TenantId AND [Name] = N'Packet');

-- 5. Restaurant Tables for Zohan Cafe & Restaurant
INSERT INTO [AppRestaurantTables] ([TenantId], [BranchId], [TableNo], [Area], [Capacity], [Status], [IsActive], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @RestaurantBranchId, N'Table T1', N'Main Hall', 4, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Table T2', N'Main Hall', 4, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Table T3', N'Main Hall', 6, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Table T4', N'Main Hall', 2, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Family F1', N'Family Section', 6, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Family F2', N'Family Section', 8, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Rooftop R1', N'Rooftop Terrace', 4, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Rooftop R2', N'Rooftop Terrace', 4, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'VIP Lounge', N'VIP Suite', 10, 0, 1, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, N'Garden G1', N'Outdoor Garden', 4, 0, 1, @Now, @UserId, 0);

-- 6. Insert 10 Restaurant Products
DECLARE @P_Zinger INT, @P_BeefBurger INT, @P_Pizza INT, @P_Pasta INT, @P_Biryani INT;
DECLARE @P_Wings INT, @P_Fries INT, @P_Tea INT, @P_Coffee INT, @P_Margarita INT;

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Crispy Zinger Crunch Burger', N'Crispy spiced chicken fillet topped with fresh lettuce and signature sauce in a toasted bun', 580.00, 380.00, 520.00, N'FD-ZG-01', @CatFastFood, @BrandChef, @UnitServing, @RestaurantBranchId, 10, 0, @Now, @UserId);
SET @P_Zinger = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Double Smoky Beef Cheese Burger', N'Two prime beef patties grilled to perfection with melted cheddar, caramelized onions and BBQ relish', 780.00, 520.00, 700.00, N'FD-BF-02', @CatFastFood, @BrandChef, @UnitServing, @RestaurantBranchId, 10, 0, @Now, @UserId);
SET @P_BeefBurger = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Chicken Tikka Supreme Pizza (Large)', N'14 inch stone-baked pizza loaded with spicy chicken tikka, mozzarella, capsicum, onions and black olives', 1450.00, 920.00, 1300.00, N'FD-PZ-03', @CatPizza, @BrandChef, @UnitServing, @RestaurantBranchId, 5, 0, @Now, @UserId);
SET @P_Pizza = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Fettuccine Alfredo with Grilled Chicken', N'Creamy parmesan white sauce pasta served with seasoned tender sliced grilled chicken and garlic bread', 890.00, 580.00, 800.00, N'FD-PS-04', @CatPizza, @BrandChef, @UnitServing, @RestaurantBranchId, 5, 0, @Now, @UserId);
SET @P_Pasta = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Special Chicken Dum Biryani', N'Fragrant long-grain basmati rice cooked on slow dum with aromatic spices and juicy chicken cuts, served with raita', 480.00, 310.00, 420.00, N'FD-BY-05', @CatDesi, @BrandChef, @UnitServing, @RestaurantBranchId, 10, 0, @Now, @UserId);
SET @P_Biryani = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Spicy Buffalo Wings (6 Pieces)', N'Golden fried crispy chicken wings tossed in hot tangy buffalo sauce served with ranch dip', 420.00, 260.00, 380.00, N'FD-WG-06', @CatFastFood, @BrandChef, @UnitServing, @RestaurantBranchId, 10, 0, @Now, @UserId);
SET @P_Wings = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Loaded Cheesy Curly Fries', N'Crisp potato curly fries drizzled with warm cheddar cheese sauce, jalapeno slices and crushed bacon bits', 380.00, 220.00, 340.00, N'FD-FR-07', @CatFastFood, @BrandChef, @UnitServing, @RestaurantBranchId, 10, 0, @Now, @UserId);
SET @P_Fries = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Special Doodh Patti Karak Chai', N'Traditional slow-brewed rich milk tea made with premium tea leaves, cardamom and pure milk', 140.00, 65.00, 110.00, N'BV-CH-08', @CatCafeDrinks, @BrandChef, @UnitCup, @RestaurantBranchId, 20, 0, @Now, @UserId);
SET @P_Tea = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Spanish Latte / Cold Brew Coffee', N'Double espresso shot shaken with cold condensed milk and chilled creamy foam over ice cubes', 460.00, 240.00, 400.00, N'BV-CF-09', @CatCafeDrinks, @BrandChef, @UnitCup, @RestaurantBranchId, 10, 0, @Now, @UserId);
SET @P_Coffee = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Fresh Mint Lemonade Margarita', N'Refreshing blend of fresh garden mint, tangy lemon juice, crushed ice and sparkling soda', 290.00, 130.00, 250.00, N'BV-MG-10', @CatCafeDrinks, @BrandChef, @UnitCup, @RestaurantBranchId, 15, 0, @Now, @UserId);
SET @P_Margarita = SCOPE_IDENTITY();

-- 7. Insert Modifiers for Restaurant items
-- Modifiers for Zinger Burger
INSERT INTO [AppProductModifierGroups] ([TenantId], [ProductId], [Name], [MinSelection], [MaxSelection], [IsRequired], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES (@TenantId, @P_Zinger, N'Spice Level', 1, 1, 1, @Now, @UserId, 0);
DECLARE @ModGrpZingerSpice INT = SCOPE_IDENTITY();

INSERT INTO [AppProductModifierOptions] ([TenantId], [ModifierGroupId], [Name], [Price], [IsDefault], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @ModGrpZingerSpice, N'Mild & Crispy', 0.00, 1, @Now, @UserId, 0),
    (@TenantId, @ModGrpZingerSpice, N'Medium Spicy', 0.00, 0, @Now, @UserId, 0),
    (@TenantId, @ModGrpZingerSpice, N'Extra Hot Ghost Pepper', 30.00, 0, @Now, @UserId, 0);

INSERT INTO [AppProductModifierGroups] ([TenantId], [ProductId], [Name], [MinSelection], [MaxSelection], [IsRequired], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES (@TenantId, @P_Zinger, N'Burger Add-ons', 0, 3, 0, @Now, @UserId, 0);
DECLARE @ModGrpZingerAddons INT = SCOPE_IDENTITY();

INSERT INTO [AppProductModifierOptions] ([TenantId], [ModifierGroupId], [Name], [Price], [IsDefault], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @ModGrpZingerAddons, N'Extra Cheddar Cheese Slice', 60.00, 0, @Now, @UserId, 0),
    (@TenantId, @ModGrpZingerAddons, N'Fried Egg', 50.00, 0, @Now, @UserId, 0),
    (@TenantId, @ModGrpZingerAddons, N'Extra Garlic Mayo Dip', 40.00, 0, @Now, @UserId, 0);

-- Modifiers for Pizza
INSERT INTO [AppProductModifierGroups] ([TenantId], [ProductId], [Name], [MinSelection], [MaxSelection], [IsRequired], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES (@TenantId, @P_Pizza, N'Crust Choice', 1, 1, 1, @Now, @UserId, 0);
DECLARE @ModGrpCrust INT = SCOPE_IDENTITY();

INSERT INTO [AppProductModifierOptions] ([TenantId], [ModifierGroupId], [Name], [Price], [IsDefault], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @ModGrpCrust, N'Traditional Pan Crust', 0.00, 1, @Now, @UserId, 0),
    (@TenantId, @ModGrpCrust, N'Thin & Crispy Italian Crust', 0.00, 0, @Now, @UserId, 0),
    (@TenantId, @ModGrpCrust, N'Cheese Stuffed Crust', 250.00, 0, @Now, @UserId, 0);

-- Modifiers for Coffee
INSERT INTO [AppProductModifierGroups] ([TenantId], [ProductId], [Name], [MinSelection], [MaxSelection], [IsRequired], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES (@TenantId, @P_Coffee, N'Milk Preference', 1, 1, 1, @Now, @UserId, 0);
DECLARE @ModGrpMilk INT = SCOPE_IDENTITY();

INSERT INTO [AppProductModifierOptions] ([TenantId], [ModifierGroupId], [Name], [Price], [IsDefault], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @ModGrpMilk, N'Full Cream Dairy Milk', 0.00, 1, @Now, @UserId, 0),
    (@TenantId, @ModGrpMilk, N'Almond Milk', 120.00, 0, @Now, @UserId, 0),
    (@TenantId, @ModGrpMilk, N'Oat Milk', 140.00, 0, @Now, @UserId, 0);

-- 8. Insert 10 Retail Products
DECLARE @P_Coke INT, @P_Water INT, @P_Lipton INT, @P_Olpers INT, @P_Ketchup INT;
DECLARE @P_Lays INT, @P_Oreo INT, @P_Dettol INT, @P_Shampoo INT, @P_Ariel INT;

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Coca-Cola 1.5 Litre PET Bottle', N'Original carbonated cola soft drink 1.5L bottle', 180.00, 155.00, 165.00, N'RTL-CC-15', @CatRetailDrinks, @BrandCoke, @UnitBottle, @RetailBranchId, 24, 0, @Now, @UserId);
SET @P_Coke = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Nestle Pure Life Mineral Water 1.5 Litre', N'Purified drinking mineral water bottled with essential minerals', 100.00, 78.00, 88.00, N'RTL-NPL-15', @CatRetailDrinks, @BrandNestle, @UnitBottle, @RetailBranchId, 30, 0, @Now, @UserId);
SET @P_Water = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Lipton Yellow Label Black Tea 475g Pack', N'Premium rich aroma and robust flavor Ceylon black tea leaves box', 1150.00, 960.00, 1050.00, N'RTL-LPT-475', @CatDairy, @BrandUnilever, @UnitPacket, @RetailBranchId, 10, 0, @Now, @UserId);
SET @P_Lipton = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Olpers Full Cream UHT Milk 1 Litre Tetra Pak', N'Pure and rich homogenized full cream dairy milk with vitamins', 290.00, 258.00, 275.00, N'RTL-OLP-1L', @CatDairy, @BrandNestle, @UnitPacket, @RetailBranchId, 24, 0, @Now, @UserId);
SET @P_Olpers = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'National Tomato Ketchup 800g Value Pouch', N'Thick sweet and tangy tomato ketchup made from ripe red tomatoes', 420.00, 345.00, 380.00, N'RTL-NTK-800', @CatDairy, @BrandUnilever, @UnitPacket, @RetailBranchId, 15, 0, @Now, @UserId);
SET @P_Ketchup = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Lays Masala Potato Chips 65g Party Pack', N'Crispy wavy potato chips spiced with authentic desi chatpata masala', 100.00, 80.00, 88.00, N'RTL-LAYS-MS', @CatSnacks, @BrandPepsi, @UnitPacket, @RetailBranchId, 40, 0, @Now, @UserId);
SET @P_Lays = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'LU Oreo Original Vanilla Creme Sandwich Cookies', N'Rich cocoa sandwich biscuits filled with smooth sweet vanilla cream', 60.00, 47.00, 52.00, N'RTL-OREO-01', @CatSnacks, @BrandPepsi, @UnitPacket, @RetailBranchId, 50, 0, @Now, @UserId);
SET @P_Oreo = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Dettol Original Anti-Bacterial Bath Soap 135g', N'Germ protection bath soap bar enriched with classic pine fragrance', 180.00, 148.00, 160.00, N'RTL-DTL-135', @CatHomeCare, @BrandUnilever, @UnitPacket, @RetailBranchId, 30, 0, @Now, @UserId);
SET @P_Dettol = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Head & Shoulders Anti-Dandruff Shampoo 360ml', N'Clinically proven anti-dandruff hair shampoo with refreshing cool menthol', 790.00, 640.00, 710.00, N'RTL-HNS-360', @CatHomeCare, @BrandUnilever, @UnitBottle, @RetailBranchId, 12, 0, @Now, @UserId);
SET @P_Shampoo = SCOPE_IDENTITY();

INSERT INTO [AppProducts] ([TenantId], [Name], [Description], [Price], [CostPrice], [WholesalePrice], [Barcode], [CategoryId], [BrandId], [UnitId], [BranchId], [AlertQuantityLimit], [IsDeleted], [CreationTime], [CreatorUserId])
VALUES (@TenantId, N'Ariel Sunrise Fresh Detergent Powder 1kg', N'Advanced deep cleaning laundry detergent powder with stain removal formula', 620.00, 510.00, 560.00, N'RTL-ARL-1KG', @CatHomeCare, @BrandUnilever, @UnitPacket, @RetailBranchId, 15, 0, @Now, @UserId);
SET @P_Ariel = SCOPE_IDENTITY();

-- 9. Insert Stock for Restaurant Products (100 units each in Restaurant branch)
INSERT INTO [AppBranchStocks] ([TenantId], [BranchId], [ProductId], [Quantity], [Price], [CostPrice], [WholesalePrice], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @RestaurantBranchId, @P_Zinger, 100, 580.00, 380.00, 520.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_BeefBurger, 80, 780.00, 520.00, 700.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Pizza, 60, 1450.00, 920.00, 1300.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Pasta, 70, 890.00, 580.00, 800.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Biryani, 120, 480.00, 310.00, 420.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Wings, 90, 420.00, 260.00, 380.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Fries, 150, 380.00, 220.00, 340.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Tea, 250, 140.00, 65.00, 110.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Coffee, 100, 460.00, 240.00, 400.00, @Now, @UserId, 0),
    (@TenantId, @RestaurantBranchId, @P_Margarita, 120, 290.00, 130.00, 250.00, @Now, @UserId, 0);

-- 10. Insert Stock for Retail Products (100 units each in Retail branch)
INSERT INTO [AppBranchStocks] ([TenantId], [BranchId], [ProductId], [Quantity], [Price], [CostPrice], [WholesalePrice], [CreationTime], [CreatorUserId], [IsDeleted])
VALUES
    (@TenantId, @RetailBranchId, @P_Coke, 150, 180.00, 155.00, 165.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Water, 200, 100.00, 78.00, 88.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Lipton, 50, 1150.00, 960.00, 1050.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Olpers, 100, 290.00, 258.00, 275.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Ketchup, 60, 420.00, 345.00, 380.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Lays, 180, 100.00, 80.00, 88.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Oreo, 200, 60.00, 47.00, 52.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Dettol, 120, 180.00, 148.00, 160.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Shampoo, 45, 790.00, 640.00, 710.00, @Now, @UserId, 0),
    (@TenantId, @RetailBranchId, @P_Ariel, 60, 620.00, 510.00, 560.00, @Now, @UserId, 0);

COMMIT TRANSACTION;

SELECT 
    b.Id as BranchId, 
    b.Name as BranchName, 
    b.Code as BranchCode, 
    b.BusinessType,
    (SELECT COUNT(*) FROM AppProducts p WHERE p.BranchId = b.Id) as ProductCount,
    (SELECT COUNT(*) FROM AppRestaurantTables t WHERE t.BranchId = b.Id) as TableCount
FROM AppBranches b
WHERE b.Id IN (@RestaurantBranchId, @RetailBranchId);
