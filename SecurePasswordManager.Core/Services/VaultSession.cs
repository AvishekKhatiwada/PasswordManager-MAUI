using System.Security.Cryptography;

namespace SecurePasswordManager.Core.Services;

/// <summary>
/// In-memory unlocked vault session. Key material is cleared on lock.
/// </summary>
public sealed class VaultSession : IDisposable
{
    private byte[] _databaseKey;

    public VaultSession(byte[] databaseKey)
    {
        _databaseKey = databaseKey;
        CreatedAtUtc = DateTime.UtcNow;
        LastActivityUtc = CreatedAtUtc;
    }

    public DateTime CreatedAtUtc { get; }

    public DateTime LastActivityUtc { get; private set; }

    public bool IsCleared { get; private set; }

    public byte[] GetDatabaseKeyCopy()
    {
        ObjectDisposedException.ThrowIf(IsCleared, this);
        var copy = new byte[_databaseKey.Length];
        Buffer.BlockCopy(_databaseKey, 0, copy, 0, _databaseKey.Length);
        return copy;
    }

    public void Touch() => LastActivityUtc = DateTime.UtcNow;

    public void Dispose()
    {
        if (IsCleared)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_databaseKey);
        _databaseKey = [];
        IsCleared = true;
    }
}
