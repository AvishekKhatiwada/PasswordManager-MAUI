using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Security;

public interface IEncryptionService
{
    byte[] GenerateSalt(int size = CryptoConstants.SaltSizeBytes);

    VaultKeySet DeriveVaultKeys(string masterPassword, byte[] salt, int iterations);

    byte[] DeriveAesKey(string password, byte[] salt, int iterations);

    string ToBase64(byte[] bytes);

    byte[] FromBase64(string value);

    bool FixedTimeEquals(byte[] left, byte[] right);

    PasswordValidationResult ValidateMasterPassword(string password);

    EncryptedPayload EncryptString(string plainText, string password);

    string DecryptString(EncryptedPayload payload, string password);
}
