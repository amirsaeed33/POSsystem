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

    public static Color Primary => Get("Primary", "#10B981");
    public static Color PrimaryDark => Get("PrimaryDark", "#059669");
    public static Color PrimaryLight => Get("PrimaryLight", "#D1FAE5");
    public static Color PageBackground => Get("PageBackground", "#F9FAFB");
    public static Color CardBackground => Get("CardBackground", "#FFFFFF");
    public static Color TextPrimary => Get("TextPrimary", "#111827");
    public static Color TextSecondary => Get("TextSecondary", "#6B7280");
    public static Color BorderSubtle => Get("BorderSubtle", "#E5E7EB");
    public static Color BadgeRed => Get("BadgeRed", "#EF4444");
    public static Color StarGold => Get("StarGold", "#F59E0B");
    public static Color StaffPrimary => Get("StaffPrimary", "#0F6B4C");
    public static Color StaffInk => Get("StaffInk", "#12261D");
    public static Color SuccessBackground => Get("SuccessBackground", "#ECFDF5");
    public static Color SuccessText => Get("SuccessText", "#065F46");
    public static Color DangerBackground => Get("DangerBackground", "#FEE2E2");
    public static Color DangerText => Get("DangerText", "#B91C1C");
    public static Color WarningBackground => Get("WarningBackground", "#FEF3C7");
    public static Color WarningText => Get("WarningText", "#92400E");
    public static Color WarningMuted => Get("WarningMuted", "#B45309");
    public static Color WarningStrong => Get("WarningStrong", "#C2410C");
    public static Color InfoBackground => Get("InfoBackground", "#EFF6FF");
    public static Color InfoDeep => Get("InfoDeep", "#1D4ED8");
    public static Color AuthMutedText => Get("AuthMutedText", "#64748B");
    public static Color AuthTitleText => Get("AuthTitleText", "#0F172A");
    public static Color AuthSubtitleOnHero => Get("AuthSubtitleOnHero", "#DBEAFE");
    public static Color AuthInputBorder => Get("AuthInputBorder", "#E2E8F0");
    public static Color Gray100 => Get("Gray100", "#F3F4F6");
    public static Color Gray300 => Get("Gray300", "#D1D5DB");
    public static Color Gray400 => Get("Gray400", "#9CA3AF");
    public static Color Gray600 => Get("Gray600", "#4B5563");
    public static Color Slate600 => Get("Slate600", "#475569");
    public static Color Slate650 => Get("Slate650", "#374151");
    public static Color SuccessBackgroundSoft => Get("SuccessBackgroundSoft", "#DCFCE7");
    public static Color Tertiary => Get("Tertiary", "#047857");
    public static Color FavoriteAccent => Get("FavoriteAccent", "#E11D48");
    public static Color White => Colors.White;
}
