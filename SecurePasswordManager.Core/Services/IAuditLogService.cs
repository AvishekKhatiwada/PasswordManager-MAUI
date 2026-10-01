using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

public interface IAuditLogService
{
    Task RecordAsync(AuditAction action, string entityType = "", string entityId = "", string details = "", CancellationToken cancellationToken = default);
}
