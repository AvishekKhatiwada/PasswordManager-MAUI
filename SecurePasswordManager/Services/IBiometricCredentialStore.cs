namespace SecurePasswordManager.Services;

public interface IBiometricCredentialStore
{
    Task SaveDatabaseKeyAsync(byte[] databaseKey);

    Task<byte[]?> TryGetDatabaseKeyAsync();

    void Clear();
}
