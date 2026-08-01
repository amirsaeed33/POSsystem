using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartPos.Brands;
using SmartPos.Categories;
using SmartPos.Products;
using SmartPos.Units;

namespace SmartPos.EntityFrameworkCore.Seed.Host
{
    /// <summary>
    /// Seeds demo bakery + general store master data (categories, brands, units, ~100 products).
    /// Soft-deletes legacy bike spare-parts demo products (BIKE-* barcodes) when present.
    /// </summary>
    public class BakeryGeneralStoreDemoDataCreator
    {
        private const string BarcodePrefix = "STORE-";

        private readonly SmartPosDbContext _context;
        private readonly int? _tenantId;

        public BakeryGeneralStoreDemoDataCreator(SmartPosDbContext context, int? tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            SoftDeleteLegacyBikeProducts();

            var unitPiece = EnsureUnit("Piece", "Single piece");
            var unitPack = EnsureUnit("Pack", "Pack / multipack");
            var unitLitre = EnsureUnit("Litre", "Liquid volume");
            var unitKg = EnsureUnit("Kg", "Weight in kilograms");
            var unitDozen = EnsureUnit("Dozen", "12 pieces");
            var unitPacket = EnsureUnit("Packet", "Sealed packet");

            var catBread = EnsureCategory("Bread & Buns", "Loaves, buns, and rolls");
            var catCakes = EnsureCategory("Cakes & Pastries", "Cakes, muffins, and pastries");
            var catCookies = EnsureCategory("Cookies & Biscuits", "Cookies, biscuits, and rusk");
            var catDairy = EnsureCategory("Dairy", "Milk, yogurt, cheese, butter");
            var catSweets = EnsureCategory("Sweets", "Traditional and bakery sweets");
            var catBeverages = EnsureCategory("Beverages", "Tea, coffee, juices, soft drinks");
            var catSnacks = EnsureCategory("Snacks", "Chips, namkeen, and savory snacks");
            var catGrocery = EnsureCategory("Grocery Staples", "Flour, sugar, rice, oil, spices");
            var catBreakfast = EnsureCategory("Breakfast", "Cereals, spreads, and breakfast items");
            var catHousehold = EnsureCategory("Household", "Cleaning and everyday household");
            var catPersonal = EnsureCategory("Personal Care", "Soap, shampoo, and hygiene");

            var brandDawn = EnsureBrand("Dawn", "Dawn bakery");
            var brandBakeParlor = EnsureBrand("Bake Parlor", "Bake Parlor");
            var brandBreadGarden = EnsureBrand("Bread Garden", "Fresh bakery brand");
            var brandOlpers = EnsureBrand("Olper's", "Olper's dairy");
            var brandNestle = EnsureBrand("Nestle", "Nestle");
            var brandTapal = EnsureBrand("Tapal", "Tapal tea");
            var brandLipton = EnsureBrand("Lipton", "Lipton");
            var brandLux = EnsureBrand("Lux", "Lux personal care");
            var brandSurf = EnsureBrand("Surf Excel", "Surf Excel detergents");
            var brandNational = EnsureBrand("National", "National foods");
            var brandShan = EnsureBrand("Shan", "Shan foods");
            var brandLays = EnsureBrand("Lays", "Lays chips");
            var brandKurkure = EnsureBrand("Kurkure", "Kurkure snacks");
            var brandMitchells = EnsureBrand("Mitchell's", "Mitchell's jams & sauces");
            var brandStore = EnsureBrand("Store Brand", "House / store brand");

            _context.SaveChanges();

            var catalog = BuildCatalog(
                unitPiece, unitPack, unitLitre, unitKg, unitDozen, unitPacket,
                catBread, catCakes, catCookies, catDairy, catSweets, catBeverages,
                catSnacks, catGrocery, catBreakfast, catHousehold, catPersonal,
                brandDawn, brandBakeParlor, brandBreadGarden, brandOlpers, brandNestle,
                brandTapal, brandLipton, brandLux, brandSurf, brandNational, brandShan,
                brandLays, brandKurkure, brandMitchells, brandStore);

            var existingNames = _context.Products.IgnoreQueryFilters()
                .Where(x => x.TenantId == _tenantId && !x.IsDeleted)
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var index = NextStoreBarcodeIndex();
            foreach (var item in catalog)
            {
                if (existingNames.Contains(item.Name))
                {
                    continue;
                }

                _context.Products.Add(new Product
                {
                    TenantId = _tenantId,
                    Name = item.Name,
                    Description = item.Description,
                    Barcode = BarcodePrefix + index.ToString("D5"),
                    Price = item.Price,
                    WholesalePrice = Math.Round(item.Price * 0.85m, 2),
                    CostPrice = Math.Round(item.Price * 0.7m, 2),
                    StockQuantity = item.Stock,
                    AlertQuantityLimit = item.Alert,
                    CategoryId = item.CategoryId,
                    BrandId = item.BrandId,
                    UnitId = item.UnitId
                });
                existingNames.Add(item.Name);
                index++;
            }

            _context.SaveChanges();
        }

