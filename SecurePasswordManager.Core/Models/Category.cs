using SQLite;

namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Category metadata used for filtering and visual grouping.
/// </summary>
[Table("Categories")]
public sealed class Category : EntityBase
{
    [Indexed]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(16)]
    public string IconGlyph { get; set; } = "\u25cf";

    [MaxLength(16)]
    public string ColorHex { get; set; } = "#2563EB";

    public bool IsDefault { get; set; }
}
