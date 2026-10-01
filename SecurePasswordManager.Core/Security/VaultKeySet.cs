using System.Security.Cryptography;

namespace SecurePasswordManager.Core.Security;

/// <summary>
/// Derived vault keys. Instances clear key material when disposed.
/// </summary>
public sealed class VaultKeySet : IDisposable
{
    public VaultKeySet(byte[] databaseKey, byte[] authenticationHash)
    {
        DatabaseKey = databaseKey;
        AuthenticationHash = authenticationHash;
    }

    public byte[] DatabaseKey { get; }

    public byte[] AuthenticationHash { get; }

    public byte[] GetDatabaseKeyCopy()
    {
        var copy = new byte[DatabaseKey.Length];
        Buffer.BlockCopy(DatabaseKey, 0, copy, 0, DatabaseKey.Length);
        return copy;
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(DatabaseKey);
        CryptographicOperations.ZeroMemory(AuthenticationHash);
    }
}
