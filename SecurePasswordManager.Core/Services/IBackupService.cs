namespace SecurePasswordManager.Core.Services;

public interface IBackupService
{
    Task<string> ExportEncryptedAsync(string outputPath, string backupPassword, CancellationToken cancellationToken = default);

    Task ImportEncryptedAsync(string inputPath, string backupPassword, bool merge = true, CancellationToken cancellationToken = default);
}
