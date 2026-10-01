using System.Globalization;

namespace SecurePasswordManager.Utilities;

public sealed class SecureNoteDisplayConverter : IValueConverter
{
    private static readonly (Color Icon, Color Badge, Color BadgeText)[] Palette =
    [
        (Color.FromArgb("#7C3AED"), Color.FromArgb("#EDE9FE"), Color.FromArgb("#6D28D9")),
        (Color.FromArgb("#2563EB"), Color.FromArgb("#DBEAFE"), Color.FromArgb("#1D4ED8")),
        (Color.FromArgb("#16A34A"), Color.FromArgb("#DCFCE7"), Color.FromArgb("#15803D")),
        (Color.FromArgb("#EA580C"), Color.FromArgb("#FFEDD5"), Color.FromArgb("#C2410C")),
        (Color.FromArgb("#DB2777"), Color.FromArgb("#FCE7F3"), Color.FromArgb("#BE185D"))
    ];

    private static readonly (Color Icon, Color Badge, Color BadgeText) NeutralPalette =
        (Color.FromArgb("#94A3B8"), Color.FromArgb("#F1F5F9"), Color.FromArgb("#475569"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;

        return (parameter as string) switch
        {
            "PrimaryTag" => PrimaryTag(text),
            "HasTag" => !string.IsNullOrWhiteSpace(PrimaryTag(text)),
            "IconColor" => PaletteFor(text).Icon,
            "BadgeColor" => PaletteFor(text).Badge,
            "BadgeTextColor" => PaletteFor(text).BadgeText,
            "Snippet" => Snippet(text),
            _ => text
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static (Color Icon, Color Badge, Color BadgeText) PaletteFor(string tags)
    {
        var tag = PrimaryTag(tags);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return NeutralPalette;
        }

        var index = Math.Abs(tag.GetHashCode()) % Palette.Length;
        return Palette[index];
    }

    private static string PrimaryTag(string tags) =>
        tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;

    private static string Snippet(string markdown)
    {
        var line = markdown
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.TrimStart('#', '*', '-', ' ').Trim())
            .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;

        return line.Length > 90 ? line[..90].TrimEnd() + "..." : line;
    }
}
