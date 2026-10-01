using SecurePasswordManager.Core.Models;

namespace SecurePasswordManager.Core.Services;

public interface ISettingsService
{
    Task<VaultSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task SaveSettingsAsync(VaultSettings settings, CancellationToken cancellationToken = default);

    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
