using SecurePasswordManager.Core.Security;

namespace SecurePasswordManager.Tests;

public sealed class EncryptionServiceTests
{
    [Fact]
    public void DeriveVaultKeys_UsesDistinctDatabaseAndAuthenticationMaterial()
    {
        var service = new EncryptionService();
        var salt = service.GenerateSalt();

        using var keys = service.DeriveVaultKeys("Correct-Horse-9-Battery!", salt, CryptoConstants.MinimumPbkdf2Iterations);

        Assert.Equal(32, keys.DatabaseKey.Length);
        Assert.Equal(32, keys.AuthenticationHash.Length);
        Assert.False(keys.DatabaseKey.SequenceEqual(keys.AuthenticationHash));
    }

    [Fact]
    public void EncryptString_RoundTripsAndRejectsWrongPassword()
    {
        var service = new EncryptionService();
        var payload = service.EncryptString("{\"secret\":\"value\"}", "Backup-Password-9!");

        var plainText = service.DecryptString(payload, "Backup-Password-9!");

        Assert.Contains("secret", plainText);
        Assert.ThrowsAny<Exception>(() => service.DecryptString(payload, "wrong-password"));
    }

    [Fact]
    public void ValidateMasterPassword_RejectsWeakPassword()
    {
        var service = new EncryptionService();

        var result = service.ValidateMasterPassword("password");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
