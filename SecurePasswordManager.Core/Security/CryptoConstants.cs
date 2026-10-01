namespace SecurePasswordManager.Core.Security;

/// <summary>
/// Central security parameters for vault and backup cryptography.
/// </summary>
public static class CryptoConstants
{
    public const int SaltSizeBytes = 32;
    public const int DatabaseKeySizeBytes = 32;
    public const int AuthenticationHashSizeBytes = 32;
    public const int AesKeySizeBytes = 32;
    public const int AesGcmNonceSizeBytes = 12;
    public const int AesGcmTagSizeBytes = 16;
    public const int DefaultPbkdf2Iterations = 210_000;
    public const int MinimumPbkdf2Iterations = 100_000;
}
