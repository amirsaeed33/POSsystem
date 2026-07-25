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
    /// Seeds demo bike spare-parts master data (categories, brands, units, ~100 products).
    /// </summary>
    public class BikeSparePartsDemoDataCreator
    {
        private readonly SmartPosDbContext _context;
        private readonly int? _tenantId;

        public BikeSparePartsDemoDataCreator(SmartPosDbContext context, int? tenantId)
        {
            _context = context;
            _tenantId = tenantId;
        }

        public void Create()
        {
            if (_context.Products.IgnoreQueryFilters()
                .Any(x => x.TenantId == _tenantId && !x.IsDeleted && x.Barcode != null && x.Barcode.StartsWith("BIKE-")))
            {
                return;
            }

            var unitPiece = EnsureUnit("Piece", "Single piece");
            var unitPair = EnsureUnit("Pair", "Pair of items");
            var unitSet = EnsureUnit("Set", "Complete set");
            var unitLitre = EnsureUnit("Litre", "Liquid volume");
            var unitPack = EnsureUnit("Pack", "Pack / kit");

            var catEngine = EnsureCategory("Engine Parts", "Engine and related spares");
            var catTransmission = EnsureCategory("Transmission", "Clutch, chain, sprocket, gearbox");
            var catBrakes = EnsureCategory("Brakes", "Brake pads, discs, cables");
            var catSuspension = EnsureCategory("Suspension", "Forks, shocks, springs");
            var catElectrical = EnsureCategory("Electrical", "Battery, CDI, wiring, switches");
            var catBody = EnsureCategory("Body & Frame", "Panels, mirrors, stands");
            var catTyres = EnsureCategory("Tyres & Wheels", "Tyres, tubes, rims");
            var catFilters = EnsureCategory("Filters & Fluids", "Oil, air filter, coolant");
            var catLighting = EnsureCategory("Lighting", "Headlight, indicators, bulbs");
            var catAccessories = EnsureCategory("Accessories", "Grips, covers, locks");

            var brandHonda = EnsureBrand("Honda", "Honda genuine / compatible");
            var brandYamaha = EnsureBrand("Yamaha", "Yamaha genuine / compatible");
            var brandSuzuki = EnsureBrand("Suzuki", "Suzuki genuine / compatible");
            var brandHero = EnsureBrand("Hero", "Hero Motocorp");
            var brandBajaj = EnsureBrand("Bajaj", "Bajaj Auto");
            var brandTvs = EnsureBrand("TVS", "TVS Motor");
            var brandBosch = EnsureBrand("Bosch", "Bosch auto parts");
            var brandNgk = EnsureBrand("NGK", "NGK spark plugs");
            var brandMotul = EnsureBrand("Motul", "Motul lubricants");
            var brandCastrol = EnsureBrand("Castrol", "Castrol lubricants");
            var brandMrf = EnsureBrand("MRF", "MRF tyres");
            var brandCeat = EnsureBrand("CEAT", "CEAT tyres");
            var brandExide = EnsureBrand("Exide", "Exide batteries");
            var brandLucas = EnsureBrand("Lucas", "Lucas TVS electrical");
            var brandOem = EnsureBrand("OEM Compatible", "Aftermarket / OEM compatible");

            _context.SaveChanges();

            var catalog = BuildCatalog(
                unitPiece, unitPair, unitSet, unitLitre, unitPack,
                catEngine, catTransmission, catBrakes, catSuspension, catElectrical,
                catBody, catTyres, catFilters, catLighting, catAccessories,
                brandHonda, brandYamaha, brandSuzuki, brandHero, brandBajaj, brandTvs,
                brandBosch, brandNgk, brandMotul, brandCastrol, brandMrf, brandCeat,
                brandExide, brandLucas, brandOem);

            var index = 1;
            foreach (var item in catalog.Take(100))
            {
                _context.Products.Add(new Product
                {
                    TenantId = _tenantId,
                    Name = item.Name,
                    Description = item.Description,
                    Barcode = "BIKE-" + index.ToString("D5"),
                    Price = item.Price,
                    WholesalePrice = Math.Round(item.Price * 0.85m, 2),
                    CostPrice = Math.Round(item.Price * 0.7m, 2),
                    StockQuantity = item.Stock,
                    AlertQuantityLimit = item.Alert,
                    CategoryId = item.CategoryId,
                    BrandId = item.BrandId,
                    UnitId = item.UnitId
                });
                index++;
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
            Unit piece, Unit pair, Unit set, Unit litre, Unit pack,
            Category engine, Category transmission, Category brakes, Category suspension, Category electrical,
            Category body, Category tyres, Category filters, Category lighting, Category accessories,
            Brand honda, Brand yamaha, Brand suzuki, Brand hero, Brand bajaj, Brand tvs,
            Brand bosch, Brand ngk, Brand motul, Brand castrol, Brand mrf, Brand ceat,
            Brand exide, Brand lucas, Brand oem)
        {
            return new List<SeedProduct>
            {
                P("NGK CR7HIX Spark Plug", "Iridium spark plug for 100-150cc", 450, 80, 15, engine, ngk, piece),
                P("NGK C7HSA Spark Plug", "Standard spark plug", 220, 120, 20, engine, ngk, piece),
                P("Bosch Spark Plug WR7DC", "Copper core spark plug", 380, 60, 12, engine, bosch, piece),
                P("Honda Piston Kit 100cc", "Piston, rings, pin kit", 1850, 25, 5, engine, honda, set),
                P("Yamaha Piston Kit 125cc", "Piston assembly 125cc", 2100, 18, 4, engine, yamaha, set),
                P("Suzuki Cylinder Kit", "Cylinder block kit", 4200, 10, 3, engine, suzuki, set),
                P("Hero Engine Oil Seal Kit", "Crank and valve seals", 650, 40, 8, engine, hero, set),
                P("Bajaj Timing Chain", "Cam chain for 150cc", 980, 30, 6, engine, bajaj, piece),
                P("TVS Camshaft Bearing", "Camshaft support bearing", 320, 45, 10, engine, tvs, piece),
                P("OEM Valve Set Intake/Exhaust", "Pair of valves", 750, 35, 8, engine, oem, pair),
                P("Honda Carburetor Rebuild Kit", "Gaskets, jets, needle", 890, 28, 6, engine, honda, set),
                P("Yamaha Air Mixture Screw", "Carb idle mixture screw", 120, 90, 15, engine, yamaha, piece),
                P("Suzuki Head Gasket", "Cylinder head gasket", 410, 50, 10, engine, suzuki, piece),
                P("Hero Base Gasket", "Cylinder base gasket", 180, 70, 12, engine, hero, piece),
                P("Bajaj Exhaust Packing", "Exhaust flange packing", 90, 100, 20, engine, bajaj, piece),

                P("Honda Clutch Plate Set", "Friction plates set", 1650, 22, 5, transmission, honda, set),
                P("Yamaha Clutch Spring Set", "Clutch springs (4 pcs)", 480, 40, 8, transmission, yamaha, set),
                P("Suzuki Drive Chain 428H", "Heavy duty 428 chain", 1950, 20, 4, transmission, suzuki, piece),
                P("Hero Drive Chain 420", "Standard 420 chain", 1250, 30, 6, transmission, hero, piece),
                P("Bajaj Front Sprocket 14T", "Countershaft sprocket", 520, 35, 7, transmission, bajaj, piece),
                P("TVS Rear Sprocket 42T", "Rear wheel sprocket", 780, 28, 6, transmission, tvs, piece),
                P("OEM Chain & Sprocket Kit", "Chain with front/rear sprocket", 3200, 15, 4, transmission, oem, set),
                P("Honda Gear Shift Lever", "Gear pedal assembly", 650, 25, 5, transmission, honda, piece),
                P("Yamaha Clutch Cable", "Clutch control cable", 280, 55, 10, transmission, yamaha, piece),
                P("Suzuki Accelerator Cable", "Throttle cable", 260, 60, 10, transmission, suzuki, piece),
                P("Hero Kick Starter Lever", "Kick pedal", 540, 30, 6, transmission, hero, piece),
                P("Bajaj Gear Box Oil Seal", "Output shaft seal", 150, 80, 15, transmission, bajaj, piece),

                P("Honda Brake Shoe Set Front", "Drum brake shoes", 720, 40, 8, brakes, honda, pair),
                P("Yamaha Brake Shoe Set Rear", "Rear drum shoes", 680, 42, 8, brakes, yamaha, pair),
                P("Suzuki Disc Brake Pad Front", "Front disc pads", 950, 35, 7, brakes, suzuki, pair),
                P("Hero Disc Brake Pad Rear", "Rear disc pads", 880, 30, 6, brakes, hero, pair),
                P("Bajaj Brake Disc Rotor", "Front disc rotor 240mm", 2450, 12, 3, brakes, bajaj, piece),
                P("TVS Brake Cable Front", "Front brake cable", 240, 70, 12, brakes, tvs, piece),
                P("OEM Brake Cable Rear", "Rear brake cable", 230, 75, 12, brakes, oem, piece),
                P("Honda Master Cylinder Kit", "Front master cylinder seals", 560, 20, 5, brakes, honda, set),
                P("Yamaha Brake Fluid DOT 4", "500ml brake fluid", 380, 45, 10, brakes, yamaha, litre),
                P("Bosch Brake Pad Set", "Universal disc pad set", 1100, 25, 5, brakes, bosch, pair),

                P("Honda Front Fork Oil Seal", "Fork oil seal pair", 420, 40, 8, suspension, honda, pair),
                P("Yamaha Rear Shock Absorber", "Rear mono shock", 3850, 10, 3, suspension, yamaha, piece),
                P("Suzuki Front Fork Spring", "Fork spring pair", 1450, 15, 4, suspension, suzuki, pair),
                P("Hero Rear Shock Pair", "Twin rear shocks", 4200, 12, 3, suspension, hero, pair),
                P("Bajaj Swing Arm Bush Kit", "Swingarm bushes", 380, 30, 6, suspension, bajaj, set),
                P("TVS Steering Cone Set", "Steering bearing set", 650, 22, 5, suspension, tvs, set),
                P("OEM Fork Oil 10W", "Front fork oil 1L", 520, 35, 8, suspension, oem, litre),
                P("Honda Handlebar Clamp", "Riser clamp set", 290, 40, 8, suspension, honda, set),

                P("Exide 12V 5Ah Battery", "MF battery for bikes", 2850, 20, 5, electrical, exide, piece),
                P("Exide 12V 7Ah Battery", "Higher capacity MF battery", 3450, 15, 4, electrical, exide, piece),
                P("Lucas CDI Unit 100cc", "Capacitor discharge ignition", 1250, 25, 5, electrical, lucas, piece),
                P("Honda Starter Relay", "Self-start relay", 480, 30, 6, electrical, honda, piece),
                P("Yamaha Regulator Rectifier", "RR unit 12V", 980, 22, 5, electrical, yamaha, piece),
                P("Suzuki Starter Motor", "Self starter motor", 4200, 8, 2, electrical, suzuki, piece),
                P("Hero Ignition Coil", "HT coil", 650, 35, 7, electrical, hero, piece),
                P("Bajaj Horn 12V", "Electric horn", 420, 50, 10, electrical, bajaj, piece),
                P("TVS Switch Assembly LH", "Left handle switch", 780, 28, 6, electrical, tvs, piece),
                P("OEM Switch Assembly RH", "Right handle switch", 820, 26, 6, electrical, oem, piece),
                P("Lucas Wiring Harness 100cc", "Main wiring harness", 1850, 12, 3, electrical, lucas, piece),
                P("Bosch Flasher Relay", "Indicator flasher", 320, 45, 8, electrical, bosch, piece),

                P("Honda Side Mirror Pair", "Rear view mirrors", 650, 40, 8, body, honda, pair),
                P("Yamaha Front Fender", "Front mudguard", 980, 18, 4, body, yamaha, piece),
                P("Suzuki Rear Fender", "Rear mudguard", 860, 20, 4, body, suzuki, piece),
                P("Hero Fuel Tank Cap", "Lockable tank cap", 420, 35, 7, body, hero, piece),
                P("Bajaj Side Stand", "Main side stand", 540, 30, 6, body, bajaj, piece),
                P("TVS Centre Stand", "Centre stand assembly", 1250, 15, 4, body, tvs, piece),
                P("OEM Seat Cover", "Two-wheeler seat cover", 780, 45, 10, body, oem, piece),
                P("Honda Leg Guard", "Crash guard / leg guard", 1450, 20, 5, body, honda, set),
                P("Yamaha Number Plate Light", "Tail number plate lamp", 280, 40, 8, body, yamaha, piece),
                P("Suzuki Visor", "Headlight visor / cowl", 650, 22, 5, body, suzuki, piece),

                P("MRF Zapper 80/100-18", "Front tyre tube type", 2450, 25, 5, tyres, mrf, piece),
                P("MRF Nylogrip 100/90-18", "Rear tyre", 2850, 22, 5, tyres, mrf, piece),
                P("CEAT Secura Zip 2.75-18", "Front tyre", 2350, 20, 4, tyres, ceat, piece),
                P("CEAT Milaze 3.00-18", "Rear tyre", 2750, 18, 4, tyres, ceat, piece),
                P("OEM Tube 2.75-18", "Inner tube front", 480, 50, 10, tyres, oem, piece),
                P("OEM Tube 3.00-18", "Inner tube rear", 520, 48, 10, tyres, oem, piece),
                P("Honda Rim Tape", "Rim strip", 80, 100, 20, tyres, honda, piece),
                P("Yamaha Wheel Bearing Kit", "Front wheel bearings", 350, 40, 8, tyres, yamaha, set),
                P("Suzuki Spoke Set 18\"", "Spoke and nipple set", 980, 15, 4, tyres, suzuki, set),
                P("Hero Alloy Wheel Front", "Cast alloy front wheel", 5200, 6, 2, tyres, hero, piece),

                P("Motul 7100 10W40 1L", "Fully synthetic engine oil", 1450, 40, 8, filters, motul, litre),
                P("Motul 3000 20W50 1L", "Mineral engine oil", 780, 55, 10, filters, motul, litre),
                P("Castrol Activ 20W40 1L", "4T engine oil", 720, 60, 12, filters, castrol, litre),
                P("Castrol Power1 10W30 1L", "Semi-synthetic oil", 980, 35, 8, filters, castrol, litre),
                P("Honda Air Filter Element", "Foam/paper air filter", 320, 50, 10, filters, honda, piece),
                P("Yamaha Oil Filter", "Cartridge oil filter", 280, 45, 9, filters, yamaha, piece),
                P("Suzuki Fuel Filter", "Inline fuel filter", 150, 70, 15, filters, suzuki, piece),
                P("Hero Coolant 1L", "Radiator coolant", 420, 30, 6, filters, hero, litre),
                P("Bajaj Chain Lube Spray", "Chain lubricant 250ml", 380, 40, 8, filters, bajaj, piece),
                P("OEM Grease Pack 100g", "Multipurpose grease", 120, 80, 15, filters, oem, pack),

                P("Honda Headlight Assembly", "Complete headlight", 1850, 15, 4, lighting, honda, piece),
                P("Yamaha LED Headlight Bulb H4", "LED conversion bulb", 980, 30, 6, lighting, yamaha, piece),
                P("Suzuki Indicator Pair Front", "Front turn signals", 520, 35, 7, lighting, suzuki, pair),
                P("Hero Indicator Pair Rear", "Rear turn signals", 480, 38, 7, lighting, hero, pair),
                P("Bajaj Tail Light Assembly", "Stop / tail lamp", 750, 25, 5, lighting, bajaj, piece),
                P("TVS Halogen Bulb 35/35W", "Headlight bulb", 180, 90, 15, lighting, tvs, piece),
                P("OEM LED Strip Flexible", "Accent LED strip", 350, 40, 8, lighting, oem, piece),
                P("Lucas Pilot Lamp Bulb", "Dashboard bulb pack", 90, 100, 20, lighting, lucas, pack),
                P("Honda Fog Lamp Kit", "Auxiliary fog lamps", 1650, 12, 3, lighting, honda, set),
                P("Bosch Indicator Bulb 12V", "Amber indicator bulbs", 60, 150, 25, lighting, bosch, pack),

                P("OEM Handle Grip Pair", "Rubber grips", 280, 60, 12, accessories, oem, pair),
                P("Honda Disc Lock", "Security disc lock", 850, 25, 5, accessories, honda, piece),
                P("Yamaha Bike Cover", "Waterproof body cover", 980, 30, 6, accessories, yamaha, piece),
                P("Suzuki Mobile Holder", "Handlebar phone mount", 450, 40, 8, accessories, suzuki, piece),
                P("Hero Tool Kit Bag", "Basic tool pouch", 320, 35, 7, accessories, hero, set),
                P("Bajaj Helmet Lock Cable", "Cable lock", 180, 50, 10, accessories, bajaj, piece),
                P("TVS Footpeg Pair", "Rider footpegs", 650, 28, 6, accessories, tvs, pair),
                P("OEM Mud Flap Pair", "Rear mud flaps", 220, 45, 9, accessories, oem, pair),
                P("Honda Tank Pad", "Fuel tank protection pad", 380, 30, 6, accessories, honda, piece),
                P("Yamaha Lever Guard Pair", "Clutch/brake lever guards", 720, 20, 5, accessories, yamaha, pair),
                P("Suzuki Chain Cover", "Chain guard cover", 540, 22, 5, accessories, suzuki, piece),
                P("Exide Battery Charger 12V", "Smart trickle charger", 1850, 10, 3, accessories, exide, piece),
                P("Motul Cleaner Spray", "Bike cleaner 400ml", 420, 35, 7, accessories, motul, piece),
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
