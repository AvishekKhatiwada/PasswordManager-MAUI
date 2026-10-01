namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Portable AES-GCM encrypted payload used by backup files.
/// </summary>
public sealed class EncryptedPayload
{
    public int Version { get; set; } = 1;

    public string Algorithm { get; set; } = "AES-256-GCM";

    public string KeyDerivation { get; set; } = "PBKDF2-SHA512";

    public int Iterations { get; set; }

    public string Salt { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;

    public string CipherText { get; set; } = string.Empty;
}