        private int NextStoreBarcodeIndex()
        {
            var barcodes = _context.Products.IgnoreQueryFilters()
                .Where(x => x.TenantId == _tenantId && x.Barcode != null && x.Barcode.StartsWith(BarcodePrefix))
                .Select(x => x.Barcode)
                .ToList();

            var max = 0;
            foreach (var barcode in barcodes)
            {
                var suffix = barcode.Substring(BarcodePrefix.Length);
                if (int.TryParse(suffix, out var n) && n > max)
                {
                    max = n;
                }
            }

            return max + 1;
        }

        private void SoftDeleteLegacyBikeProducts()
        {
            var legacy = _context.Products.IgnoreQueryFilters()
                .Where(x => x.TenantId == _tenantId && !x.IsDeleted && x.Barcode != null && x.Barcode.StartsWith("BIKE-"))
                .ToList();

            if (legacy.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var product in legacy)
            {
                product.IsDeleted = true;
                product.DeletionTime = now;
            }

            _context.SaveChanges();
        }

        private Unit EnsureUnit(string name, string description)
        {
            var existing = _context.Units.IgnoreQueryFilters()
                .FirstOrDefault(x => x.TenantId == _tenantId && !x.IsDeleted && x.Name == name);
            if (existing != null)
            {
                return existing;
            }

            var unit = new Unit
            {
                TenantId = _tenantId,
                Name = name,
                Description = description
            };
            _context.Units.Add(unit);
            _context.SaveChanges();
            return unit;
        }

        private Category EnsureCategory(string name, string description)
        {
            var existing = _context.Categories.IgnoreQueryFilters()
                .FirstOrDefault(x => x.TenantId == _tenantId && !x.IsDeleted && x.Name == name);
            if (existing != null)
            {
                return existing;
            }

            var category = new Category
            {
                TenantId = _tenantId,
                Name = name,
                Description = description
            };
            _context.Categories.Add(category);
            _context.SaveChanges();
            return category;
        }

        private Brand EnsureBrand(string name, string description)
        {
            var existing = _context.Brands.IgnoreQueryFilters()
                .FirstOrDefault(x => x.TenantId == _tenantId && !x.IsDeleted && x.Name == name);
            if (existing != null)
            {
                return existing;
            }

            var brand = new Brand
            {
                TenantId = _tenantId,
                Name = name,
                Description = description
            };
            _context.Brands.Add(brand);
            _context.SaveChanges();
            return brand;
        }

