namespace SmartPos.HostCatalog
{
    public static class HostCatalogItemTypes
    {
        public const string CompanyType = "CompanyType";
        public const string Category = "Category";
        public const string Unit = "Unit";
        public const string Brand = "Brand";

        public static readonly string[] All =
        {
            CompanyType,
            Category,
            Unit,
            Brand
        };

        public static bool IsValid(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            foreach (var item in All)
            {
                if (string.Equals(item, type.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsCompanyType(string type)
        {
            return string.Equals(type, CompanyType, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsChildType(string type)
        {
            return string.Equals(type, Category, System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, Unit, System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(type, Brand, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
