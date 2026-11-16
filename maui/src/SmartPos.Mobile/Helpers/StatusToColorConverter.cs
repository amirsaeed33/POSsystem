using System.Globalization;

using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Helpers;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value switch
        {
            int i => i,
            string s when int.TryParse(s, out var n) => n,
            _ => -1
        };

        return status switch
        {
            0 => ThemeColors.Get("CartAccentBackground", "#FDF1EB"), // Available
            1 => ThemeColors.Get("OccupiedSoft", "#FDECEB"), // Occupied
            2 => ThemeColors.Get("BilledSoft", "#F9E0D2"), // Billed
            _ => ThemeColors.Gray100
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StatusToAccentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value switch
        {
            int i => i,
            string s when int.TryParse(s, out var n) => n,
            _ => -1
        };

        return status switch
        {
            0 => ThemeColors.StaffPrimary,
            1 => ThemeColors.Get("DangerAccent", "#B83227"),
            2 => ThemeColors.InfoDeep,
            _ => ThemeColors.Get("CartMuted", "#5C5048")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
