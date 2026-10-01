using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Core.Security;
using SecurePasswordManager.Core.Services;

namespace SecurePasswordManager.Tests;

internal sealed class ServiceFactory
{
    public ServiceFactory(TempVaultFileSystem fileSystem)
    {
        FileSystem = fileSystem;
        Encryption = new EncryptionService();
        Database = new DatabaseService(fileSystem);
        Audit = new AuditLogService(Database);
        Authentication = new AuthenticationService(Database, fileSystem, Encryption, Audit);
        Categories = new CategoryRepository(Database);
        Credentials = new CredentialRepository(Database, Audit);
        Notes = new SecureNoteRepository(Database, Audit);
        Settings = new SettingsService(Database, Audit);
        Backup = new BackupService(Encryption, Credentials, Categories, Notes, Settings, Audit);
        Generator = new PasswordGeneratorService();
    }

    public TempVaultFileSystem FileSystem { get; }

    public IEncryptionService Encryption { get; }

    public IDatabaseService Database { get; }

    public IAuditLogService Audit { get; }

    public IAuthenticationService Authentication { get; }

    public ICategoryRepository Categories { get; }

    public ICredentialRepository Credentials { get; }

    public ISecureNoteRepository Notes { get; }

    public ISettingsService Settings { get; }

    public IBackupService Backup { get; }

    public IPasswordGeneratorService Generator { get; }
}
