using SQLite;

namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Markdown-capable secure note stored in the same encrypted vault.
/// </summary>
[Table("SecureNotes")]
public sealed class SecureNote : EntityBase
{
    [Indexed]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public string Markdown { get; set; } = string.Empty;

    [MaxLength(480)]
    public string Tags { get; set; } = string.Empty;

    [Indexed]
    public bool IsFavorite { get; set; }

    [Indexed]
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
