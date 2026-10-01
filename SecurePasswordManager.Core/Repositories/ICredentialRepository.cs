using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Repositories;

public interface ICredentialRepository
{
    Task<IReadOnlyList<CredentialItem>> SearchAsync(CredentialSearchQuery query, CancellationToken cancellationToken = default);

    Task<CredentialItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task SaveAsync(CredentialItem item, CancellationToken cancellationToken = default);

    Task<CredentialItem?> DuplicateAsync(string id, CancellationToken cancellationToken = default);

    Task SetFavoriteAsync(string id, bool isFavorite, CancellationToken cancellationToken = default);

    Task ArchiveAsync(string id, bool archive, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task RestoreAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CredentialItem>> GetTrashedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CredentialItem>> GetArchivedAsync(CancellationToken cancellationToken = default);

    Task PermanentDeleteAsync(string id, CancellationToken cancellationToken = default);
}
