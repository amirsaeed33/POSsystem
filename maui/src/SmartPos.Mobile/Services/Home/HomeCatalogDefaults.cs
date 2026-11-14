using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services.Home;

/// <summary>Default/fallback catalog content for home when APIs return empty.</summary>
public static class HomeCatalogDefaults
{
    public static List<CustomerFeedbackDto> CreateDefaultFeedbacks(string branchName) =>
    [
        new CustomerFeedbackDto
        {
            OrderId = 101,
            CustomerName = "Hamza Ali",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-1),
            BranchName = branchName,
            Comment = "Food was piping hot and delivered in just 25 minutes! Best burgers in town.",
            ItemsSummary = "Zinger Supreme Meal • Loaded Fries"
        },
        new CustomerFeedbackDto
        {
            OrderId = 102,
            CustomerName = "Ayesha Khan",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-2),
            BranchName = branchName,
            Comment = "Pizza cheese pull was incredible! Highly recommend the family combo deal.",
            ItemsSummary = "Large Ranch Pizza • Garlic Bread"
        },
        new CustomerFeedbackDto
        {
            OrderId = 103,
            CustomerName = "Usman Tariq",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-3),
            BranchName = branchName,
            Comment = "Everything was fresh, properly packed, and delicious. Great experience!",
            ItemsSummary = "Double Patty Beef Burger • Mint Mojito"
        },
        new CustomerFeedbackDto
        {
            OrderId = 104,
            CustomerName = "Zainab Fatima",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-4),
            BranchName = branchName,
            Comment = "Crispy wings with honey mustard sauce are a must try. Fast delivery!",
            ItemsSummary = "10 Pcs Honey Glazed Wings • Drink"
        }
    ];

    public static List<HeroSlideDto> CreateDefaultHeroSlides() =>
    [
        new HeroSlideDto
        {
            Id = 1,
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
            SecondaryBtnText = "Explore Full Menu",
            BgImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 2,
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
            SecondaryBtnText = "View All Burgers",
            BgImageUrl = "https://images.unsplash.com/photo-1572802419224-296b0aeee0d9?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 3,
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
            SecondaryBtnText = "View BBQ Platters",
            BgImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 4,
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
            SecondaryBtnText = "See All Combos",
            BgImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 5,
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
            SecondaryBtnText = "Track Active Order",
            BgImageUrl = "https://images.unsplash.com/photo-1617347454431-f49d7ff5c3b1?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1617347454431-f49d7ff5c3b1?auto=format&fit=crop&w=800&h=600&q=80"
        }
    ];

    public static List<HomeCategoryDisplayDto> CreateDefaultDisplayCategories()
    {
        var defaults = new (string Name, string Emoji, string Image)[]
        {
            ("Promo", "🏷️", "promo_icon.png"),
            ("Burger", "🍔", "burger.png"),
            ("Pizza", "🍕", "pizza.png"),
            ("Drinks", "🥤", "drinks.png"),
            ("Chicken", "🍗", "meat.png"),
            ("Fries", "🍟", "taco.png"),
            ("Deals", "🔥", "sushi.png"),
            ("More", "⠿", "more_dots.png")
        };

        var list = new List<HomeCategoryDisplayDto>();
        for (var i = 0; i < defaults.Length; i++)
        {
            var isMore = string.Equals(defaults[i].Name, "More", StringComparison.OrdinalIgnoreCase);
            list.Add(new HomeCategoryDisplayDto
            {
                Id = i + 1,
                Name = defaults[i].Name,
                IconOrEmoji = defaults[i].Emoji,
                ImageUrl = defaults[i].Image,
                IsMoreButton = isMore,
                IsSelected = false,
                GridRow = i / 4,
                GridColumn = i % 4
            });
        }
        return list;
    }

    public static string ResolveCategoryEmoji(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "🍽️";
        }

        var lower = name.ToLowerInvariant().Trim();

        // Burgers, Zingers & Sandwiches
        if (lower.Contains("burg") || lower.Contains("zinger") || lower.Contains("patty") || lower.Contains("bun")) return "🍔";
        if (lower.Contains("sandw") || lower.Contains("sub") || lower.Contains("panini") || lower.Contains("toast")) return "🥪";

        // Pan Fry, Tawa, Skillet Dishes (Checked before general BBQ so BBQ Pan Fry gets a skillet)
        if (lower.Contains("pan") || lower.Contains("fry") || lower.Contains("tawa") || lower.Contains("skillet")) return "🍳";

        // Platters & Combos
        if (lower.Contains("platter") || lower.Contains("thali") || lower.Contains("bento")) return "🍱";

        // Pizza & Pastas
        if (lower.Contains("pizza") || lower.Contains("slice") || lower.Contains("calzone")) return "🍕";
        if (lower.Contains("pasta") || lower.Contains("noodle") || lower.Contains("spaghetti") || lower.Contains("lasagna") || lower.Contains("macaroni") || lower.Contains("chowmein")) return "🍝";

        // Starters, Appetizers & Finger Foods
        if (lower.Contains("start") || lower.Contains("appetiz") || lower.Contains("finger") || lower.Contains("nugget") || lower.Contains("bite") || lower.Contains("snack") || lower.Contains("samosa") || lower.Contains("roll")) return "🍢";
        if (lower.Contains("fries") || lower.Contains("potato") || lower.Contains("chips") || lower.Contains("wedges")) return "🍟";

        // Main Course, Curries, Karahi, Handi & Boneless
        if (lower.Contains("main") || lower.Contains("course") || lower.Contains("handi") || lower.Contains("karahi") || lower.Contains("curry") || lower.Contains("gravy") || lower.Contains("salan") || lower.Contains("nihari") || lower.Contains("korma") || lower.Contains("boneless")) return "🍲";
        if (lower.Contains("rice") || lower.Contains("biryani") || lower.Contains("pulao") || lower.Contains("fried rice")) return "🍚";

        // Chicken, BBQ & Meat
        if (lower.Contains("chicken") || lower.Contains("broast") || lower.Contains("wings") || lower.Contains("drumstick") || lower.Contains("crispy")) return "🍗";
        if (lower.Contains("bbq") || lower.Contains("barbecue") || lower.Contains("tikka") || lower.Contains("kabab") || lower.Contains("kebab") || lower.Contains("boti")) return "🔥";
        if (lower.Contains("meat") || lower.Contains("beef") || lower.Contains("mutton") || lower.Contains("steak") || lower.Contains("lamb")) return "🥩";

        // Seafood
        if (lower.Contains("fish") || lower.Contains("prawn") || lower.Contains("shrimp") || lower.Contains("sea") || lower.Contains("sushi")) return "🍣";

        // Drinks & Beverages
        if (lower.Contains("drink") || lower.Contains("beverage") || lower.Contains("juice") || lower.Contains("cold") || lower.Contains("soda") || lower.Contains("cola") || lower.Contains("pepsi") || lower.Contains("shake") || lower.Contains("smoothie") || lower.Contains("mojito")) return "🥤";
        if (lower.Contains("tea") || lower.Contains("chai") || lower.Contains("coffee") || lower.Contains("cappuccino") || lower.Contains("latte") || lower.Contains("green tea")) return "☕";

        // Desserts & Ice Cream
        if (lower.Contains("dessert") || lower.Contains("ice") || lower.Contains("cream") || lower.Contains("sweet") || lower.Contains("cake") || lower.Contains("pastry") || lower.Contains("kheer") || lower.Contains("halwa") || lower.Contains("brownie") || lower.Contains("waffle") || lower.Contains("donut")) return "🍦";

        // Wraps & Mexican
        if (lower.Contains("taco") || lower.Contains("shawarma") || lower.Contains("wrap") || lower.Contains("burrito") || lower.Contains("fajita")) return "🌮";

        // Bread, Roti & Naan
        if (lower.Contains("naan") || lower.Contains("roti") || lower.Contains("bread") || lower.Contains("paratha")) return "🫓";

        // Deals & Combos
        if (lower.Contains("deal") || lower.Contains("combo") || lower.Contains("special") || lower.Contains("promo") || lower.Contains("offer") || lower.Contains("discount") || lower.Contains("family")) return "🔥";

        // Permanent Items / Specials / Favorites
        if (lower.Contains("perman") || lower.Contains("fav") || lower.Contains("pop") || lower.Contains("chef") || lower.Contains("star") || lower.Contains("signat")) return "⭐";

        // Soup & Salad
        if (lower.Contains("soup")) return "🥣";
        if (lower.Contains("salad") || lower.Contains("diet") || lower.Contains("green") || lower.Contains("veg")) return "🥗";

        return "🍽️";
    }

    public static List<OnlineProductDto> CreateDefaultComboDeals()
    {
        return
        [
            new OnlineProductDto
            {
                Id = -101,
                DealId = 101,
                Name = "🔥 Mega Crunch Feast Combo",
                Description = "Crispy golden chicken fillet zinger burger, seasoned cheese fries, and a chilled can drink.",
                CategoryName = "Deals",
                Price = 890,
                OriginalPrice = 1060,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍔 1x Zinger Burger • 🍟 1x Regular Fries • 🥤 1x Soft Drink"
            },
            new OnlineProductDto
            {
                Id = -102,
                DealId = 102,
                Name = "👑 Royal Pizza & Wings Deal",
                Description = "Medium cheesy specialty pizza, 10 pcs spicy hot wings, and a large cold drink for pizza lovers.",
                CategoryName = "Deals",
                Price = 1490,
                OriginalPrice = 1720,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍕 1x Medium Pizza • 🍗 10x Hot Wings • 🥤 1x Soft Drink"
            },
            new OnlineProductDto
            {
                Id = -103,
                DealId = 103,
                Name = "🍔 Duo Burger Buddy Pack",
                Description = "Two handcrafted crispy burgers, 2 golden regular fries, and 2 chilled soft drinks for you and a friend.",
                CategoryName = "Deals",
                Price = 1290,
                OriginalPrice = 1520,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍔 2x Burgers • 🍟 2x Fries • 🥤 2x Drinks"
            },
            new OnlineProductDto
            {
                Id = -104,
                DealId = 104,
                Name = "🍢 Smoky BBQ Platter Feast",
                Description = "Tender grilled chicken malai boti, authentic seekh kebabs, 2 hot roghni naans, and a chilled drink.",
                CategoryName = "Deals",
                Price = 1390,
                OriginalPrice = 1550,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍢 1x Malai Boti • 🍢 1x Seekh Kabab • 🫓 2x Naan • 🥤 1x Drink"
            }
        ];
    }


    // =========================================================================

    public static List<ProductModifierGroupDto> CreateFallbackModifiersForName(string productName)
    {
        var lower = (productName ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("burger") || lower.Contains("zinger"))
        {
            return
            [
                new ProductModifierGroupDto
                {
                    Id = 101,
                    Name = "Spice Level",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1001, Name = "Spicy Crunch", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1002, Name = "Mild Normal" }
                    ]
                }
            ];
        }

        if (lower.Contains("drink") || lower.Contains("beverage") || lower.Contains("soft"))
        {
            return
            [
                new ProductModifierGroupDto
                {
                    Id = 104,
                    Name = "Drink Flavour",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1008, Name = "Pepsi", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1009, Name = "7Up / Sprite" },
                        new ProductModifierOptionDto { Id = 1010, Name = "Mirinda" }
                    ]
                }
            ];
        }

        return new List<ProductModifierGroupDto>();
    }

    public static bool LooksLikePizza(OnlineProductDto product)
    {
        var n = (product.Name ?? string.Empty).ToLowerInvariant();
        var c = (product.CategoryName ?? string.Empty).ToLowerInvariant();
        return n.Contains("pizza") || c.Contains("pizza");
    }
}
