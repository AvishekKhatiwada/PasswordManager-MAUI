namespace SecurePasswordManager.Core.Services;

public interface IVaultFileSystem
{
    string AppDataDirectory { get; }

    string DatabasePath { get; }

    string MetadataPath { get; }

    string BackupDirectory { get; }
}
