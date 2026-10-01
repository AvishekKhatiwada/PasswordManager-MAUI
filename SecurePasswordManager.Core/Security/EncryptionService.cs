using System.Security.Cryptography;
using System.Text;
using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Security;

/// <summary>
/// PBKDF2 and AES-GCM implementation for zero-knowledge vault auth and encrypted backups.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private static readonly string[] CommonFragments =
    [
        "password",
        "qwerty",
        "letmein",
        "admin",
        "welcome",
        "dragon",
        "monkey"
    ];

    public byte[] GenerateSalt(int size = CryptoConstants.SaltSizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 16);
        return RandomNumberGenerator.GetBytes(size);
    }

    public VaultKeySet DeriveVaultKeys(string masterPassword, byte[] salt, int iterations)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            throw new ArgumentException("Master password is required.", nameof(masterPassword));
        }

        ArgumentNullException.ThrowIfNull(salt);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, CryptoConstants.MinimumPbkdf2Iterations);

        var outputSize = CryptoConstants.DatabaseKeySizeBytes + CryptoConstants.AuthenticationHashSizeBytes;
        using var pbkdf2 = new Rfc2898DeriveBytes(masterPassword, salt, iterations, HashAlgorithmName.SHA512);
        var derived = pbkdf2.GetBytes(outputSize);
        var databaseKey = new byte[CryptoConstants.DatabaseKeySizeBytes];
        var authHash = new byte[CryptoConstants.AuthenticationHashSizeBytes];
        Buffer.BlockCopy(derived, 0, databaseKey, 0, databaseKey.Length);
        Buffer.BlockCopy(derived, databaseKey.Length, authHash, 0, authHash.Length);
        CryptographicOperations.ZeroMemory(derived);
        return new VaultKeySet(databaseKey, authHash);
    }

    public byte[] DeriveAesKey(string password, byte[] salt, int iterations)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        ArgumentNullException.ThrowIfNull(salt);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, CryptoConstants.MinimumPbkdf2Iterations);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
        return pbkdf2.GetBytes(CryptoConstants.AesKeySizeBytes);
    }

    public string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);

    public byte[] FromBase64(string value) => Convert.FromBase64String(value);

    public bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    public PasswordValidationResult ValidateMasterPassword(string password)
    {
        var errors = new List<string>();
        password ??= string.Empty;

        if (password.Length < 12)
        {
            errors.Add("Use at least 12 characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Add an uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Add a lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Add a number.");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Add a symbol.");
        }

        var lower = password.ToLowerInvariant();
        if (CommonFragments.Any(lower.Contains))
        {
            errors.Add("Avoid common password words.");
        }

        var pool = 0;
        if (password.Any(char.IsLower))
        {
            pool += 26;
        }

        if (password.Any(char.IsUpper))
        {
            pool += 26;
        }

        if (password.Any(char.IsDigit))
        {
            pool += 10;
        }

        if (password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            pool += 32;
        }

        var entropy = password.Length * Math.Log2(Math.Max(pool, 1));
        var score = Math.Clamp(entropy / 100d, 0d, 1d);
        var label = score switch
        {
            >= 0.85 => "Excellent",
            >= 0.65 => "Strong",
            >= 0.45 => "Fair",
            _ => "Weak"
        };

        return new PasswordValidationResult
        {
            Score = score,
            Label = label,
            Errors = errors
        };
    }

    public EncryptedPayload EncryptString(string plainText, string password)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        var salt = GenerateSalt();
        var nonce = RandomNumberGenerator.GetBytes(CryptoConstants.AesGcmNonceSizeBytes);
        var tag = new byte[CryptoConstants.AesGcmTagSizeBytes];
        var cipherText = new byte[Encoding.UTF8.GetByteCount(plainText)];
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var key = DeriveAesKey(password, salt, CryptoConstants.DefaultPbkdf2Iterations);

        try
        {
            using var aes = new AesGcm(key, CryptoConstants.AesGcmTagSizeBytes);
            aes.Encrypt(nonce, plainBytes, cipherText, tag);

            return new EncryptedPayload
            {
                Iterations = CryptoConstants.DefaultPbkdf2Iterations,
                Salt = ToBase64(salt),
                Nonce = ToBase64(nonce),
                Tag = ToBase64(tag),
                CipherText = ToBase64(cipherText)
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public string DecryptString(EncryptedPayload payload, string password)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var salt = FromBase64(payload.Salt);
        var nonce = FromBase64(payload.Nonce);
        var tag = FromBase64(payload.Tag);
        var cipherText = FromBase64(payload.CipherText);
        var plainBytes = new byte[cipherText.Length];
        var key = DeriveAesKey(password, salt, payload.Iterations);

        try
        {
            using var aes = new AesGcm(key, CryptoConstants.AesGcmTagSizeBytes);
            aes.Decrypt(nonce, cipherText, tag, plainBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
            CryptographicOperations.ZeroMemory(key);
        }
    }
}
