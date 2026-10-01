using SQLite;

namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Encrypted-database setting row.
/// </summary>
[Table("Settings")]
public sealed class AppSetting
{
    [PrimaryKey]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
