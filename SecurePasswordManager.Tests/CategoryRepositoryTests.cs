namespace SecurePasswordManager.Tests;

public sealed class CategoryRepositoryTests
{
    [Fact]
    public async Task DeleteAsync_RemovesCustomCategory_ButRejectsDefaultCategory()
    {
        using var fileSystem = new TempVaultFileSystem();
        var services = new ServiceFactory(fileSystem);
        await services.Authentication.CreateVaultAsync("Correct-Horse-9-Battery!");

        var custom = await services.Categories.GetOrCreateAsync("Streaming");
        await services.Categories.DeleteAsync(custom.Id);
        Assert.Null(await services.Categories.GetByIdAsync(custom.Id));

        var defaultCategory = (await services.Categories.GetAllAsync()).First(category => category.IsDefault);
        await Assert.ThrowsAsync<InvalidOperationException>(() => services.Categories.DeleteAsync(defaultCategory.Id));
        Assert.NotNull(await services.Categories.GetByIdAsync(defaultCategory.Id));

        await services.Authentication.LockAsync();
    }
}
