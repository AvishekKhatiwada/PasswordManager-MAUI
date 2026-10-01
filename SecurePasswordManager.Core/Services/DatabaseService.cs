using SQLite;
using SQLitePCL;
using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

/// <summary>
/// Owns the SQLCipher-backed SQLite connection and schema initialization.
/// </summary>
public sealed class DatabaseService : IDatabaseService
{
    private readonly IVaultFileSystem _fileSystem;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public DatabaseService(IVaultFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public bool IsInitialized => _connection is not null;

    public SQLiteAsyncConnection Connection =>
        _connection ?? throw new InvalidOperationException("The vault database is locked or has not been initialized.");

    public async Task InitializeAsync(byte[] databaseKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseKey);
        cancellationToken.ThrowIfCancellationRequested();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                SQLiteAsyncConnection.ResetPool();
            }

            Directory.CreateDirectory(_fileSystem.AppDataDirectory);
            Batteries_V2.Init();

            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex;
            var connectionString = new SQLiteConnectionString(
                _fileSystem.DatabasePath,
                flags,
                storeDateTimeAsTicks: true,
                key: databaseKey,
                preKeyAction: null,
                postKeyAction: connection =>
                {
                    connection.Execute("PRAGMA cipher_memory_security = ON;");
                    connection.Execute("PRAGMA foreign_keys = ON;");
                    connection.Execute("PRAGMA synchronous = NORMAL;");
                },
                vfsName: null,
                dateTimeStringFormat: null,
                storeTimeSpanAsTicks: true);

            _connection = new SQLiteAsyncConnection(connectionString)
            {
                Trace = false,
                TimeExecution = false
            };

            await CreateSchemaAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReKeyAsync(byte[] newDatabaseKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newDatabaseKey);
        cancellationToken.ThrowIfCancellationRequested();
        await Connection.ReKeyAsync(newDatabaseKey);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is null)
            {
                return;
            }

            await _connection.CloseAsync();
            _connection = null;
            SQLiteAsyncConnection.ResetPool();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteVaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await CloseAsync(cancellationToken);

        foreach (var path in Directory.EnumerateFiles(_fileSystem.AppDataDirectory, "vault.db*"))
        {
            File.Delete(path);
        }
    }

    private async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = Connection;
        await connection.CreateTableAsync<Category>();
        await connection.CreateTableAsync<CredentialItem>();
        await connection.CreateTableAsync<SecureNote>();
        await connection.CreateTableAsync<AppSetting>();
        await connection.CreateTableAsync<AuditLog>();

        await connection.CreateIndexAsync("Credentials", "Title");
        await connection.CreateIndexAsync("Credentials", "Username");
        await connection.CreateIndexAsync("Credentials", "Email");
        await connection.CreateIndexAsync("Credentials", "CategoryId");
        await connection.CreateIndexAsync("Credentials", "IsDeleted");
        await connection.CreateIndexAsync("Credentials", "UpdatedAt");
        await connection.CreateIndexAsync("SecureNotes", "Title");
        await connection.CreateIndexAsync("SecureNotes", "UpdatedAt");
        await connection.CreateIndexAsync("AuditLogs", "Action");
        await connection.CreateIndexAsync("AuditLogs", "CreatedAt");
    }
}