        private static List<SeedProduct> BuildCatalog(
            Unit piece, Unit pack, Unit litre, Unit kg, Unit dozen, Unit packet,
            Category bread, Category cakes, Category cookies, Category dairy, Category sweets, Category beverages,
            Category snacks, Category grocery, Category breakfast, Category household, Category personal,
            Brand dawn, Brand bakeParlor, Brand breadGarden, Brand olpers, Brand nestle,
            Brand tapal, Brand lipton, Brand lux, Brand surf, Brand national, Brand shan,
            Brand lays, Brand kurkure, Brand mitchells, Brand store)
        {
            return new List<SeedProduct>
            {
                // Popular bakery / dairy sale items (English names)
                P("Milk", "Fresh full cream milk 1 litre", 280, 120, 25, dairy, olpers, litre),
                P("Butter", "Salted butter 200g", 320, 80, 15, dairy, store, packet),
                P("Desi Ghee", "Pure desi ghee 500g", 950, 45, 8, dairy, store, packet),
                P("Barfi", "Milk barfi sweet per piece", 60, 150, 30, sweets, breadGarden, piece),

                // Bread & Buns
                P("Milk Bread Large", "Soft white milk bread loaf", 160, 80, 15, bread, dawn, piece),
                P("Milk Bread Small", "Small milk bread loaf", 90, 100, 20, bread, dawn, piece),
                P("Brown Bread", "Whole wheat brown bread", 180, 60, 12, bread, dawn, piece),
                P("Sandwich Bread", "Sliced sandwich loaf", 170, 70, 14, bread, bakeParlor, piece),
                P("Burger Bun Pack 4", "Soft burger buns (4 pcs)", 120, 90, 18, bread, bakeParlor, pack),
                P("Hot Dog Bun Pack 4", "Hot dog rolls (4 pcs)", 130, 75, 15, bread, bakeParlor, pack),
                P("Dinner Rolls Pack 6", "Soft dinner rolls", 140, 65, 12, bread, breadGarden, pack),
                P("Garlic Bread Stick", "Garlic flavored bread stick", 110, 55, 10, bread, breadGarden, piece),
                P("Fruit Bun", "Sweet fruit bun", 50, 120, 25, bread, dawn, piece),
                P("Plain Kulcha Pack 4", "Soft kulcha bread", 100, 70, 14, bread, store, pack),

                // Cakes & Pastries
                P("Chocolate Cupcake", "Chocolate frosted cupcake", 80, 90, 20, cakes, breadGarden, piece),
                P("Vanilla Cupcake", "Vanilla frosted cupcake", 75, 90, 20, cakes, breadGarden, piece),
                P("Cream Donut", "Filled cream donut", 90, 70, 15, cakes, dawn, piece),
                P("Chocolate Donut", "Chocolate glazed donut", 95, 70, 15, cakes, dawn, piece),
                P("Croissant Plain", "Butter croissant", 120, 50, 10, cakes, bakeParlor, piece),
                P("Chocolate Pastry", "Chocolate cream pastry", 150, 40, 8, cakes, breadGarden, piece),
                P("Pineapple Pastry", "Pineapple cream pastry", 140, 40, 8, cakes, breadGarden, piece),
                P("Muffin Chocolate", "Chocolate muffin", 100, 55, 10, cakes, bakeParlor, piece),
                P("Muffin Blueberry", "Blueberry muffin", 110, 50, 10, cakes, bakeParlor, piece),
                P("Slice Cake Chocolate", "Chocolate cake slice", 180, 35, 7, cakes, dawn, piece),

                // Cookies & Biscuits
                P("Sooper Biscuits Pack", "Classic sooper-style biscuits", 50, 150, 30, cookies, store, pack),
                P("Chocolate Chip Cookies", "Chocolate chip cookies pack", 180, 80, 15, cookies, nestle, pack),
                P("Butter Cookies Tin", "Butter cookies assortment", 450, 40, 8, cookies, store, pack),
                P("Rusk Plain Large", "Tea rusk large pack", 220, 60, 12, cookies, bakeParlor, pack),
                P("Rusk Zeera", "Zeera flavored rusk", 240, 55, 10, cookies, bakeParlor, pack),
                P("Glucose Biscuits", "Glucose energy biscuits", 40, 200, 40, cookies, nestle, pack),
                P("Cream Biscuits Chocolate", "Chocolate cream biscuits", 60, 140, 25, cookies, nestle, pack),
                P("Jam Sandwich Biscuits", "Fruit jam filled biscuits", 70, 120, 20, cookies, store, pack),

                // Dairy
                P("Olper's Milk 1L", "Full cream milk 1 litre", 280, 100, 20, dairy, olpers, litre),
                P("Olper's Milk 500ml", "Full cream milk 500ml", 150, 120, 25, dairy, olpers, litre),
                P("Olper's Cream 200ml", "Dairy cream carton", 180, 70, 15, dairy, olpers, piece),
                P("Nestle Yogurt Plain 400g", "Plain yogurt cup", 160, 80, 15, dairy, nestle, piece),
                P("Nestle Yogurt Sweet 400g", "Sweet yogurt cup", 170, 80, 15, dairy, nestle, piece),
                P("Butter 200g", "Salted butter pack", 320, 50, 10, dairy, store, packet),
                P("Cheese Slices Pack 10", "Processed cheese slices", 380, 45, 8, dairy, nestle, pack),
                P("Eggs Farm Fresh Dozen", "Farm fresh eggs", 360, 60, 12, dairy, store, dozen),

                // Beverages
                P("Tapal Danedar 900g", "Black tea family pack", 980, 40, 8, beverages, tapal, pack),
                P("Tapal Danedar 200g", "Black tea small pack", 280, 80, 15, beverages, tapal, pack),
                P("Lipton Yellow Label 200g", "Tea carton", 320, 70, 14, beverages, lipton, pack),
                P("Nestle Everyday 400g", "Tea whitener", 520, 55, 10, beverages, nestle, pack),
                P("Nestle Nido 400g", "Milk powder", 980, 35, 7, beverages, nestle, pack),
                P("Soft Drink 1.5L Cola", "Cola soft drink bottle", 180, 90, 18, beverages, store, litre),
                P("Soft Drink 1.5L Lemon", "Lemon soft drink bottle", 180, 90, 18, beverages, store, litre),
                P("Mineral Water 1.5L", "Bottled drinking water", 80, 150, 30, beverages, store, litre),
                P("Orange Juice 1L", "Ready-to-drink orange juice", 260, 50, 10, beverages, mitchells, litre),
                P("Mango Juice 1L", "Ready-to-drink mango juice", 270, 50, 10, beverages, mitchells, litre),

                // Snacks
                P("Lays Classic 30g", "Salted potato chips", 50, 200, 40, snacks, lays, pack),
                P("Lays Masala 30g", "Masala potato chips", 50, 200, 40, snacks, lays, pack),
                P("Lays French Cheese 30g", "Cheese flavor chips", 50, 180, 35, snacks, lays, pack),
                P("Kurkure Chutney Chaska", "Corn snack", 50, 180, 35, snacks, kurkure, pack),
                P("Kurkure Masala Munch", "Spicy corn snack", 50, 180, 35, snacks, kurkure, pack),
                P("Nimco Mix 200g", "Savory nimco mix", 180, 70, 14, snacks, store, pack),
                P("Popcorn Butter 100g", "Butter popcorn pack", 90, 80, 15, snacks, store, pack),
                P("Peanuts Roasted 200g", "Roasted salted peanuts", 160, 65, 12, snacks, store, pack),

                // Grocery staples
                P("Wheat Flour 5kg", "Fine atta / wheat flour", 620, 50, 10, grocery, store, kg),
                P("Wheat Flour 10kg", "Fine atta family pack", 1180, 30, 6, grocery, store, kg),
                P("Sugar 1kg", "White sugar", 220, 80, 15, grocery, store, kg),
                P("Rice Basmati 5kg", "Basmati rice", 1450, 35, 7, grocery, store, kg),
                P("Cooking Oil 1L", "Vegetable cooking oil", 520, 60, 12, grocery, store, litre),
                P("Cooking Oil 5L", "Vegetable cooking oil jerry", 2450, 25, 5, grocery, store, litre),
                P("Salt Iodized 800g", "Iodized table salt", 60, 100, 20, grocery, national, pack),
                P("National Red Chilli 200g", "Red chilli powder", 220, 70, 14, grocery, national, pack),
                P("National Turmeric 200g", "Turmeric powder", 180, 70, 14, grocery, national, pack),
                P("Shan Biryani Masala", "Biryani spice mix", 140, 90, 18, grocery, shan, pack),
                P("Shan Chicken Masala", "Chicken curry spice mix", 140, 90, 18, grocery, shan, pack),
                P("National Ketchup 500g", "Tomato ketchup", 280, 55, 10, grocery, national, pack),
                P("Mitchell's Jam Mixed Fruit 450g", "Mixed fruit jam", 320, 45, 8, grocery, mitchells, pack),

                // Breakfast
                P("Cornflakes 250g", "Breakfast cornflakes", 380, 50, 10, breakfast, nestle, pack),
                P("Oats 500g", "Rolled oats", 420, 45, 8, breakfast, nestle, pack),
                P("Honey 250g", "Natural honey jar", 480, 40, 8, breakfast, store, piece),
                P("Peanut Butter 340g", "Creamy peanut butter", 520, 35, 7, breakfast, store, piece),
                P("Chocolate Spread 350g", "Chocolate hazelnut spread", 650, 30, 6, breakfast, nestle, piece),
                P("Instant Noodles Pack", "Masala instant noodles", 60, 200, 40, breakfast, nestle, pack),
                P("Instant Noodles Family 5", "Family pack noodles", 280, 80, 15, breakfast, nestle, pack),

                // Household
                P("Surf Excel 1kg", "Laundry detergent powder", 420, 50, 10, household, surf, pack),
                P("Surf Excel 500g", "Laundry detergent small", 230, 70, 14, household, surf, pack),
                P("Dishwashing Liquid 500ml", "Dish wash liquid", 220, 60, 12, household, store, piece),
                P("Floor Cleaner 1L", "Multipurpose floor cleaner", 280, 45, 8, household, store, litre),
                P("Tissue Roll Pack 4", "Toilet tissue rolls", 320, 55, 10, household, store, pack),
                P("Kitchen Tissue Pack", "Kitchen paper towels", 180, 60, 12, household, store, pack),
                P("Garbage Bags Medium", "Trash bags roll", 150, 70, 14, household, store, pack),
                P("Match Box Pack 10", "Safety matches", 50, 120, 25, household, store, pack),

                // Personal care
                P("Lux Soap 115g", "Bathing soap", 110, 100, 20, personal, lux, piece),
                P("Lux Soap Pack 3", "Bathing soap multipack", 300, 60, 12, personal, lux, pack),
                P("Shampoo 180ml", "Daily care shampoo", 320, 50, 10, personal, nestle, piece),
                P("Toothpaste 100g", "Fluoride toothpaste", 220, 70, 14, personal, store, piece),
                P("Toothbrush Soft", "Soft bristle toothbrush", 90, 80, 15, personal, store, piece),
                P("Hand Wash 250ml", "Liquid hand wash", 180, 55, 10, personal, store, piece),
                P("Face Wash 100ml", "Gentle face wash", 280, 45, 8, personal, store, piece),
                P("Tissue Pocket Pack", "Facial tissue pocket", 40, 150, 30, personal, store, pack),
            };
        }

        private static SeedProduct P(
            string name,
            string description,
            decimal price,
            decimal stock,
            decimal alert,
            Category category,
            Brand brand,
            Unit unit)
        {
            return new SeedProduct
            {
                Name = name,
                Description = description,
                Price = price,
                Stock = stock,
                Alert = alert,
                CategoryId = category.Id,
                BrandId = brand.Id,
                UnitId = unit.Id
            };
        }

        private class SeedProduct
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public decimal Stock { get; set; }
            public decimal Alert { get; set; }
            public int CategoryId { get; set; }
            public int BrandId { get; set; }
            public int UnitId { get; set; }
        }
    }
}
