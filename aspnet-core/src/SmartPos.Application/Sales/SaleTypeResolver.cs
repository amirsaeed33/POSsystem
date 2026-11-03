using System;

namespace SmartPos.Sales
{
    public static class SaleTypeResolver
    {
        public const string Takeaway = "Takeaway";
        public const string DineIn = "Dine-In";
        public const string OnlineOrder = "Online Order";

        public static string Resolve(string notes, decimal deliveryCharges = 0, string deliveryAreaName = null)
        {
            if (!string.IsNullOrWhiteSpace(notes))
            {
                if (notes.IndexOf("[Online Order", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Online Order", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Online Delivery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("ORD-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("From order ORD-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Order ORD-", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return OnlineOrder;
                }

                if (notes.IndexOf("[Dine-In", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Dine-In", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Dine In", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("[Car Service", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Car Service", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return DineIn;
                }

                if (notes.IndexOf("[Takeaway", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    notes.IndexOf("Takeaway", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return Takeaway;
                }
            }

            if (deliveryCharges > 0 || !string.IsNullOrWhiteSpace(deliveryAreaName))
            {
                return OnlineOrder;
            }

            return Takeaway;
        }
    }
}
