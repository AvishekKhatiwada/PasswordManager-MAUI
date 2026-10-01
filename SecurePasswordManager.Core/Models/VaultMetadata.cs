namespace SecurePasswordManager.Core.Models;

/// <summary>
/// Non-secret vault metadata stored outside the encrypted database.
/// </summary>
public sealed class VaultMetadata
{
    public int Version { get; set; } = 1;

    public string PasswordHash { get; set; } = string.Empty;

    public string Salt { get; set; } = string.Empty;

    public int Iterations { get; set; }

    public string HashAlgorithm { get; set; } = "SHA512";

    public int FailedAttempts { get; set; }

    public DateTime? LockoutUntilUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
