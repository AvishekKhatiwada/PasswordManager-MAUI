using SQLite;

namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Audit entry that intentionally avoids storing credential secrets.
/// </summary>
[Table("AuditLogs")]
public sealed class AuditLog : EntityBase
{
    [Indexed]
    public AuditAction Action { get; set; }

    [Indexed]
    [MaxLength(32)]
    public string EntityId { get; set; } = string.Empty;

    [MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}
