using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Core.Repositories;

public sealed class CredentialRepository : ICredentialRepository
{
    private readonly IDatabaseService _database;
    private readonly IAuditLogService _auditLog;

    public CredentialRepository(IDatabaseService database, IAuditLogService auditLog)
    {
        _database = database;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<CredentialItem>> SearchAsync(CredentialSearchQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        query ??= new CredentialSearchQuery();
        var sql = "SELECT c.*, COALESCE(cat.Name, '') AS CategoryName FROM Credentials c LEFT JOIN Categories cat ON c.CategoryId = cat.Id WHERE 1=1";
        var args = new List<object>();

        if (!query.IncludeDeleted)
        {
            sql += " AND c.IsDeleted = 0";
        }

        if (!query.IncludeArchived)
        {
            sql += " AND c.IsArchived = 0";
        }

        if (query.FavoritesOnly)
        {
            sql += " AND c.IsFavorite = 1";
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            sql += " AND c.CategoryId = ?";
            args.Add(query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var term = $"%{query.SearchText.Trim()}%";
            sql += " AND (c.Title LIKE ? OR c.Username LIKE ? OR c.Email LIKE ? OR c.Website LIKE ? OR c.Url LIKE ? OR c.Tags LIKE ? OR c.Notes LIKE ? OR cat.Name LIKE ?)";
            args.AddRange([term, term, term, term, term, term, term, term]);
        }

        sql += " ORDER BY c.IsFavorite DESC, c.UpdatedAt DESC LIMIT ? OFFSET ?";
        args.Add(Math.Clamp(query.Limit, 1, 500));
        args.Add(Math.Max(query.Offset, 0));

        return await _database.Connection.QueryAsync<CredentialItem>(sql, args.ToArray());
    }

    public async Task<CredentialItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.Connection.Table<CredentialItem>()
            .Where(item => item.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(CredentialItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        item.UpdatedAt = DateTime.UtcNow;

        var exists = await GetByIdAsync(item.Id, cancellationToken);
        if (exists is null)
        {
            item.CreatedAt = DateTime.UtcNow;
            await _database.Connection.InsertAsync(item);
            await _auditLog.RecordAsync(AuditAction.CredentialCreated, nameof(CredentialItem), item.Id, item.Title, cancellationToken);
            return;
        }

        await _database.Connection.UpdateAsync(item);
        await _auditLog.RecordAsync(AuditAction.CredentialUpdated, nameof(CredentialItem), item.Id, item.Title, cancellationToken);
    }

    public async Task<CredentialItem?> DuplicateAsync(string id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var copy = new CredentialItem
        {
            Title = $"{existing.Title} Copy",
            Website = existing.Website,
            Username = existing.Username,
            Email = existing.Email,
            Password = existing.Password,
            Notes = existing.Notes,
            CategoryId = existing.CategoryId,
            Tags = existing.Tags,
            Url = existing.Url,
            Kind = existing.Kind,
            IsFavorite = false
        };
        await SaveAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetFavoriteAsync(string id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IsFavorite = isFavorite;
        await SaveAsync(item, cancellationToken);
    }

    public async Task ArchiveAsync(string id, bool archive, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IsArchived = archive;
        item.ArchivedAt = archive ? DateTime.UtcNow : null;
        await SaveAsync(item, cancellationToken);
        await _auditLog.RecordAsync(AuditAction.CredentialArchived, nameof(CredentialItem), id, archive ? "Archived" : "Unarchived", cancellationToken);
    }

    public async Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await SaveAsync(item, cancellationToken);
        await _auditLog.RecordAsync(AuditAction.CredentialDeleted, nameof(CredentialItem), id, item.Title, cancellationToken);
    }

    public async Task RestoreAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IsDeleted = false;
        item.DeletedAt = null;
        await SaveAsync(item, cancellationToken);
        await _auditLog.RecordAsync(AuditAction.CredentialRestored, nameof(CredentialItem), id, item.Title, cancellationToken);
    }

    public Task<IReadOnlyList<CredentialItem>> GetTrashedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return QueryFlaggedAsync("IsDeleted = 1");
    }

    public Task<IReadOnlyList<CredentialItem>> GetArchivedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return QueryFlaggedAsync("IsArchived = 1 AND IsDeleted = 0");
    }

    public async Task PermanentDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return;
        }

        await _database.Connection.DeleteAsync(item);
        await _auditLog.RecordAsync(AuditAction.CredentialPurged, nameof(CredentialItem), id, item.Title, cancellationToken);
    }

    private async Task<IReadOnlyList<CredentialItem>> QueryFlaggedAsync(string whereClause)
    {
        var sql = $"SELECT c.*, COALESCE(cat.Name, '') AS CategoryName FROM Credentials c " +
                  $"LEFT JOIN Categories cat ON c.CategoryId = cat.Id WHERE {whereClause} ORDER BY c.UpdatedAt DESC";
        return await _database.Connection.QueryAsync<CredentialItem>(sql);
    }
}
