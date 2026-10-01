using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Repositories;

public interface ISecureNoteRepository
{
    Task<IReadOnlyList<SecureNote>> SearchAsync(string searchText = "", bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task<SecureNote?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task SaveAsync(SecureNote note, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task RestoreAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SecureNote>> GetTrashedAsync(CancellationToken cancellationToken = default);

    Task PermanentDeleteAsync(string id, CancellationToken cancellationToken = default);
}
