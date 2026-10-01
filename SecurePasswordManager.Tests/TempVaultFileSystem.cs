using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Tests;

internal sealed class TempVaultFileSystem : IVaultFileSystem, IDisposable
{
    public TempVaultFileSystem()
    {
        AppDataDirectory = Path.Combine(Path.GetTempPath(), "spm-tests", Guid.NewGuid().ToString("N"));
        BackupDirectory = Path.Combine(AppDataDirectory, "Backups");
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }

    public string AppDataDirectory { get; }

    public string DatabasePath => Path.Combine(AppDataDirectory, "vault.db");

    public string MetadataPath => Path.Combine(AppDataDirectory, "vault.meta.json");

    public string BackupDirectory { get; }

    public void Dispose()
    {
        if (Directory.Exists(AppDataDirectory))
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Directory.Delete(AppDataDirectory, recursive: true);
                    return;
                }
                catch (IOException) when (attempt < 4)
                {
                    Thread.Sleep(100);
                }
                catch (IOException)
                {
                    return;
                }
            }
        }
    }
}
