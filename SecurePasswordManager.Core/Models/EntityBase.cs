using SQLite;

namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Base SQLite entity with GUID identity and audit timestamps.
/// </summary>
public abstract class EntityBase
{
    [PrimaryKey]
    [MaxLength(32)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Indexed]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
