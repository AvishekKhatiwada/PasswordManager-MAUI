using System.Text.Json;
using SecurePasswordManager.Core.Models;
using SecurePasswordManager.Core.Repositories;
using SecurePasswordManager.Core.Security;

namespace SecurePasswordManager.Core.Services;

/// <summary>
/// Encrypted JSON backup export/import service.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IEncryptionService _encryption;
    private readonly ICredentialRepository _credentials;
    private readonly ICategoryRepository _categories;
    private readonly ISecureNoteRepository _notes;
    private readonly ISettingsService _settings;
    private readonly IAuditLogService _auditLog;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public BackupService(
        IEncryptionService encryption,
        ICredentialRepository credentials,
        ICategoryRepository categories,
        ISecureNoteRepository notes,
        ISettingsService settings,
        IAuditLogService auditLog)
    {
        _encryption = encryption;
        _credentials = credentials;
        _categories = categories;
        _notes = notes;
        _settings = settings;
        _auditLog = auditLog;
    }

    public async Task<string> ExportEncryptedAsync(string outputPath, string backupPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupPassword))
        {
            throw new ArgumentException("A backup password is required.", nameof(backupPassword));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        var allCredentials = new List<CredentialItem>();
        var offset = 0;
        while (true)
        {
            var page = await _credentials.SearchAsync(new CredentialSearchQuery
            {
                IncludeArchived = true,
                IncludeDeleted = true,
                Limit = 500,
                Offset = offset
            }, cancellationToken);

            if (page.Count == 0)
            {
                break;
            }

            allCredentials.AddRange(page);
            offset += page.Count;
        }

        var document = new BackupDocument
        {
            ExportedAt = DateTime.UtcNow,
            Categories = (await _categories.GetAllAsync(cancellationToken)).ToList(),
            Credentials = allCredentials,
            SecureNotes = (await _notes.SearchAsync(includeDeleted: true, cancellationToken: cancellationToken)).ToList(),
            Settings = await _settings.GetSettingsAsync(cancellationToken)
        };

        var json = JsonSerializer.Serialize(document, _jsonOptions);
        var encrypted = _encryption.EncryptString(json, backupPassword);
        var encryptedJson = JsonSerializer.Serialize(encrypted, _jsonOptions);
        await File.WriteAllTextAsync(outputPath, encryptedJson, cancellationToken);
        await _auditLog.RecordAsync(AuditAction.BackupExported, details: Path.GetFileName(outputPath), cancellationToken: cancellationToken);
        return outputPath;
    }

    public async Task ImportEncryptedAsync(string inputPath, string backupPassword, bool merge = true, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Backup file was not found.", inputPath);
        }

        var encryptedJson = await File.ReadAllTextAsync(inputPath, cancellationToken);
        var payload = JsonSerializer.Deserialize<EncryptedPayload>(encryptedJson, _jsonOptions)
            ?? throw new InvalidOperationException("Backup file is not valid.");
        var json = _encryption.DecryptString(payload, backupPassword);
        var document = JsonSerializer.Deserialize<BackupDocument>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Backup payload is not valid.");

        foreach (var category in document.Categories)
        {
            await _categories.SaveAsync(category, cancellationToken);
        }

        foreach (var credential in document.Credentials)
        {
            if (!merge)
            {
                credential.Id = Guid.NewGuid().ToString("N");
            }

            await _credentials.SaveAsync(credential, cancellationToken);
        }

        foreach (var note in document.SecureNotes)
        {
            if (!merge)
            {
                note.Id = Guid.NewGuid().ToString("N");
            }

            await _notes.SaveAsync(note, cancellationToken);
        }

        await _settings.SaveSettingsAsync(document.Settings, cancellationToken);
        await _auditLog.RecordAsync(AuditAction.BackupImported, details: Path.GetFileName(inputPath), cancellationToken: cancellationToken);
    }
}
