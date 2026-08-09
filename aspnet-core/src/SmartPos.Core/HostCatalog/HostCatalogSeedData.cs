using System.Collections.Generic;

namespace SmartPos.HostCatalog
{
    public class HostCatalogSeedItem
    {
        public string Type { get; set; }
        public string CompanyTypeName { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

    public static class HostCatalogSeedData
    {
        public static IReadOnlyList<HostCatalogSeedItem> Items { get; } = Build();

        private static List<HostCatalogSeedItem> Build()
        {
            var items = new List<HostCatalogSeedItem>();

            void CompanyType(string name) =>
                items.Add(new HostCatalogSeedItem { Type = HostCatalogItemTypes.CompanyType, Name = name });

            void Child(string companyType, string type, string name, string symbol = null) =>
                items.Add(new HostCatalogSeedItem
                {
                    Type = type,
                    CompanyTypeName = companyType,
                    Name = name,
                    Symbol = symbol
                });

            CompanyType("Mobile Shop");
            CompanyType("Bakery");
            CompanyType("Spare Parts");
            CompanyType("Grocery");
            CompanyType("Restaurant");

            // Mobile Shop
            Child("Mobile Shop", HostCatalogItemTypes.Category, "Mobiles");
            Child("Mobile Shop", HostCatalogItemTypes.Category, "Accessories");
            Child("Mobile Shop", HostCatalogItemTypes.Category, "Chargers");
            Child("Mobile Shop", HostCatalogItemTypes.Category, "Cables");
            Child("Mobile Shop", HostCatalogItemTypes.Category, "Power Banks");
            Child("Mobile Shop", HostCatalogItemTypes.Unit, "Piece", "pc");
            Child("Mobile Shop", HostCatalogItemTypes.Unit, "Box", "box");
            Child("Mobile Shop", HostCatalogItemTypes.Brand, "Samsung");
            Child("Mobile Shop", HostCatalogItemTypes.Brand, "Apple");
            Child("Mobile Shop", HostCatalogItemTypes.Brand, "Oppo");
            Child("Mobile Shop", HostCatalogItemTypes.Brand, "Vivo");

            // Bakery
            Child("Bakery", HostCatalogItemTypes.Category, "Cakes");
            Child("Bakery", HostCatalogItemTypes.Category, "Biscuits");
            Child("Bakery", HostCatalogItemTypes.Category, "Bread");
            Child("Bakery", HostCatalogItemTypes.Category, "Sweets");
            Child("Bakery", HostCatalogItemTypes.Unit, "Piece", "pc");
            Child("Bakery", HostCatalogItemTypes.Unit, "Dozen", "dz");
            Child("Bakery", HostCatalogItemTypes.Unit, "Kg", "kg");
            Child("Bakery", HostCatalogItemTypes.Brand, "Local Bake");
            Child("Bakery", HostCatalogItemTypes.Brand, "Sweet House");

            // Spare Parts
            Child("Spare Parts", HostCatalogItemTypes.Category, "Engine Parts");
            Child("Spare Parts", HostCatalogItemTypes.Category, "Body Parts");
            Child("Spare Parts", HostCatalogItemTypes.Category, "Electrical");
            Child("Spare Parts", HostCatalogItemTypes.Unit, "Piece", "pc");
            Child("Spare Parts", HostCatalogItemTypes.Unit, "Set", "set");
            Child("Spare Parts", HostCatalogItemTypes.Brand, "Bosch");
            Child("Spare Parts", HostCatalogItemTypes.Brand, "Denso");

            // Grocery
            Child("Grocery", HostCatalogItemTypes.Category, "Dairy");
            Child("Grocery", HostCatalogItemTypes.Category, "Beverages");
            Child("Grocery", HostCatalogItemTypes.Category, "Snacks");
            Child("Grocery", HostCatalogItemTypes.Unit, "Piece", "pc");
            Child("Grocery", HostCatalogItemTypes.Unit, "Kg", "kg");
            Child("Grocery", HostCatalogItemTypes.Unit, "Liter", "L");
            Child("Grocery", HostCatalogItemTypes.Brand, "Nestle");
            Child("Grocery", HostCatalogItemTypes.Brand, "Unilever");

            // Restaurant
            Child("Restaurant", HostCatalogItemTypes.Category, "Starters");
            Child("Restaurant", HostCatalogItemTypes.Category, "Main Course");
            Child("Restaurant", HostCatalogItemTypes.Category, "Drinks");
            Child("Restaurant", HostCatalogItemTypes.Category, "Desserts");
            Child("Restaurant", HostCatalogItemTypes.Unit, "Portion", "ptn");
            Child("Restaurant", HostCatalogItemTypes.Unit, "Glass", "gl");
            Child("Restaurant", HostCatalogItemTypes.Brand, "House Special");

            return items;
        }
    }
}