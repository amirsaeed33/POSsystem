using System.Collections.Generic;

namespace SmartPos.Orders
{
    public static class OnlineOrderHeroSlideDefaults
    {
        public const string DefaultsFolder = "/OnlineOrderHeroImages/defaults";

        public class SlideBlueprint
        {
            public int SortOrder { get; set; }
            public string Badge { get; set; }
            public string BadgeIcon { get; set; }
            public string TitleMain { get; set; }
            public string TitleHighlight { get; set; }
            public string TitleEnd { get; set; }
            public string Description { get; set; }
            public string OfferPill { get; set; }
            public string Rating { get; set; }
            public string PrimaryBtnText { get; set; }
            public string PrimaryTarget { get; set; }
            public string SecondaryBtnText { get; set; }
            public string SecondaryTarget { get; set; }
            public string ImageFileName { get; set; }
        }

        public static IReadOnlyList<SlideBlueprint> GetBlueprints()
        {
            return new List<SlideBlueprint>
            {
                new SlideBlueprint
                {
                    SortOrder = 1,
                    Badge = "FRESH FROM OVEN • 30-45 MIN DELIVERY",
                    BadgeIcon = "🔥",
                    TitleMain = "Craving Something",
                    TitleHighlight = "Hot, Cheesy",
                    TitleEnd = "& Delicious?",
                    Description = "Explore handcrafted pizzas loaded with 100% mozzarella, sizzling juicy burgers, crispy wings, and mouth-watering family deals. Cooked fresh on order!",
                    OfferPill = "FLAT 25% OFF ON LARGE PIZZAS",
                    Rating = "4.9★",
                    PrimaryBtnText = "Order Deals Now",
                    PrimaryTarget = "deals",
                    SecondaryBtnText = "Explore Full Menu",
                    SecondaryTarget = "menu",
                    ImageFileName = "pizza.jpg"
                },
                new SlideBlueprint
                {
                    SortOrder = 2,
                    Badge = "SMASHED & GRILLED TO PERFECTION",
                    BadgeIcon = "🍔",
                    TitleMain = "Bite Into Pure",
                    TitleHighlight = "Juicy & Crispy",
                    TitleEnd = "Gourmet Burgers",
                    Description = "Double smashed beef patties, crispy tender chicken fillets, secret spicy mayo, and melted golden cheddar packed inside freshly toasted brioche buns.",
                    OfferPill = "FREE CRISPY FRIES + DIP WITH COMBOS",
                    Rating = "4.8★",
                    PrimaryBtnText = "Grab Burger Deal",
                    PrimaryTarget = "deals",
                    SecondaryBtnText = "View All Burgers",
                    SecondaryTarget = "menu",
                    ImageFileName = "burger.jpg"
                },
                new SlideBlueprint
                {
                    SortOrder = 3,
                    Badge = "SMOKY CHARCOAL & AUTHENTIC TASTE",
                    BadgeIcon = "🍢",
                    TitleMain = "Sizzling Live",
                    TitleHighlight = "Smoky BBQ",
                    TitleEnd = "& Kebab Platters",
                    Description = "Tender chicken boti, melt-in-mouth seekh kebabs, and grilled reshmi tikka marinated in rich spices and served with piping-hot kulchas and mint raita.",
                    OfferPill = "FAMILY PLATTER SPECIAL SAVE PKR 800",
                    Rating = "4.9★",
                    PrimaryBtnText = "Taste The BBQ",
                    PrimaryTarget = "menu",
                    SecondaryBtnText = "View BBQ Platters",
                    SecondaryTarget = "menu",
                    ImageFileName = "bbq.jpg"
                },
                new SlideBlueprint
                {
                    SortOrder = 4,
                    Badge = "MEGA VALUE PACKS & CELEBRATIONS",
                    BadgeIcon = "🎉",
                    TitleMain = "Gather Round For",
                    TitleHighlight = "Mega Family",
                    TitleEnd = "Feast Combos",
                    Description = "The ultimate party solution! 2 Large Specialty Pizzas, 10 pcs Crispy Wings, Cheesy Garlic Bread, and a chilled 1.5L drink for the whole squad.",
                    OfferPill = "MEGA SAVINGS UP TO PKR 1,200",
                    Rating = "5.0★",
                    PrimaryBtnText = "Get Family Feast",
                    PrimaryTarget = "deals",
                    SecondaryBtnText = "See All Combos",
                    SecondaryTarget = "deals",
                    ImageFileName = "family.jpg"
                },
                new SlideBlueprint
                {
                    SortOrder = 5,
                    Badge = "PIPING HOT DELIVERY GUARANTEE",
                    BadgeIcon = "⚡",
                    TitleMain = "Delivered In",
                    TitleHighlight = "30-45 Mins",
                    TitleEnd = "Right To Your Door",
                    Description = "Experience lightning-fast express delivery with insulated heat bags. Live rider GPS tracking keeps you informed every step of the way from grill to table.",
                    OfferPill = "FREE EXPRESS DELIVERY TODAY",
                    Rating = "4.9★",
                    PrimaryBtnText = "Order Online Now",
                    PrimaryTarget = "menu",
                    SecondaryBtnText = "Explore Full Menu",
                    SecondaryTarget = "menu",
                    ImageFileName = "delivery.jpg"
                }
            };
        }

        public static string ToImagePath(string fileName)
        {
            return $"{DefaultsFolder}/{fileName}";
        }

        public static OnlineOrderHeroSlide CreateEntity(int branchId, int? tenantId, SlideBlueprint bp)
        {
            var path = ToImagePath(bp.ImageFileName);
            return new OnlineOrderHeroSlide
            {
                TenantId = tenantId,
                BranchId = branchId,
                SortOrder = bp.SortOrder,
                IsActive = true,
                Badge = bp.Badge,
                BadgeIcon = bp.BadgeIcon,
                TitleMain = bp.TitleMain,
                TitleHighlight = bp.TitleHighlight,
                TitleEnd = bp.TitleEnd,
                Description = bp.Description,
                OfferPill = bp.OfferPill,
                Rating = bp.Rating,
                PrimaryBtnText = bp.PrimaryBtnText,
                PrimaryTarget = bp.PrimaryTarget,
                SecondaryBtnText = bp.SecondaryBtnText,
                SecondaryTarget = bp.SecondaryTarget,
                ImagePath = path,
                BgImagePath = path
            };
        }
    }
}
