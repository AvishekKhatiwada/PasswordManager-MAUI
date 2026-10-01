using SecurePasswordManager.Core.Security;

namespace SecurePasswordManager.Core.Services;

public interface IAuthenticationService
{
    VaultSession? CurrentSession { get; }

    bool IsUnlocked { get; }

    Task<bool> IsVaultCreatedAsync(CancellationToken cancellationToken = default);

    Task<VaultSession> CreateVaultAsync(string masterPassword, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> UnlockAsync(string masterPassword, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> UnlockWithDatabaseKeyAsync(byte[] databaseKey, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> ChangeMasterPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task LockAsync(CancellationToken cancellationToken = default);

    Task ResetVaultAsync(CancellationToken cancellationToken = default);

    void Touch();

    bool ShouldAutoLock(TimeSpan timeout);
}
