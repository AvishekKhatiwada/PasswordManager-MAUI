namespace SecurePasswordManager.Services;

public sealed class BiometricCredentialStore : IBiometricCredentialStore
{
    private const string DatabaseKeyName = "vault_database_key";

    public async Task SaveDatabaseKeyAsync(byte[] databaseKey)
    {
        ArgumentNullException.ThrowIfNull(databaseKey);
        await SecureStorage.Default.SetAsync(DatabaseKeyName, Convert.ToBase64String(databaseKey));
    }

    public async Task<byte[]?> TryGetDatabaseKeyAsync()
    {
        try
        {
            var value = await SecureStorage.Default.GetAsync(DatabaseKeyName);
            return string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value);
        }
        catch
        {
            return null;
        }
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(DatabaseKeyName);
    }
}
