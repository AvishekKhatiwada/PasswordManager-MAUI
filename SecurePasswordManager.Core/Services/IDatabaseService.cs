using SQLite;

namespace SecurePasswordManager.Core.Services;

public interface IDatabaseService
{
    bool IsInitialized { get; }

    SQLiteAsyncConnection Connection { get; }

    Task InitializeAsync(byte[] databaseKey, CancellationToken cancellationToken = default);

    Task ReKeyAsync(byte[] newDatabaseKey, CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);

    Task DeleteVaultAsync(CancellationToken cancellationToken = default);
}
