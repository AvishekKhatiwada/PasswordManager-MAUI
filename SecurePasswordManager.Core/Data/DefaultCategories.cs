using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Data;

public static class DefaultCategories
{
    public static IReadOnlyList<Category> Create() =>
    [
        New("Social", "\u2605", "#2563EB"),
        New("Google", "G", "#DB4437"),
        New("GitHub", "\uE709", "#24292F"),
        New("Banking", "$", "#047857"),
        New("Work", "\u25A3", "#7C3AED"),
        New("Personal", "\u25CF", "#0F766E"),
        New("Entertainment", "\u25B6", "#E11D48"),
        New("Shopping", "\u25C6", "#EA580C"),
        New("Crypto", "\u20BF", "#B45309"),
        New("Servers", "\u25A6", "#334155"),
        New("Custom", "+", "#64748B")
    ];

    private static Category New(string name, string glyph, string color) =>
        new()
        {
            Name = name,
            IconGlyph = glyph,
            ColorHex = color,
            IsDefault = true
        };
}
