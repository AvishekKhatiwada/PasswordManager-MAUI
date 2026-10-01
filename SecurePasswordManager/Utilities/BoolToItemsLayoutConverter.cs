using System.Globalization;

namespace SecurePasswordManager.Utilities;

public sealed class BoolToItemsLayoutConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
            : new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is GridItemsLayout;
    }
}
