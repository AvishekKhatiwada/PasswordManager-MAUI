namespace SecurePasswordManager.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task CreateVault_DoesNotStoreMasterPassword_AndUnlocksWithCorrectPassword()
    {
        using var fileSystem = new TempVaultFileSystem();
        var services = new ServiceFactory(fileSystem);
        const string masterPassword = "Correct-Horse-9-Battery!";

        await services.Authentication.CreateVaultAsync(masterPassword);
        await services.Authentication.LockAsync();

        var metadata = await File.ReadAllTextAsync(fileSystem.MetadataPath);
        Assert.DoesNotContain(masterPassword, metadata);
        Assert.Contains("passwordHash", metadata);
        Assert.Contains("salt", metadata);

        var failure = await services.Authentication.UnlockAsync("Wrong-Horse-9-Battery!");
        Assert.False(failure.Succeeded);

        var success = await services.Authentication.UnlockAsync(masterPassword);
        Assert.True(success.Succeeded);
        Assert.True(services.Authentication.IsUnlocked);

        await services.Authentication.LockAsync();
    }

    [Fact]
    public async Task DatabaseService_PersistsCredentialInsideEncryptedVault()
    {
        using var fileSystem = new TempVaultFileSystem();
        var services = new ServiceFactory(fileSystem);
        await services.Authentication.CreateVaultAsync("Correct-Horse-9-Battery!");
        var categories = await services.Categories.GetAllAsync();

        await services.Credentials.SaveAsync(new()
        {
            Title = "GitHub",
            Username = "octo",
            Password = "Token-Value-123!",
            CategoryId = categories.First().Id,
            Tags = "work,git"
        });

        var results = await services.Credentials.SearchAsync(new() { SearchText = "octo" });

        Assert.Single(results);
        Assert.Equal("GitHub", results[0].Title);
        await services.Authentication.LockAsync();
        var databaseBytes = await File.ReadAllBytesAsync(fileSystem.DatabasePath);
        var databaseText = System.Text.Encoding.UTF8.GetString(databaseBytes);
        Assert.DoesNotContain("Token-Value-123!", databaseText);
        Assert.True(databaseBytes.Length > 0);
    }

    [Fact]
    public async Task ChangeMasterPassword_ReKeysVault_OldPasswordRejected_NewPasswordUnlocksWithData()
    {
        using var fileSystem = new TempVaultFileSystem();
        var services = new ServiceFactory(fileSystem);
        const string oldPassword = "Correct-Horse-9-Battery!";
        const string newPassword = "Different-Staple-4-Clamp!";

        await services.Authentication.CreateVaultAsync(oldPassword);
        var categories = await services.Categories.GetAllAsync();
        await services.Credentials.SaveAsync(new()
        {
            Title = "GitHub",
            Username = "octo",
            Password = "Token-Value-123!",
            CategoryId = categories.First().Id
        });

        var wrongCurrent = await services.Authentication.ChangeMasterPasswordAsync("Wrong-Horse-9-Battery!", newPassword);
        Assert.False(wrongCurrent.Succeeded);

        var changed = await services.Authentication.ChangeMasterPasswordAsync(oldPassword, newPassword);
        Assert.True(changed.Succeeded);

        await services.Authentication.LockAsync();

        var oldPasswordUnlock = await services.Authentication.UnlockAsync(oldPassword);
        Assert.False(oldPasswordUnlock.Succeeded);

        var newPasswordUnlock = await services.Authentication.UnlockAsync(newPassword);
        Assert.True(newPasswordUnlock.Succeeded);

        var results = await services.Credentials.SearchAsync(new() { SearchText = "octo" });
        Assert.Single(results);
        Assert.Equal("GitHub", results[0].Title);

        await services.Authentication.LockAsync();
    }
}
