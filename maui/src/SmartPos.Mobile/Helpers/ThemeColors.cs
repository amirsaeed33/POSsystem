namespace SmartPos.Mobile.Helpers;

/// <summary>Resolve theme colors from Application resources with safe fallbacks.</summary>
public static class ThemeColors
{
    public static Color Get(string key, string fallbackHex)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
        {
            return color;
        }

        return Color.FromArgb(fallbackHex);
    }

    public static Color Primary => Get("Primary", "#C84B11");
    public static Color PrimaryDark => Get("PrimaryDark", "#8F3006");
    public static Color PrimaryLight => Get("PrimaryLight", "#FDF1EB");
    public static Color PageBackground => Get("PageBackground", "#FAF7F4");
    public static Color CardBackground => Get("CardBackground", "#FFFFFF");
    public static Color TextPrimary => Get("TextPrimary", "#1E1916");
    public static Color TextSecondary => Get("TextSecondary", "#5C5048");
    public static Color BorderSubtle => Get("BorderSubtle", "#EAE2DA");
    public static Color BadgeRed => Get("BadgeRed", "#B83227");
    public static Color StarGold => Get("StarGold", "#E89B3C");
    public static Color StaffPrimary => Get("StaffPrimary", "#C84B11");
    public static Color StaffInk => Get("StaffInk", "#1E1916");
    public static Color SuccessBackground => Get("SuccessBackground", "#FEF6EB");
    public static Color SuccessText => Get("SuccessText", "#8F3006");
    public static Color DangerBackground => Get("DangerBackground", "#FDECEB");
    public static Color DangerText => Get("DangerText", "#B83227");
    public static Color WarningBackground => Get("WarningBackground", "#FDF1EB");
    public static Color WarningText => Get("WarningText", "#8F3006");
    public static Color WarningMuted => Get("WarningMuted", "#D9822B");
    public static Color WarningStrong => Get("WarningStrong", "#C84B11");
    public static Color InfoBackground => Get("InfoBackground", "#FEF6EB");
    public static Color InfoDeep => Get("InfoDeep", "#C84B11");
    public static Color AuthMutedText => Get("AuthMutedText", "#8C7E74");
    public static Color AuthTitleText => Get("AuthTitleText", "#1E1916");
    public static Color AuthSubtitleOnHero => Get("AuthSubtitleOnHero", "#F9E0D2");
    public static Color AuthInputBorder => Get("AuthInputBorder", "#EAE2DA");
    public static Color Gray100 => Get("Gray100", "#F4EFEA");
    public static Color Gray300 => Get("Gray300", "#D6C9BD");
    public static Color Gray400 => Get("Gray400", "#8C7E74");
    public static Color Gray600 => Get("Gray600", "#5C5048");
    public static Color Slate600 => Get("Slate600", "#5C5048");
    public static Color Slate650 => Get("Slate650", "#6E5F55");
    public static Color SuccessBackgroundSoft => Get("SuccessBackgroundSoft", "#FDF4E3");
    public static Color Tertiary => Get("Tertiary", "#D9822B");
    public static Color FavoriteAccent => Get("FavoriteAccent", "#B83227");
    public static Color White => Colors.White;
}
