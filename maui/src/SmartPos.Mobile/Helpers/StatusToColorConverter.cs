using System.Globalization;

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
            0 => Color.FromArgb("#E7F6EE"), // Available
            1 => Color.FromArgb("#FFF1E0"), // Occupied
            2 => Color.FromArgb("#E8EEFF"), // Billed
            _ => Color.FromArgb("#F1F3F2")
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
            0 => Color.FromArgb("#0F6B4C"),
            1 => Color.FromArgb("#C45C16"),
            2 => Color.FromArgb("#3558C7"),
            _ => Color.FromArgb("#5C6B63")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
