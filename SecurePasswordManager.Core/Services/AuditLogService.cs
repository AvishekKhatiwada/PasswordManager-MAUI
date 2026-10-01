using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IDatabaseService _database;

    public AuditLogService(IDatabaseService database)
    {
        _database = database;
    }

    public async Task RecordAsync(AuditAction action, string entityType = "", string entityId = "", string details = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_database.IsInitialized)
        {
            return;
        }

        var log = new AuditLog
        {
            Action = action,
            EntityId = entityId,
            EntityType = entityType,
            Details = Redact(details)
        };

        await _database.Connection.InsertAsync(log);
    }

    private static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 160 ? value : value[..160];
    }
}
