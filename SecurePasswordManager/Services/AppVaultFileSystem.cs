using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Services;

public sealed class AppVaultFileSystem : IVaultFileSystem
{
    public string AppDataDirectory => FileSystem.AppDataDirectory;

    public string DatabasePath => Path.Combine(AppDataDirectory, "vault.db");

    public string MetadataPath => Path.Combine(AppDataDirectory, "vault.meta.json");

    public string BackupDirectory => Path.Combine(AppDataDirectory, "Backups");
}
