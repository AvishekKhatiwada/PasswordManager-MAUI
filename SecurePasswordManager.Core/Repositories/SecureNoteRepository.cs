using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Core.Repositories;

public sealed class SecureNoteRepository : ISecureNoteRepository
{
    private readonly IDatabaseService _database;
    private readonly IAuditLogService _auditLog;

    public SecureNoteRepository(IDatabaseService database, IAuditLogService auditLog)
    {
        _database = database;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<SecureNote>> SearchAsync(string searchText = "", bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = "SELECT * FROM SecureNotes WHERE 1=1";
        var args = new List<object>();

        if (!includeDeleted)
        {
            sql += " AND IsDeleted = 0";
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = $"%{searchText.Trim()}%";
            sql += " AND (Title LIKE ? OR Markdown LIKE ? OR Tags LIKE ?)";
            args.AddRange([term, term, term]);
        }

        sql += " ORDER BY IsFavorite DESC, UpdatedAt DESC";
        return await _database.Connection.QueryAsync<SecureNote>(sql, args.ToArray());
    }

    public async Task<SecureNote?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.Connection.Table<SecureNote>()
            .Where(note => note.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(SecureNote note, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        note.UpdatedAt = DateTime.UtcNow;
        var existing = await GetByIdAsync(note.Id, cancellationToken);
        if (existing is null)
        {
            note.CreatedAt = DateTime.UtcNow;
            await _database.Connection.InsertAsync(note);
            await _auditLog.RecordAsync(AuditAction.SecureNoteCreated, nameof(SecureNote), note.Id, note.Title, cancellationToken);
            return;
        }

        await _database.Connection.UpdateAsync(note);
        await _auditLog.RecordAsync(AuditAction.SecureNoteUpdated, nameof(SecureNote), note.Id, note.Title, cancellationToken);
    }

    public async Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var note = await GetByIdAsync(id, cancellationToken);
        if (note is null)
        {
            return;
        }

        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;
        await SaveAsync(note, cancellationToken);
    }

    public async Task RestoreAsync(string id, CancellationToken cancellationToken = default)
    {
        var note = await GetByIdAsync(id, cancellationToken);
        if (note is null)
        {
            return;
        }

        note.IsDeleted = false;
        note.DeletedAt = null;
        await SaveAsync(note, cancellationToken);
    }

    public async Task<IReadOnlyList<SecureNote>> GetTrashedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.Connection.Table<SecureNote>()
            .Where(note => note.IsDeleted)
            .OrderByDescending(note => note.UpdatedAt)
            .ToListAsync();
    }

    public async Task PermanentDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var note = await GetByIdAsync(id, cancellationToken);
        if (note is null)
        {
            return;
        }

        await _database.Connection.DeleteAsync(note);
        await _auditLog.RecordAsync(AuditAction.SecureNotePurged, nameof(SecureNote), id, note.Title, cancellationToken);
    }
}
