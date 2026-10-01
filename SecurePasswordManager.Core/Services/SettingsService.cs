using System.Text.Json;
using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly IDatabaseService _database;
    private readonly IAuditLogService _auditLog;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SettingsService(IDatabaseService database, IAuditLogService auditLog)
    {
        _database = database;
        _auditLog = auditLog;
    }

    public async Task<VaultSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return new VaultSettings
        {
            Theme = await GetAsync("Theme", "System", cancellationToken),
            AutoLockMinutes = await GetAsync("AutoLockMinutes", 5, cancellationToken),
            ClipboardClearSeconds = await GetAsync("ClipboardClearSeconds", 30, cancellationToken),
            BiometricsEnabled = await GetAsync("BiometricsEnabled", false, cancellationToken),
            HighContrast = await GetAsync("HighContrast", false, cancellationToken)
        };
    }

    public async Task SaveSettingsAsync(VaultSettings settings, CancellationToken cancellationToken = default)
    {
        await SetAsync("Theme", settings.Theme, cancellationToken);
        await SetAsync("AutoLockMinutes", Math.Clamp(settings.AutoLockMinutes, 1, 120), cancellationToken);
        await SetAsync("ClipboardClearSeconds", Math.Clamp(settings.ClipboardClearSeconds, 5, 300), cancellationToken);
        await SetAsync("BiometricsEnabled", settings.BiometricsEnabled, cancellationToken);
        await SetAsync("HighContrast", settings.HighContrast, cancellationToken);
        await _auditLog.RecordAsync(AuditAction.SettingsUpdated, details: "Settings saved", cancellationToken: cancellationToken);
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var setting = await _database.Connection.Table<AppSetting>()
            .Where(item => item.Key == key)
            .FirstOrDefaultAsync();

        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(setting.Value, _jsonOptions) ?? defaultValue;
        }
        catch (JsonException)
        {
            return defaultValue;
        }
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var setting = new AppSetting
        {
            Key = key,
            Value = JsonSerializer.Serialize(value, _jsonOptions),
            UpdatedAt = DateTime.UtcNow
        };

        var existing = await _database.Connection.Table<AppSetting>()
            .Where(item => item.Key == key)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            await _database.Connection.InsertAsync(setting);
            return;
        }

        await _database.Connection.UpdateAsync(setting);
    }
}
