using System.Security.Cryptography;
using System.Text.Json;
using SecurePasswordManager.Core.Data;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Security;

namespace SecurePasswordManager.Core.Services;

/// <summary>
/// Zero-knowledge vault authentication. The master password is never stored.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(2);
    private readonly IDatabaseService _database;
    private readonly IVaultFileSystem _fileSystem;
    private readonly IEncryptionService _encryption;
    private readonly IAuditLogService _auditLog;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public AuthenticationService(
        IDatabaseService database,
        IVaultFileSystem fileSystem,
        IEncryptionService encryption,
        IAuditLogService auditLog)
    {
        _database = database;
        _fileSystem = fileSystem;
        _encryption = encryption;
        _auditLog = auditLog;
    }

    public VaultSession? CurrentSession { get; private set; }

    public bool IsUnlocked => CurrentSession is { IsCleared: false } && _database.IsInitialized;

    public Task<bool> IsVaultCreatedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(_fileSystem.MetadataPath) && File.Exists(_fileSystem.DatabasePath));
    }

    public async Task<VaultSession> CreateVaultAsync(string masterPassword, CancellationToken cancellationToken = default)
    {
        var validation = _encryption.ValidateMasterPassword(masterPassword);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (await IsVaultCreatedAsync(cancellationToken))
            {
                throw new InvalidOperationException("A vault already exists.");
            }

            Directory.CreateDirectory(_fileSystem.AppDataDirectory);
            Directory.CreateDirectory(_fileSystem.BackupDirectory);

            var salt = _encryption.GenerateSalt();
            using var keys = await Task.Run(
                () => _encryption.DeriveVaultKeys(masterPassword, salt, CryptoConstants.DefaultPbkdf2Iterations),
                cancellationToken);
            var metadata = new VaultMetadata
            {
                Salt = _encryption.ToBase64(salt),
                PasswordHash = _encryption.ToBase64(keys.AuthenticationHash),
                Iterations = CryptoConstants.DefaultPbkdf2Iterations,
                HashAlgorithm = "PBKDF2-SHA512"
            };

            await SaveMetadataAsync(metadata, cancellationToken);
            await _database.InitializeAsync(keys.DatabaseKey, cancellationToken);
            await SeedAsync(cancellationToken);

            var sessionKey = keys.GetDatabaseKeyCopy();
            try
            {
                ReplaceSession(sessionKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sessionKey);
            }

            await _auditLog.RecordAsync(AuditAction.VaultCreated, details: "Vault created", cancellationToken: cancellationToken);
            return CurrentSession!;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AuthenticationResult> UnlockAsync(string masterPassword, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var metadata = await LoadMetadataAsync(cancellationToken);
            if (metadata is null)
            {
                return AuthenticationResult.Failure("No vault exists yet.");
            }

            if (metadata.LockoutUntilUtc is { } lockout && lockout > DateTime.UtcNow)
            {
                return AuthenticationResult.Failure("Too many attempts. Try again later.", lockout - DateTime.UtcNow);
            }

            var salt = _encryption.FromBase64(metadata.Salt);
            using var keys = await Task.Run(
                () => _encryption.DeriveVaultKeys(masterPassword, salt, metadata.Iterations),
                cancellationToken);
            var expectedHash = _encryption.FromBase64(metadata.PasswordHash);

            if (!_encryption.FixedTimeEquals(keys.AuthenticationHash, expectedHash))
            {
                metadata.FailedAttempts++;
                if (metadata.FailedAttempts >= MaxFailedAttempts)
                {
                    metadata.LockoutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                    metadata.FailedAttempts = 0;
                }

                await SaveMetadataAsync(metadata, cancellationToken);
                return AuthenticationResult.Failure("Master password was not accepted.", metadata.LockoutUntilUtc - DateTime.UtcNow);
            }

            metadata.FailedAttempts = 0;
            metadata.LockoutUntilUtc = null;
            await SaveMetadataAsync(metadata, cancellationToken);
            await _database.InitializeAsync(keys.DatabaseKey, cancellationToken);
            var sessionKey = keys.GetDatabaseKeyCopy();
            try
            {
                ReplaceSession(sessionKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sessionKey);
            }

            await _auditLog.RecordAsync(AuditAction.VaultUnlocked, details: "Password unlock", cancellationToken: cancellationToken);
            return AuthenticationResult.Success(CurrentSession!);
        }
        catch (CryptographicException)
        {
            return AuthenticationResult.Failure("Vault unlock failed.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AuthenticationResult> UnlockWithDatabaseKeyAsync(byte[] databaseKey, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!await IsVaultCreatedAsync(cancellationToken))
            {
                return AuthenticationResult.Failure("No vault exists yet.");
            }

            await _database.InitializeAsync(databaseKey, cancellationToken);
            ReplaceSession(databaseKey);
            await _auditLog.RecordAsync(AuditAction.VaultUnlocked, details: "Biometric unlock", cancellationToken: cancellationToken);
            return AuthenticationResult.Success(CurrentSession!, "Vault unlocked with biometrics.");
        }
        catch
        {
            return AuthenticationResult.Failure("Biometric vault key was not accepted. Use the master password.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AuthenticationResult> ChangeMasterPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsUnlocked)
            {
                return AuthenticationResult.Failure("Unlock the vault before changing the master password.");
            }

            var validation = _encryption.ValidateMasterPassword(newPassword);
            if (!validation.IsValid)
            {
                return AuthenticationResult.Failure(string.Join(Environment.NewLine, validation.Errors));
            }

            var metadata = await LoadMetadataAsync(cancellationToken);
            if (metadata is null)
            {
                return AuthenticationResult.Failure("No vault exists yet.");
            }

            var currentSalt = _encryption.FromBase64(metadata.Salt);
            using var currentKeys = await Task.Run(
                () => _encryption.DeriveVaultKeys(currentPassword, currentSalt, metadata.Iterations),
                cancellationToken);
            var expectedHash = _encryption.FromBase64(metadata.PasswordHash);
            if (!_encryption.FixedTimeEquals(currentKeys.AuthenticationHash, expectedHash))
            {
                return AuthenticationResult.Failure("Current password was not accepted.");
            }

            var newSalt = _encryption.GenerateSalt();
            using var newKeys = await Task.Run(
                () => _encryption.DeriveVaultKeys(newPassword, newSalt, CryptoConstants.DefaultPbkdf2Iterations),
                cancellationToken);

            await _database.ReKeyAsync(newKeys.DatabaseKey, cancellationToken);

            metadata.Salt = _encryption.ToBase64(newSalt);
            metadata.PasswordHash = _encryption.ToBase64(newKeys.AuthenticationHash);
            metadata.Iterations = CryptoConstants.DefaultPbkdf2Iterations;
            metadata.FailedAttempts = 0;
            metadata.LockoutUntilUtc = null;
            await SaveMetadataAsync(metadata, cancellationToken);

            var sessionKey = newKeys.GetDatabaseKeyCopy();
            try
            {
                ReplaceSession(sessionKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sessionKey);
            }

            await _auditLog.RecordAsync(AuditAction.MasterPasswordChanged, details: "Master password changed", cancellationToken: cancellationToken);
            return AuthenticationResult.Success(CurrentSession!, "Master password changed.");
        }
        catch (CryptographicException)
        {
            return AuthenticationResult.Failure("Master password change failed.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _auditLog.RecordAsync(AuditAction.VaultLocked, details: "Vault locked", cancellationToken: cancellationToken);
            CurrentSession?.Dispose();
            CurrentSession = null;
            await _database.CloseAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetVaultAsync(CancellationToken cancellationToken = default)
    {
        await LockAsync(cancellationToken);
        if (File.Exists(_fileSystem.MetadataPath))
        {
            File.Delete(_fileSystem.MetadataPath);
        }

        await _database.DeleteVaultAsync(cancellationToken);
    }

    public void Touch() => CurrentSession?.Touch();

    public bool ShouldAutoLock(TimeSpan timeout)
    {
        if (!IsUnlocked || CurrentSession is null)
        {
            return false;
        }

        return DateTime.UtcNow - CurrentSession.LastActivityUtc >= timeout;
    }

    private void ReplaceSession(byte[] databaseKey)
    {
        CurrentSession?.Dispose();
        var copy = new byte[databaseKey.Length];
        Buffer.BlockCopy(databaseKey, 0, copy, 0, databaseKey.Length);
        CurrentSession = new VaultSession(copy);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var connection = _database.Connection;
        foreach (var category in DefaultCategories.Create())
        {
            await connection.InsertAsync(category);
        }

        await connection.InsertAsync(new SecureNote
        {
            Title = "Welcome to your vault",
            Markdown = "This encrypted note is stored locally in SQLCipher. Replace it with your own secure notes whenever you are ready.",
            Tags = "welcome,security"
        });
    }

    private async Task<VaultMetadata?> LoadMetadataAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_fileSystem.MetadataPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_fileSystem.MetadataPath);
        return await JsonSerializer.DeserializeAsync<VaultMetadata>(stream, _jsonOptions, cancellationToken);
    }

    private async Task SaveMetadataAsync(VaultMetadata metadata, CancellationToken cancellationToken)
    {
        metadata.UpdatedAt = DateTime.UtcNow;
        Directory.CreateDirectory(_fileSystem.AppDataDirectory);
        await using var stream = File.Create(_fileSystem.MetadataPath);
        await JsonSerializer.SerializeAsync(stream, metadata, _jsonOptions, cancellationToken);
    }
}
