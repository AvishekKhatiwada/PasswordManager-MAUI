using Microsoft.Extensions.DependencyInjection;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Core.Security;

namespace SecurePasswordManager.Core.Services;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddSecurePasswordManagerCore(this IServiceCollection services)
    {
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IAuditLogService, AuditLogService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IPasswordGeneratorService, PasswordGeneratorService>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<ICredentialRepository, CredentialRepository>();
        services.AddSingleton<ISecureNoteRepository, SecureNoteRepository>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IBackupService, BackupService>();
        return services;
    }
}
