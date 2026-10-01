using SQLite;

namespace SecurePasswordManager.Core.Models;

/// <summary>
/// A credential, API key, email account, or custom login stored in the encrypted vault database.
/// </summary>
[Table("Credentials")]
public sealed class CredentialItem : EntityBase
{
    [Indexed]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(240)]
    public string Website { get; set; } = string.Empty;

    [Indexed]
    [MaxLength(160)]
    public string Username { get; set; } = string.Empty;

    [Indexed]
    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    [Indexed]
    [MaxLength(32)]
    public string CategoryId { get; set; } = string.Empty;

    [MaxLength(480)]
    public string Tags { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public VaultItemKind Kind { get; set; } = VaultItemKind.WebsiteCredential;

    [Indexed]
    public bool IsFavorite { get; set; }

    [Indexed]
    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    [Indexed]
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    [Ignore]
    public string CategoryName { get; set; } = string.Empty;
}
